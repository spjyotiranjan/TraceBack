// Run against the local Release app; Playwright must be available via NODE_PATH.
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL || 'http://127.0.0.1:5188';
const cases = [
  ['/', 'home-guest', 200], ['/Account/Login', 'auth', 200],
  ['/Account/Denied', 'denied', 200], ['/no-such-route/art-fixture', 'not-found', 404],
  ['/Home/Error', 'error', 200], ['/Home/Privacy', 'privacy', 200],
];

async function inspect(page, route, mode, phone, staticPose) {
  const artwork = page.locator('[data-route-art]');
  const file = `${route}-${phone ? 'phone' : 'desktop'}-${mode}-a.svg`;
  // Browser style recalculation may trail a live media or theme change by a frame.
  await page.waitForFunction(file => getComputedStyle(document.querySelector('[data-route-art]'), '::before')
    .backgroundImage.includes(file), file, {timeout: 3000});
  const styles = await artwork.evaluate(el => {
    const a = getComputedStyle(el, '::before');
    const b = getComputedStyle(el, '::after');
    return { a: a.backgroundImage, b: b.backgroundImage, displayB: b.display,
      names: [a.animationName, b.animationName], iterations: [a.animationIterationCount, b.animationIterationCount],
      pointer: getComputedStyle(el).pointerEvents, position: getComputedStyle(el).position,
      z: getComputedStyle(el).zIndex, hidden: el.getAttribute('aria-hidden'), children: el.childElementCount,
      width: document.documentElement.scrollWidth,
      theme: document.documentElement.dataset.theme,
      canvas: getComputedStyle(document.body).backgroundColor,
      value: getComputedStyle(el).getPropertyValue('--tb-route-art-a') };
  });
  assert(styles.a.includes(file), `${JSON.stringify(styles)}: expected ${file}`);
  const response = await page.request.get(`${base}/art/routes/${file}`);
  assert.equal(response.status(), 200);
  assert.match(response.headers()['content-type'], /image\/svg\+xml/);
  assert.deepEqual([styles.pointer, styles.position, styles.z, styles.hidden, styles.children],
    ['none', 'fixed', '-1', 'true', 0]);
  assert.equal(styles.width, page.viewportSize().width);
  if (staticPose) {
    assert.deepEqual(styles.names, ['none', 'none']);
    assert.equal(styles.displayB, 'none');
  } else {
    assert(styles.b.includes(file.replace(/-a\.svg$/, '-b.svg')));
    assert.notEqual(styles.displayB, 'none');
    assert(styles.names.every(name => name !== 'none'));
    assert.deepEqual(styles.iterations, ['infinite', 'infinite']);
  }
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const width of [1440, 390]) {
      const phone = width === 390;
      const page = await browser.newPage({ viewport: {width, height: 900}, colorScheme: 'light', reducedMotion: 'no-preference' });
      const failures = [];
      page.on('response', response => {
        if (/\/art\//.test(response.url()) && !response.ok()) failures.push(response.url());
      });
      page.on('pageerror', error => failures.push(error.message));
      for (const [uri, variant, status] of cases) {
        await page.emulateMedia({colorScheme: 'light', reducedMotion: 'no-preference'});
        assert.equal((await page.goto(`${base}${uri}`)).status(), status);
        await inspect(page, variant, 'light', phone, !['auth','home-guest'].includes(variant));
        if (variant === 'home-guest') {
          const hero = page.locator('svg.tb-home-illustration');
          assert.equal(await hero.count(), 1);
          assert(await hero.evaluate(el => el.getBoundingClientRect().width > 0 && el.getBBox().height > 0 &&
            el.getAttribute('viewBox') === '0 0 480 280' && el.getAttribute('focusable') === 'false' &&
            !!el.closest('[aria-hidden="true"]')));
        }
        if (process.env.ART_SCREENSHOTS) await page.screenshot({path: `${process.env.ART_SCREENSHOTS}/${variant}-light-${width}.png`, animations: 'disabled'});
        await page.emulateMedia({colorScheme: 'dark', reducedMotion: 'reduce'});
        await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
        await inspect(page, variant, 'dark', phone, true);
        if (process.env.ART_SCREENSHOTS) await page.screenshot({path: `${process.env.ART_SCREENSHOTS}/${variant}-dark-${width}.png`});
        const main = await page.locator('main').ariaSnapshot();
        assert.match(main, /heading/);
        assert(!main.includes('img'));
      }
      // CSS-only fixtures cover all private choices without a browser login or database.
      for (const variant of ['home-member', 'browse', 'details', 'editor', 'dashboard']) {
        await page.locator('[data-route-art]').evaluate((el, name) => el.dataset.routeArt = name, variant);
        await inspect(page, variant, 'dark', phone, true);
        await page.emulateMedia({colorScheme: 'light', reducedMotion: 'no-preference'});
        await inspect(page, variant, 'light', phone, false);
        await page.emulateMedia({colorScheme: 'dark', reducedMotion: 'reduce'});
      }
      assert.deepEqual(failures, []);
      await page.close();
      const fallback = await browser.newPage({viewport: {width, height: 900}, colorScheme: 'dark', reducedMotion: 'reduce', javaScriptEnabled: false});
      const imageRequests = [];
      fallback.on('request', request => {
        if (request.resourceType() === 'image') imageRequests.push(request.url());
      });
      await fallback.goto(`${base}/Account/Login`);
      await inspect(fallback, 'auth', 'dark', phone, true);
      assert.equal(imageRequests.filter(url => url.includes('/art/routes/')).length, 1);
      assert(!imageRequests.some(url => /-b\.svg/.test(url)));
      await fallback.emulateMedia({forcedColors: 'active'});
      assert.equal(await fallback.locator('[data-route-art]').evaluate(el => getComputedStyle(el).display), 'none');
      await fallback.close();
      console.log(`Route artwork verified at ${width}px in both themes`);
    }
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
