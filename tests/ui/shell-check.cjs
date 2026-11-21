// Exercised by ReportAuthorizationTests.Shared_shell_in_browser (TRACEBACK_BROWSER=1).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const { base, member, admin } = JSON.parse(fs.readFileSync(0, 'utf8'));
const exact = name => ({ name, exact: true });
const panel = (page, name) => page.locator('details').filter({ has: page.locator(`summary[aria-label="${name}"]`) });
const signedIn = async (context, cookie) => context.addCookies([{ ...cookie, url: base }]);
const focusIs = (element) => element.evaluate(e => e === document.activeElement);
const current = async (nav, name, value) => assert.equal(await nav.getByRole('link', exact(name)).getAttribute('aria-current'), value);
const screenshot = async (page, name) => {
  if (process.env.SHELL_SCREENSHOTS) await page.screenshot({ path: path.join(process.env.SHELL_SCREENSHOTS, `${name}.png`) });
};

async function verifyGeometry(page, width, dock) {
  await page.evaluate(() => window.scrollTo(0, 0));
  const geometry = await page.evaluate(() => {
    const header = document.querySelector('.site-header');
    const dock = document.querySelector('.tb-member-dock');
    const rect = e => e.getBoundingClientRect().toJSON();
    const elements = [...document.querySelectorAll('.site-header a, .site-header summary, .site-header button, .tb-member-dock a')]
      .filter(element => element.checkVisibility({ contentVisibilityAuto: true }) && !element.closest('details:not([open]) :not(summary, summary *)'))
      .map(rect);
    return { width: document.documentElement.scrollWidth, viewport: document.documentElement.clientWidth,
      header: rect(header), main: rect(document.querySelector('main')),
      dock: dock && getComputedStyle(dock).display !== 'none' ? rect(dock) : null, elements };
  });
  assert.equal(geometry.width, geometry.viewport, 'Horizontal overflow');
  assert(geometry.header.bottom < geometry.main.top, 'Header overlaps the start of the document');
  assert.equal(geometry.header.height, width >= 896 ? 72 : 60);
  if (width === 1440) assert.equal(geometry.header.width, 1248);
  if (width === 390) assert.equal(geometry.header.width, 366);
  assert.equal(!!geometry.dock, dock);
  if (dock && width === 390) assert.deepEqual([geometry.dock.width, geometry.dock.height], [350, 64]);
  for (const target of geometry.elements) {
    assert(target.width >= 44 && target.height >= 44, `Small touch target ${JSON.stringify(target)}`);
    assert(target.left >= 0 && target.right <= geometry.viewport, 'Control offscreen');
  }
  const privacy = page.getByRole('contentinfo').getByRole('link', exact('Privacy'));
  await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
  const footerBox = await privacy.boundingBox();
  assert(footerBox.y >= 0 && footerBox.y + footerBox.height <= (geometry.dock?.top ?? page.viewportSize().height), 'Footer obscured');
  assert.equal(await page.locator('footer').evaluate(e => getComputedStyle(e).position), 'static');
  assert.equal(await page.locator('.site-header').evaluate(e => e.getBoundingClientRect().top), geometry.header.top);
  await page.evaluate(() => window.scrollTo(0, 0));
}

