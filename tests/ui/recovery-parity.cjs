// Real Razor templates on the disposable Kestrel hosts in the recovery tests.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const errorHost = process.argv[2] === 'error';
const { base, member } = errorHost ? { base: process.env.TRACEBACK_URL } : JSON.parse(fs.readFileSync(0, 'utf8'));
const evidence = process.env.RECOVERY_EVIDENCE;
const denied = { key: 'denied', url: '/Admin', status: 200, roles: ['member'], destination: '/' };
const missing = { key: 'not-found', url: '/Items/Details/000000000000000000000000', status: 404, roles: ['guest', 'member'], destination: '/Items' };
const privacy = { key: 'privacy', url: '/Home/Privacy', status: 200, roles: ['guest', 'member'], destination: '/Home/Privacy' };
const error = { key: 'error', url: '/Home/Privacy?fixtureFault=parity-marker', status: 500, roles: ['guest', 'member'], destination: '/Home/Privacy' };
const cases = errorHost ? [error] : [denied, missing, privacy];
const geometryFailures = [];
const matched = [];

function expectGeometry(label, actual, goal, tolerance = 1) {
  for (const [key, target] of Object.entries(goal)) {
    if (!Number.isFinite(actual[key]) || Math.abs(actual[key] - target) > tolerance) {
      geometryFailures.push(`${label} ${key}: ${actual[key]} vs ${target}`);
    }
  }
}

async function verifyFixedArtwork(page, label) {
  // Viewport crops keep the art and document in their original stacking contexts.
  const style = 'body > :not([data-route-art]) { opacity: 0 !important; }';
  const crop = extra => page.screenshot({ style: style + extra, animations: 'disabled' });
  const before = await crop('');
  assert(!before.equals(await crop(' [data-route-art] { opacity: 0 !important; }')), `${label}: painted artwork`);
  const heading = page.locator('main h1');
  const top = await heading.evaluate(e => e.getBoundingClientRect().top);
  const moved = await page.evaluate(() => { scrollTo({ top: 220, behavior: 'instant' }); return scrollY; });
  assert(moved >= 100, `${label}: insufficient room to exercise real document scrolling`);
  assert.equal(top - await heading.evaluate(e => e.getBoundingClientRect().top), moved);
  assert(before.equals(await crop('')), `${label}: route artwork moved with the document`);
}

