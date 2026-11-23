const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const id = process.env.TRACEBACK_REPORT_ID;
const cookie = process.env.TRACEBACK_TEST_COOKIE;
const evidence = process.env.TRACEBACK_HOME_EVIDENCE;
const homeUrl = new URL('/?Query=stale&Status=Resolved&Page=7', base).href;

async function signIn(context, url = base, value = cookie) {
  const split = value.indexOf('=');
  await context.addCookies([{ name: value.slice(0, split), value: value.slice(split + 1), url }]);
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

async function checkHeroMotion(browser) {
  for (const width of [1440, 390]) {
    const page = await browser.newPage({ viewport: { width, height: width === 390 ? 844 : 760 } });
    try {
      await page.goto(homeUrl);
      const art = page.locator('.tb-home-illustration');
      const background = page.locator('[data-route-art]');
      const shot = () => art.screenshot({ style: '[data-route-art] { visibility: hidden }' });
      const first = await shot();
      await page.waitForTimeout(650);
      assert(!(await shot()).equals(first), 'the hero artwork must visibly animate');

      const motion = await art.evaluate(el => el.getAnimations({ subtree: true }).map(animation => {
        animation.pause();
        const { target } = animation.effect;
        const { endTime } = animation.effect.getComputedTiming();
        const read = time => {
          animation.currentTime = time;
          const style = getComputedStyle(target);
          const transform = new DOMMatrixReadOnly(style.transform === 'none' ? undefined : style.transform);
          return [transform.toString(), style.strokeDashoffset, style.opacity];
        };
        return { name: animation.animationName, endTime, start: read(0), middle: read(endTime * .42), end: read(5000) };
      }));
      const floats = motion.filter(entry => entry.name === 'home-hero-float');
      assert.equal(floats.length, 3);
      assert.equal(motion.filter(entry => entry.name === 'home-hero-pulse').length, 1);
      assert.equal(new Set(floats.map(entry => entry.middle[0])).size, 3);
      for (const entry of motion) {
        assert(entry.endTime <= 5000, 'the artwork settles within five seconds');
        assert.notDeepEqual(entry.middle, entry.start);
        assert.deepEqual(entry.end, entry.start);
      }

      await page.emulateMedia({ reducedMotion: 'reduce' });
      const resting = await shot();
      assert.equal(await art.evaluate(el => el.getAnimations({ subtree: true }).length), 0);
      await page.waitForTimeout(650);
      assert((await shot()).equals(resting), 'the reduced-motion SVG must be static');
      const before = (await art.boundingBox()).y;
      const backgroundTop = (await background.boundingBox()).y;
      const scroll = await page.evaluate(() => { scrollTo({ top: 200, behavior: 'instant' }); return scrollY; });
      assert(scroll >= 100 && Math.abs(before - (await art.boundingBox()).y - scroll) < 1);
      assert.equal((await background.boundingBox()).y, backgroundTop, 'only the route background stays pinned');
      assert.equal(await background.evaluate(el => getComputedStyle(el).position), 'fixed');

      console.log(`hero ${width}: embedded motion, static pose and distinct scroll anchoring passed`);
    } finally {
      await page.close();
    }
  }
}

async function run() {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  const deltas = [];
  const failures = [];
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
          const illustration = art.locator('.tb-home-illustration');
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
          assert.equal(await illustration.evaluate(el => el.getAnimations({ subtree: true }).length), 0);
          if (member) assert.equal(await media.locator('img').evaluate(img => img.complete), false);
          releaseArt();
          releasePhoto();
          await page.waitForLoadState('load');
          if (member) await media.locator('img').evaluate(img => img.decode());
          assert.deepEqual(await art.boundingBox(), a);
          assert.deepEqual(await intro.boundingBox(), i);
          if (member) assert.deepEqual(await media.boundingBox(), mediaBefore);
          assert.equal(artRequests.length, 1, 'reduced-motion Home loads only one route-art pose');
          assert.match(artRequests[0], new RegExp(`home-${role}-${width <= 599 ? 'phone' : 'desktop'}-${colorScheme}-a\\.svg$`));
          assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);
          assert.equal(await page.locator('.tb-member-dock').count(), member ? 1 : 0);
          assert.equal(await page.locator('.home-recent .tb-report-card').count(), member ? 6 : 0);
          if (member) {
            if (await page.locator('#process-title').textContent() !== 'Report. Search. Reunite.') failures.push(`${role} ${width}: use the short member guidance`);
          } else {
            assert.deepEqual(await page.locator('.process-steps h3').allTextContents(), ['Report', 'Search', 'Reunite']);
            for (const icon of await page.locator('.process-icon svg').all()) assert(await icon.isVisible());
            if (!(await page.locator('.process-icon use').last().getAttribute('href')).endsWith('#shield-check')) {
              failures.push(`guest ${width}: recovery guidance needs the shield-check icon`);
            }
          }
          if (evidence && [390, 1440].includes(width)) {
            await page.screenshot({ path: path.join(evidence, `${role}-${width}-${colorScheme}.png`), fullPage: true });
            deltas.push({ role, width, colorScheme, hero: await hero.boundingBox(), title: t, art: a, intro: i,
              actions: await hero.locator('.home-hero-actions').boundingBox(), recent: await page.locator('.home-recent').count() ?
                await page.locator('.home-recent').boundingBox() : null, guidance: await page.locator('.process-section').boundingBox() });
          }
          const presentation = await art.evaluate(el => {
            const style = getComputedStyle(el);
            return [style.borderTopWidth, style.backgroundImage, style.overflow, getComputedStyle(el.firstElementChild).overflow];
          });
          if (String(presentation) !== '0px,none,visible,visible') failures.push(`${role} ${width}: boxed or cropped art (${presentation})`);
          if (member) {
            if (!await card.evaluate(el => {
              const media = el.querySelector('.tb-report-card__media').getBoundingClientRect();
              const body = el.querySelector('.tb-report-card__body').getBoundingClientRect();
              return media.width === body.width && media.bottom <= body.top;
            })) failures.push(`member ${width}: recent card media should sit above its details`);
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
            await input.fill(colorScheme === 'light' ? 'navy & coral' : 'remove me');
            if (colorScheme === 'dark') {
              const clear = hero.getByRole('button', { name: 'Clear search words' });
              await (phone ? clear.tap() : clear.press('Enter'));
              assert.equal(await input.inputValue(), '');
              assert.equal(page.url(), homeUrl, 'clearing does not submit');
              assert(await input.evaluate(el => el === document.activeElement));
            }
            await Promise.all([page.waitForURL('**/Items?*'), phone ? hero.getByRole('button', {name: 'Search reports'}).tap() : input.press('Enter')]);
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
    for (const [suffix, state, status, heading, action] of [
      ['EMPTY', 'empty', 200, 'No active reports yet', 'Report an item'],
      ['FAILED', 'error', 503, 'Recent reports could not load', 'Retry']
    ]) {
      const url = process.env[`TRACEBACK_URL_${suffix}`];
      const target = new URL('/Home/Index?Query=stale', url).href;
      for (const [width, height] of [[1440, 1040], [390, 844]]) {
        for (const colorScheme of ['light', 'dark']) {
          const ctx = await browser.newContext({ viewport: { width, height }, colorScheme, reducedMotion: 'reduce', hasTouch: width === 390 });
          try {
            await signIn(ctx, url, process.env[`TRACEBACK_TEST_COOKIE_${suffix}`]);
            const page = await ctx.newPage();
            const errors = [];
            page.on('pageerror', error => errors.push(error.message));
            page.on('response', response => { if (!response.ok() && response.url() !== target) errors.push(response.url()); });
            assert.equal((await page.goto(target)).status(), status);
            assert.equal(await page.locator('html').getAttribute('data-theme'), colorScheme);
            const recent = page.getByRole('region', { name: 'Recent reports' });
            const title = recent.getByRole('heading', { name: heading });
            const cta = recent.getByRole('link', { name: action });
            assert(await title.isVisible());
            assert.equal(await recent.locator('.tb-report-card').count(), 0);
            assert.equal(await cta.getAttribute('href'), status === 503 ? '/Home/Index?Query=stale' : '/Items/Create');
            assert.equal(await recent.getByRole('link', {name: 'Browse all reports'}).getAttribute('href'), '/Items');
            assert.equal(await page.getByRole('searchbox').inputValue(), '');
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);
            if (evidence) {
              await page.evaluate(() => document.fonts.ready);
              const key = `member-${width}-${colorScheme}-${state}`;
              await page.screenshot({ path: path.join(evidence, `${key}.png`), fullPage: true });
              await title.evaluate(el => el.scrollIntoView({block: 'center'}));
              await page.screenshot({ path: path.join(evidence, `${key}-scrolled.png`) });
            }
            await cta.evaluate(el => el.scrollIntoView({block: 'center'}));
            await assertClear(cta);
            assert.deepEqual(errors, []);
            if (!await title.evaluate(el => el.tagName === 'H3' && getComputedStyle(el).textAlign === 'start')) {
              failures.push(`${state}: must be a nested, left-aligned recent status`);
            }
            console.log(`member ${width} ${colorScheme}: ${status} recent ${state}, search, Browse and ${action} passed`);
          } finally {
            await ctx.close();
          }
        }
      }
    }
    if (evidence) fs.writeFileSync(path.join(evidence, 'geometry.json'), JSON.stringify(deltas, null, 2));
    assert.deepEqual(failures, [], 'Home parity regressions');
    await checkHeroMotion(browser);
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
