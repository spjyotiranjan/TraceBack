// Real Razor app on a disposable loopback host (ReportAuthorizationTests).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL;
const [firstId] = process.env.TRACEBACK_TEST_ITEM_IDS.split(',');
const cookie = process.env.TRACEBACK_TEST_COOKIE;
const [name, value] = [cookie.slice(0, cookie.indexOf('=')), cookie.slice(cookie.indexOf('=') + 1)];
const evidence = process.env.ART_SCREENSHOTS;
const cases = [
  ['guest-home', '/', 'home-guest', 200, false],
  ['guest-login', '/Account/Login', 'auth', 200, false],
  ['guest-register', '/Account/Register', 'auth', 200, false],
  ['guest-denied', '/Account/Denied', 'denied', 200, false],
  ['guest-error', '/Home/Error', 'error', 200, false],
  ['guest-missing', '/no-such-route/parity-fixture', 'not-found', 404, false],
  ['guest-privacy', '/Home/Privacy', 'privacy', 200, false],
  ['member-home', '/', 'home-member', 200, true],
  ['member-browse', '/Items', 'browse', 200, true],
  ['member-details', `/Items/Details/${firstId}`, 'details', 200, true],
  ['member-create', '/Items/Create', 'editor', 200, true],
  ['member-edit', `/Items/Edit/${firstId}`, 'editor', 200, true],
  ['member-dashboard', '/Dashboard', 'dashboard', 200, true],
];
const save = (label, buffer) => evidence && fs.writeFileSync(path.join(evidence, `${label}.png`), buffer);
const capture = page => page.screenshot({ animations: 'disabled', caret: 'hide' });
const isolation = 'body > :not([data-route-art]) { opacity: 0 !important; }';
const artCrop = (page, animations = 'disabled') => page.screenshot({ style: isolation, animations, caret: 'hide' });

async function checkScroll(page, label) {
  const heading = page.getByRole('heading', { level: 1 }).first();
  // Use a short viewport for intrinsically short pages. Keep the same width, theme,
  // layout, viewport height and art pose for both scroll samples.
  const room = () => page.evaluate(() => document.scrollingElement.scrollHeight - innerHeight);
  if (await room() < 80) await page.setViewportSize({ width: page.viewportSize().width, height: 420 });
  if (await room() < 80) await page.setViewportSize({ width: page.viewportSize().width, height: 280 });
  const before = await artCrop(page);
  const blank = await page.screenshot({ style: `${isolation} [data-route-art] { opacity: 0 !important; }`, animations: 'disabled' });
  assert(!before.equals(blank), `${label}: route art did not paint`);
  const y = await heading.evaluate(el => el.getBoundingClientRect().y);
  if (evidence) save(`${label}-content-a0`, await capture(page));
  const beforeTop = await page.locator('[data-route-art]').evaluate(el => el.getBoundingClientRect().top);
  await page.evaluate(() => scrollTo({top: 220, behavior: 'instant'}));
  const scrolled = await page.evaluate(() => scrollY);
  assert(scrolled >= 50, `${label}: insufficient natural scrolling room (${scrolled}px)`);
  const moved = y - await heading.evaluate(el => el.getBoundingClientRect().y);
  assert(Math.abs(moved - scrolled) < 1, `${label}: content did not move by ${scrolled}px (moved ${moved}px)`);
  const after = await artCrop(page);
  const afterTop = await page.locator('[data-route-art]').evaluate(el => el.getBoundingClientRect().top);
  const same = before.equals(after);
  save(`${label}-scroll-a0`, before);
  save(`${label}-scroll-a1`, after);
  if (evidence) save(`${label}-content-scrolled`, await capture(page));
  return { label, viewport: page.viewportSize(), moved, top: [beforeTop, afterTop], same };
}