async function exerciseDisclosures(page, mobile) {
  const appearance = panel(page, 'Appearance');
  const trigger = appearance.locator('summary');
  const cdp = await page.context().newCDPSession(page);
  const appearanceNode = async () => (await cdp.send('Accessibility.getFullAXTree')).nodes.find(node =>
    node.name?.value === 'Appearance' && node.properties?.some(property => property.name === 'expanded'));
  assert(await appearanceNode(), 'Appearance should have an accessible name and expanded state');
  await trigger.focus();
  await page.keyboard.press('Enter');
  assert((await appearanceNode()).properties.find(property => property.name === 'expanded').value.value);
  await cdp.detach();
  await page.keyboard.press('Tab');
  assert(await focusIs(page.getByRole('radio', exact('System'))));
  await page.keyboard.press('ArrowDown');
  assert(await page.getByRole('radio', exact('Light')).isChecked());
  await page.keyboard.press('Escape');
  assert(!await appearance.evaluate(e => e.open));
  assert(await focusIs(trigger));
  await trigger.click();
  const outside = page.getByRole('heading', { level: 1 });
  await outside.click({ position: { x: 4, y: 4 } });
  assert(!await appearance.evaluate(e => e.open));
  assert(await focusIs(trigger));
  await trigger.click();
  const search = page.locator('main input[type="search"], main input[name="Query"]').first();
  await search.click({ position: { x: 6, y: 10 } });
  assert(!await appearance.evaluate(e => e.open));
  assert(await focusIs(search), 'Outside controls should retain their own focus');
  if (mobile) {
    const menu = panel(page, 'Menu');
    await trigger.click();
    await menu.locator('summary').click();
    assert(await menu.evaluate(e => e.open));
    assert(!await appearance.evaluate(e => e.open));
    await page.keyboard.press('Tab');
    assert(await focusIs(menu.getByRole('button', exact('Sign out'))));
    await page.keyboard.press('Escape');
    assert(!await menu.evaluate(e => e.open));
    assert(await focusIs(menu.locator('summary')));
  }
}