async function verify(page, scenario, role, theme, width, height, reference = false) {
  const isPrivacy = scenario === privacy;
  const isError = scenario === error;
  const desktop = width >= 896;
  const phone = width === 390;
  const label = `${scenario.key}-${role}-${theme}-${width}`;
  const errors = [];
  page.on('pageerror', e => errors.push(e.message));
  page.on('response', r => {
    if (r.status() >= 400 && r.request().resourceType() !== 'document') errors.push(`${r.status()} ${r.url()}`);
  });
  if (role === 'member') {
    if (errorHost) assert.equal((await page.request.post(`${base}/ui-controls-test/member-cookie`)).status(), 204);
    else await page.context().addCookies([{ ...member, url: base }]);
  }
  const url = `${base}${scenario.url}${isError && role === 'guest' ? '&omitReference=1' : ''}`;
  const response = await page.goto(url);
  assert.equal(response.status(), scenario.status, label);
  if (scenario === denied) assert.equal((await response.request().redirectedFrom().response()).status(), 302);
  if (isError) {
    assert.equal(await page.locator('main code').count(), role === 'member' ? 1 : 0);
    assert.equal(await page.locator('main .tb-recovery-content__actions a').count(), 2);
    assert.doesNotMatch(await page.locator('main').innerText(), /parity-marker|Fixture exception detail|InvalidOperationException/);
    assert.equal(await response.headerValue('referrer-policy'), 'no-referrer');
  }
  await page.evaluate(() => document.fonts.ready);
  const metrics = await page.evaluate(({ isPrivacy, key }) => {
    const rect = e => e?.getBoundingClientRect();
    const select = s => document.querySelector(s);
    const panel = select(isPrivacy ? '.tb-privacy__policy' : '.tb-recovery-content--page');
    const title = select('main h1');
    const help = panel.querySelector(isPrivacy ? 'p' : ':scope > p:not(.tb-recovery-content__reference)');
    const art = panel.querySelector('.tb-recovery-content__art');
    const icon = art?.querySelector('.tb-icon');
    const controls = [...panel.querySelectorAll('.tb-recovery-content__actions a')];
    const ref = panel.querySelector('.tb-recovery-content__reference');
    const subtitle = panel.querySelector('strong');
    const background = select(`[data-route-art="${key}"]`);
    return {
      panelTop: rect(panel).top, panelWidth: rect(panel).width, panelHeight: rect(panel).height,
      panelRadius: parseFloat(getComputedStyle(panel).borderRadius), panelColor: getComputedStyle(panel).backgroundColor,
      borderColor: getComputedStyle(panel).borderTopColor,
      titleTop: rect(title).top, titleOffset: rect(title).top - rect(panel).top,
      titleSize: parseFloat(getComputedStyle(title).fontSize), titleLine: parseFloat(getComputedStyle(title).lineHeight),
      helpSize: parseFloat(getComputedStyle(help).fontSize), helpLine: parseFloat(getComputedStyle(help).lineHeight),
      helpOffset: rect(help).top - rect(panel).top, helpWidth: rect(help).width,
      artSize: rect(art)?.width, artOffset: art && rect(art).top - rect(panel).top,
      iconSize: rect(icon)?.width, iconAsset: icon?.querySelector('use')?.getAttribute('href')?.split('#').pop(),
      iconSurface: art && getComputedStyle(art).backgroundColor,
      actionWidth: rect(controls[0])?.width, actionHeight: rect(controls[0])?.height,
      actionOffset: controls.length && rect(controls[0]).top - rect(panel).top,
      referenceOffset: ref && rect(ref).top - rect(panel).top,
      subtitleSize: subtitle && parseFloat(getComputedStyle(subtitle).fontSize),
      subtitleTop: subtitle && rect(subtitle).top - rect(panel).top,
      footerClearance: rect(select('footer')).top - rect(panel).bottom,
      theme: document.documentElement.dataset.theme,
      fits: document.documentElement.scrollWidth === document.documentElement.clientWidth,
      layer: getComputedStyle(background).position,
      phaseA: getComputedStyle(background, '::before').backgroundImage.includes('-a.svg'),
      staticArt: getComputedStyle(background, '::before').animationName === 'none'
    };
  }, { isPrivacy, key: scenario.key });
  assert.equal(metrics.theme, theme);
  assert(metrics.fits, `${label} horizontal overflow`);
  assert.equal(metrics.layer, 'fixed');
  assert(metrics.phaseA && metrics.staticArt);
  assert.equal(await page.locator('.tb-member-dock').isVisible(), role === 'member' && !desktop);
  if (reference) {
    matched.push({ label, ...metrics });
    if (evidence) await page.screenshot({ path: path.join(evidence, `${label}.png`), animations: 'disabled' });
    if (isPrivacy) {
      expectGeometry(label, metrics, {
        panelWidth: phone ? 350 : 720, panelTop: phone ? 176 : 248, panelRadius: 18,
        titleTop: phone ? 100 : 150, titleSize: phone ? 36 : 48, titleLine: phone ? 55 : 73,
        subtitleSize: phone ? 16 : 18, subtitleTop: phone ? 25 : 28,
        helpSize: phone ? 15 : 17, helpLine: phone ? 23 : 26, helpOffset: phone ? 60 : 64,
        helpWidth: phone ? 310 : 652
      });
      expectGeometry(label, metrics, { panelHeight: phone ? 542 : 430 }, 3);
      assert(metrics.footerClearance >= 70, `${label}: policy panel needs space before the shared footer`);
    } else {
      expectGeometry(label, metrics, {
        panelWidth: phone ? 350 : 590, panelTop: phone ? 154 : 232, panelRadius: 20,
        artSize: 80, iconSize: 24, artOffset: phone ? 30 : 34, titleOffset: phone ? 131 : 136,
        titleSize: phone ? 29 : 40, titleLine: phone ? 39 : 54, helpSize: phone ? 15 : 17,
        helpLine: phone ? 20 : 23, helpWidth: phone ? 298 : 538, actionWidth: isError ? 136 : 166, actionHeight: 48
      });
      if (scenario === missing && metrics.iconAsset !== 'search') geometryFailures.push(`${label}: expected search glyph`);
      if (scenario === missing) expectGeometry(label, metrics, { panelHeight: phone ? 348 : 328, actionOffset: phone ? 251 : 265 });
      if (isError) {
        expectGeometry(label, metrics, {
          panelHeight: phone ? (role === 'member' ? 436 : 386) : (role === 'member' ? 386 : 352),
          actionOffset: phone ? 306 : 265
        });
        if (role === 'member') expectGeometry(label, metrics, { referenceOffset: phone ? 365 : 329 });
      }
    }
  }
  if (phone && height === 844) {
    const action = isPrivacy
      ? page.getByRole('contentinfo').getByRole('link', { name: 'Privacy', exact: true })
      : page.locator('main .tb-recovery-content__actions a').first();
    if (isPrivacy) await page.evaluate(() => scrollTo({ top: document.documentElement.scrollHeight, behavior: 'instant' }));
    await action.scrollIntoViewIfNeeded();
    assert(await action.evaluate(e => {
      const r = e.getBoundingClientRect();
      return r.height >= 44 && e.contains(document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2));
    }), `${label}: recovery/footer action is not tappable`);
    const navigation = page.waitForEvent('response', r => r.request().isNavigationRequest());
    await action.tap();
    const result = await navigation;
    assert.equal(new URL(result.url()).pathname, scenario.destination);
    assert.equal(result.request().method(), 'GET');
    assert.equal(result.status(), scenario === missing && role === 'guest' ? 302 : 200);
    if (scenario === missing && role === 'guest') await page.waitForURL('**/Account/Login?**');
    await page.waitForLoadState();
  }
  if (width === 895 && role === 'member') {
    // Preserve width (and hence the shell/breakpoint) while giving short pages room to scroll.
    await page.setViewportSize({ width, height: 350 });
    await verifyFixedArtwork(page, label);
  }
  assert.deepEqual(errors, [], label);
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const scenario of cases) for (const role of scenario.roles) for (const theme of ['light', 'dark']) {
      for (const [width, height, reference] of [[1440, 1040, true], [390, 844, true], [895, 768, false], [896, 768, false]]) {
        const context = await browser.newContext({ viewport: { width, height }, colorScheme: theme,
          hasTouch: width === 390, reducedMotion: 'reduce' });
        try { await verify(await context.newPage(), scenario, role, theme, width, height, reference); }
        finally { await context.close(); }
      }
    }
  } finally { await browser.close(); }
  if (evidence) fs.writeFileSync(path.join(evidence, errorHost ? 'error.json' : 'recovery-privacy.json'), JSON.stringify(matched, null, 2));
  assert.deepEqual(geometryFailures, []);
  console.log(`${matched.length} ${errorHost ? 'error' : 'recovery and Privacy'} reference captures; responsive, fixed Phase A pixel comparisons and touch navigation passed`);
})().catch(error => { console.error(error); process.exitCode = 1; });