async function checkMotion(page, label, regressions) {
  // Capture the fully settled pose before starting the compositor animation.
  // Seeking an active pseudo-element animation to t=0 can read opacity 0
  // from style before the compositor has discarded its previous frame.
  const start = await artCrop(page);
  // Reload to start the real CSS animations, then seek both pseudo-elements to
  // exact times on the same timeline; do not approximate phases with timeouts.
  await page.emulateMedia({ reducedMotion: 'no-preference' });
  assert.equal((await page.reload()).status(), 200);
  await page.locator('[data-route-art]').evaluate(async layer => {
    for (const pseudo of ['::before', '::after']) {
      const source = getComputedStyle(layer, pseudo).backgroundImage.match(/^url\("(.+)"\)$/)[1];
      const image = new Image();
      image.src = source;
      await image.decode();
    }
  });
  const timeline = await page.evaluateHandle(async () => {
    const animations = document.getAnimations().filter(animation => animation.animationName?.startsWith('tb-route-phase-'));
    if (animations.length !== 2) throw new Error(`Expected two route animations, got ${animations.length}`);
    for (const animation of animations) animation.pause();
    await Promise.all(animations.map(animation => animation.ready));
    return animations;
  });
  const phase = fraction => timeline.evaluate(async (animations, fraction) => {
    const layer = document.querySelector('[data-route-art]');
    for (const animation of animations) animation.currentTime = animation.effect.getTiming().duration * fraction;
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    return ['::before', '::after'].map(pseudo => Number(getComputedStyle(layer, pseudo).opacity));
  }, fraction);
  assert.deepEqual(await phase(0), [1, 0]);
  const [, rising] = await phase(.25);
  const [, peak] = await phase(.5);
  assert(rising > .2 && rising < .55 && peak > .72 && peak < .85, `${label}: abrupt or imperceptible crossfade`);
  const half = await artCrop(page, 'allow');
  assert(!start.equals(half), `${label}: no visible animation between A and B`);
  save(`${label}-motion-a`, start);
  save(`${label}-motion-peak`, half);
  const [, falling] = await phase(.75);
  assert(Math.abs(falling - rising) < .001);
  assert.deepEqual(await phase(1), [1, 0]);
  const end = await artCrop(page, 'allow');
  assert(start.equals(end), `${label}: the cycle boundary must match the resting pose`);
  save(`${label}-motion-end`, end);
  const repeats = await timeline.evaluate(animations => animations.every(animation => animation.effect.getTiming().iterations === Infinity));
  const laterPeak = await phase(3.5);
  if (!repeats || Math.abs(laterPeak[1] - peak) >= .001 || Math.abs(laterPeak[0] + laterPeak[1] - 1) >= .001) {
    regressions.push(`${label}: route crossfade does not loop continuously (${laterPeak})`);
  }
  const seam = [await phase(.99), await phase(1.01)];
  assert(seam.every(([a, b]) => 1 - a < .01 && b < .01), `${label}: crossfade must ease through the loop seam`);
  await timeline.dispose();
  await page.emulateMedia({ reducedMotion: 'reduce' });
  assert.equal(await page.locator('[data-route-art]').evaluate(el => getComputedStyle(el, '::after').display), 'none');
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  const results = [];
  const regressions = [];
  try {
    for (const colorScheme of ['light', 'dark']) {
      for (const [width, height] of [[1440, 900], [390, 844]]) {
        for (const member of [false, true]) {
          const context = await browser.newContext({ viewport: {width, height}, colorScheme,
            reducedMotion: 'reduce', hasTouch: width === 390 });
          try {
            if (member) await context.addCookies([{name, value, url: base}]);
            // Do not send test addresses/coordinates to the network.
            await context.route('**/*', route => route.request().url().startsWith(`${base}/`) ? route.continue() : route.abort());
            const page = await context.newPage();
            page.setDefaultTimeout(5000);
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            page.on('response', response => {
              if (response.status() >= 400 && response.request().resourceType() !== 'document')
                errors.push(`${response.status()} ${new URL(response.url()).pathname}`);
            });
            for (const [key, href, variant, status] of cases.filter(test => test[4] === member)) {
              await page.setViewportSize({width, height});
              const response = await page.goto(`${base}${href}`);
              assert.equal(response.status(), status, key);
              const artwork = page.locator('[data-route-art]');
              assert.equal(await artwork.getAttribute('data-route-art'), variant);
              assert.equal(await artwork.getAttribute('aria-hidden'), 'true');
              const style = await artwork.evaluate(el => ({
                a: getComputedStyle(el, '::before').backgroundImage,
                b: getComputedStyle(el, '::after').display,
                pointer: getComputedStyle(el).pointerEvents,
                z: getComputedStyle(el).zIndex,
              }));
              assert(style.a.includes(`${variant}-${width === 390 ? 'phone' : 'desktop'}-${colorScheme}-a.svg`));
              assert.deepEqual([style.b, style.pointer, style.z], ['none', 'none', '-1']);
              const label = `${key}-${colorScheme}-${width}`;
              const footer = page.getByRole('contentinfo');
              assert(await footer.getByText('TraceBack helps campus belongings find their way home.', { exact: true }).isVisible());
              assert(await footer.getByRole('link', { name: 'Privacy' }).isVisible());
              const footerPaint = await footer.evaluate(el => {
                const style = getComputedStyle(el);
                return [style.backgroundColor, style.backgroundImage, style.borderTopWidth];
              });
              const [color, image, border] = footerPaint;
              if (color !== 'rgba(0, 0, 0, 0)' || image !== 'none' || border !== '0px') {
                regressions.push(`${label}: footer paints ${footerPaint.join(' ')}`);
              }
              if (evidence) save(`${label}-rest`, await capture(page));
              results.push(await checkScroll(page, label));
              if (['guest-home', 'member-edit'].includes(key)) await checkMotion(page, label, regressions);
            }
            assert.deepEqual(errors, []);
          } finally { await context.close(); }
        }
      }
    }
  } finally { await browser.close(); }
  if (evidence) fs.writeFileSync(path.join(evidence, 'scroll-metrics.json'), JSON.stringify(results, null, 2));
  console.log(`Measured ${results.length} matched viewport captures with moving content`);
  const mismatches = results.filter(r => !r.same || r.top.some(y => y !== 0));
  assert.deepEqual(mismatches, [], 'The art/gradient pixels or layer geometry moved with the document');
  assert.deepEqual(regressions, [], 'Shared footer and route motion regressions');
})().catch(error => { console.error(error); process.exitCode = 1; });