async function verifyNativeZoom() {
  // A disposable profile isolates Chromium's real page-zoom setting from the user.
  const bin = path.join(process.cwd(), 'src', 'TraceBack', 'bin');
  fs.mkdirSync(bin, { recursive: true });
  const profile = fs.mkdtempSync(path.join(bin, 'shell-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath: process.env.CHROME_PATH || undefined,
      channel: process.env.CHROME_PATH ? undefined : 'chromium',
      viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1, reducedMotion: 'reduce'
    });
    try {
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      await signedIn(context, member);
      for (const colorScheme of ['light', 'dark']) {
        await page.emulateMedia({ colorScheme });
        for (const width of [1440, 640]) {
          await page.setViewportSize({ width, height: 900 });
          await page.goto(`${base}/Home/Privacy`);
          assert.deepEqual(await page.evaluate(() => [innerWidth, innerHeight, devicePixelRatio]), [width / 2, 450, 2]);
          await verifyGeometry(page, width / 2, true);
          await panel(page, 'Appearance').locator('summary').click();
          assert(await page.getByRole('radio', exact('System')).isChecked());
          await screenshot(page, `zoom-${colorScheme}-${width}`);
          console.log(`${colorScheme}: native 200% zoom at ${width}px (${width / 2} CSS px) passed`);
        }
      }
    } finally { await context.close(); }
  } finally { fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); }
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const colorScheme of ['light', 'dark']) {
      for (const [width, height] of [[1440, 900], [1024, 768], [896, 768], [895, 768], [390, 844]]) {
        const mobile = width < 896;
        const context = await browser.newContext({ viewport: { width, height }, colorScheme, reducedMotion: 'reduce' });
        try {
          const page = await context.newPage();
          page.setDefaultTimeout(5000);
          const errors = [];
          page.on('pageerror', error => errors.push(error.message));
          assert.equal((await page.goto(`${base}/Account/Login`)).status(), 200);
          const menu = panel(page, 'Menu');
          if (mobile) await menu.locator('summary').click();
          const actions = mobile ? menu : page.getByRole('banner');
          assert.equal(await actions.getByRole('link', exact('Browse')).getAttribute('href'), '/Items');
          await current(actions, 'Sign in', 'page');
          assert(await actions.getByRole('link', exact('Create account')).isVisible());
          if (width === 390 || width === 1440) await screenshot(page, `guest-${colorScheme}-${width}`);
          if (mobile) await menu.locator('summary').click();
          await verifyGeometry(page, width, false);

          await signedIn(context, member);
          assert.equal((await page.goto(`${base}/Items?Query=backpack`)).status(), 200);
          const nav = page.getByRole('navigation', exact('Member navigation'));
          assert.equal(await nav.count(), 1, 'Only one responsive navigation should be exposed');
          await current(nav, 'Browse', 'page');
          assert.equal(await nav.getByRole('link', exact('My dashboard')).getAttribute('href'), '/Dashboard');
          const report = nav.getByRole('link', exact('Report an item'));
          assert.equal(await report.getAttribute('href'), '/Items/Create');
          if (mobile) assert.equal(await nav.locator('a > svg[aria-hidden=true]').count(), 3);
          assert.equal(await page.getByRole('banner').locator('a[href="/Admin"]').count(), 0);
          await verifyGeometry(page, width, mobile);
          if (width === 390 || width === 1440) await screenshot(page, `member-${colorScheme}-${width}`);
          if (width === 390 || width === 1440) await exerciseDisclosures(page, mobile);
          await report.click();
          await page.waitForURL('**/Items/Create');
          await current(nav, 'Report an item', 'page');
          assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);

          if (width === 390) {
            // The dedicated claims drawer ships separately; exercise Bootstrap's actual offcanvas layers.
            await page.evaluate(() => {
              const drawer = document.createElement('div');
              drawer.className = 'offcanvas offcanvas-end';
              drawer.id = 'layer-probe';
              drawer.tabIndex = -1;
              document.body.append(drawer);
              window.bootstrap.Offcanvas.getOrCreateInstance(drawer).show();
            });
            await page.waitForFunction(() => document.querySelector('#layer-probe').classList.contains('show'));
            for (const shell of ['.site-header', '.tb-member-dock']) {
              assert(await page.locator(shell).evaluate(e => {
                const r = e.getBoundingClientRect();
                return !!document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2)
                  ?.closest('#layer-probe, .offcanvas-backdrop');
              }));
            }
            await page.evaluate(() => window.bootstrap.Offcanvas.getInstance('#layer-probe').hide());
            await page.goto(`${base}/Home/Privacy`);
            const cdp = await context.newCDPSession(page);
            await cdp.send('Emulation.setEmulatedMedia', { features: [
              { name: 'prefers-reduced-transparency', value: 'reduce' },
              { name: 'prefers-color-scheme', value: colorScheme },
              { name: 'prefers-reduced-motion', value: 'reduce' }
            ] });
            await page.waitForFunction(() => matchMedia('(prefers-reduced-transparency: reduce)').matches);
            for (const shell of ['.site-header', '.tb-member-dock']) {
              const style = await page.locator(shell).evaluate(e => [getComputedStyle(e).backgroundColor, getComputedStyle(e).backdropFilter]);
              assert(!style[0].startsWith('rgba') && !style[0].startsWith('color('), `Expected opaque surface, got ${style[0]}`);
              assert.equal(style[1], 'none');
            }
            await cdp.send('Emulation.setSafeAreaInsetsOverride', { insets: { top: 27, right: 25, bottom: 34, left: 28 } });
            const header = await page.locator('.site-header').boundingBox();
            const dock = await page.locator('.tb-member-dock').boundingBox();
            assert.deepEqual([header.x, header.y, height - dock.y - dock.height], [28, 39, 46]);
            await current(page.getByRole('contentinfo'), 'Privacy', 'page');
            await cdp.detach();
          }

          await signedIn(context, admin);
          await page.goto(`${base}/Home/Privacy`);
          if (mobile) await menu.locator('summary').click();
          assert.equal(await actions.getByRole('link', exact('Admin')).getAttribute('href'), '/Admin');
          const logout = actions.getByRole('button', exact('Sign out'));
          assert.deepEqual(await logout.evaluate(button => [button.form.method, button.form.querySelector('[name=__RequestVerificationToken]').value.length > 0]), ['post', true]);
          if (width === 390 || width === 1440) {
            await screenshot(page, `admin-${colorScheme}-${width}`);
            await logout.click();
            await page.waitForURL(`${base}/`);
            await page.waitForLoadState('domcontentloaded');
            assert.equal(await nav.count(), 0);
          }
          assert.deepEqual(errors, []);
          console.log(`${colorScheme}: guest/member/admin at ${width}x${height} passed`);
        } finally { await context.close(); }
      }
    }
    const noJs = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 844 } });
    await noJs.goto(`${base}/Account/Login`);
    await panel(noJs, 'Menu').locator('summary').click();
    assert(await panel(noJs, 'Menu').getByRole('link', exact('Create account')).isVisible());
    assert(!await panel(noJs, 'Appearance').isVisible());
    console.log('Native mobile menu works without JavaScript');
  } finally { await browser.close(); }
  await verifyNativeZoom();
})().catch(error => { console.error(error); process.exitCode = 1; });
