const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const id = process.env.TRACEBACK_REPORT_ID;
const cookie = process.env.TRACEBACK_TEST_COOKIE;
const evidence = process.env.TRACEBACK_HOME_EVIDENCE;
const homeUrl = new URL('/?Query=stale&Status=Resolved&Page=7', base).href;

async function signIn(context) {
  const split = cookie.indexOf('=');
  await context.addCookies([{ name: cookie.slice(0, split), value: cookie.slice(split + 1), url: base }]);
}

async function assertClear(locator) {
  assert(await locator.evaluate(el => {
    const rect = el.getBoundingClientRect();
    const header = document.querySelector('.site-header').getBoundingClientRect();
    const dock = document.querySelector('.tb-member-dock')?.getBoundingClientRect();
    return rect.top >= header.bottom && rect.bottom <= (dock?.height ? dock.top : innerHeight)
      && el.contains(document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2));
  }), 'action must not be obscured by fixed navigation');
}

async function run() {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const [width, height, colorScheme] of [[1440, 1040, 'light'], [1440, 1040, 'dark'],
      [390, 844, 'light'], [390, 844, 'dark'], [895, 780, 'light'], [896, 780, 'dark'], [320, 520, 'dark']]) {
      for (const member of [false, true]) {
        const phone = width < 896;
        const role = member ? 'member' : 'guest';
        const ctx = await browser.newContext({ viewport: { width, height }, colorScheme, reducedMotion: 'reduce', hasTouch: phone });
        if (member) await signIn(ctx);
        const page = await ctx.newPage();
        page.setDefaultTimeout(7000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        page.on('response', res => { if (res.status() >= 400) errors.push(`${res.status()} ${res.url()}`); });
        let releaseArt;
        const artHeld = new Promise(resolve => { releaseArt = resolve; });
        const artRequests = [];
        await page.route('**/art/routes/home-*.svg', async route => {
          artRequests.push(new URL(route.request().url()).pathname);
          await artHeld;
          await route.continue();
        });
        await page.route(url => url.pathname === '/art/home-hero.svg', async route => { await artHeld; await route.continue(); });
        let releasePhoto;
        const photoHeld = new Promise(resolve => { releasePhoto = resolve; });
        await page.route(`**/Items/Image/${id}`, async route => { await photoHeld; await route.continue(); });
        try {
          assert.equal((await page.goto(homeUrl, { waitUntil: 'domcontentloaded' })).status(), 200);
          await page.evaluate(async () => {
            // Compare the boxes with the same loaded typeface before and after releasing the art.
            await document.fonts.load('400 18px Inter');
            await document.fonts.load('700 48px Inter');
            await document.fonts.ready;
          });
          const hero = page.locator('.home-hero');
          const background = page.locator(`[data-route-art="home-${role}"]`);
          const art = hero.locator('.home-hero-aside');
          const illustration = art.locator('img');
          const title = hero.getByRole('heading', { name: 'Find what matters.' });
          const intro = hero.locator('.home-hero-intro');
          const a = await art.boundingBox();
          const t = await title.boundingBox();
          const i = await intro.boundingBox();
          assert(a.height > 0 && a.width > 0, 'art must reserve space before it paints');
          if (phone) {
            assert(t.y + t.height <= a.y && a.y + a.height <= i.y, 'mobile order is headline → art → copy');
            assert.equal(await title.evaluate(el => getComputedStyle(el).textAlign), 'center');
          } else {
            assert(t.x + t.width <= a.x && i.x + i.width <= a.x, 'desktop art is in the right column');
          }
          assert(Math.abs(a.width / a.height - (phone ? 7 / 4 : 1)) < .02, 'hero frame uses the responsive crop');
          assert.equal(await background.evaluate(el => getComputedStyle(el, '::after').display), 'none');
          assert.equal(await background.evaluate(el => getComputedStyle(el, '::before').animationName), 'none');
          const card = page.locator('.home-recent .tb-report-card').first();
          const media = card.locator('.tb-report-card__media');
          const mediaBefore = member ? await media.boundingBox() : null;
          assert.equal(await illustration.evaluate(img => img.complete), false);
          if (member) assert.equal(await media.locator('img').evaluate(img => img.complete), false);
          releaseArt();
          releasePhoto();
          await page.waitForLoadState('load');
          await illustration.evaluate(img => img.decode());
          if (member) await media.locator('img').evaluate(img => img.decode());
          assert.deepEqual(await art.boundingBox(), a);
          assert.deepEqual(await intro.boundingBox(), i);
          if (member) assert.deepEqual(await media.boundingBox(), mediaBefore);
          assert.equal(artRequests.length, 1, 'reduced-motion Home loads only one route-art pose');
          assert.match(artRequests[0], new RegExp(`home-${role}-${width <= 599 ? 'phone' : 'desktop'}-${colorScheme}-a\\.svg$`));
          assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);
          assert.equal(await page.locator('.tb-member-dock').count(), member ? 1 : 0);
          assert.equal(await page.locator('.home-recent .tb-report-card').count(), member ? 6 : 0);
          assert.deepEqual(await page.locator('.process-steps h3').allTextContents(), ['Report', 'Search', 'Reunite']);
          for (const icon of await page.locator('.process-icon svg').all()) assert(await icon.isVisible());
          if (evidence && [390, 1440].includes(width)) {
            await page.screenshot({ path: path.join(evidence, `${role}-${width}-${colorScheme}.png`), fullPage: true });
          }
          if (member) {
            const form = hero.getByRole('search');
            const textbox = form.getByRole('searchbox');
            if (width === 390) {
              await assertClear(textbox);
              await assertClear(form.getByRole('button', { name: 'Search reports' }));
            }
            assert.equal(await textbox.inputValue(), '');
            assert.equal(await hero.getByRole('link', { name: 'Report an item' }).getAttribute('href'), '/Items/Create');
            const report = card.getByRole('link', { name: 'Blue backpack fixture' });
            assert.equal(await report.getAttribute('href'), `/Items/Details/${id}`);
            if (width === 390 && colorScheme === 'light') {
              await Promise.all([page.waitForURL(new URL(`/Items/Details/${id}`, base).href), report.tap()]);
              assert.equal(await page.getByRole('heading', { level: 1 }).textContent(), 'Blue backpack fixture');
              await page.goto(homeUrl);
            }
            await form.evaluate(el => el.scrollIntoView({ block: 'center' }));
            await assertClear(textbox);
            await assertClear(form.getByRole('button', { name: 'Search reports' }));
          } else {
            assert.equal(await hero.getByRole('search').count(), 0);
            assert.equal(await hero.getByRole('link', { name: 'Sign in' }).getAttribute('href'), '/Account/Login');
            assert.equal(await hero.getByRole('link', { name: 'Create account' }).getAttribute('href'), '/Account/Register');
            assert.equal(await page.locator('main a[href^="/Items"], main a[href^="/Dashboard"]').count(), 0);
          }
          await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
          await assertClear(page.getByRole('link', { name: 'Privacy' }));
          if (member && [390, 1440].includes(width)) {
            const input = hero.getByRole('searchbox');
            await input.fill(colorScheme === 'light' ? 'navy & coral' : '');
            await Promise.all([page.waitForURL('**/Items?*'), input.press('Enter')]);
            const query = new URL(page.url()).searchParams;
            assert.deepEqual([...query], [['Query', colorScheme === 'light' ? 'navy & coral' : '']]);
            assert.equal(await page.locator('select[name=Status]').inputValue(), 'Active');
          }
          assert.deepEqual(errors, []);
          console.log(`${role} ${width} ${colorScheme}: responsive layout, motion, media, shell and actions passed`);
        } finally {
          releaseArt();
          releasePhoto();
          await ctx.close();
        }
      }
    }
  } finally {
    await browser.close();
  }

  const bin = path.join(__dirname, '../../src/TraceBack/bin');
  const profile = fs.mkdtempSync(path.join(bin, 'home-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : { channel: 'chromium' }),
      viewport: { width: 780, height: 960 }, deviceScaleFactor: 1, reducedMotion: 'reduce'
    });
    try {
      await signIn(context);
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      await page.goto(homeUrl);
      assert.deepEqual(await page.evaluate(() => [innerWidth, devicePixelRatio, document.documentElement.scrollWidth]), [390, 2, 390]);
      const search = page.getByRole('search');
      const textbox = search.getByRole('searchbox');
      await search.evaluate(el => el.scrollIntoView({ block: 'center' }));
      await textbox.focus();
      await assertClear(textbox);
      await textbox.press('Tab');
      const submit = search.getByRole('button', { name: 'Search reports' });
      assert(await submit.evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
      await assertClear(submit);
      const privacy = page.getByRole('link', { name: 'Privacy' });
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await assertClear(privacy);
      console.log('member native 200% zoom: no overflow, keyboard focus, search and footer clearance passed');
    } finally {
      await context.close();
    }
  } finally {
    fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 200 });
  }
}

run().catch(error => { console.error(error); process.exitCode = 1; });
