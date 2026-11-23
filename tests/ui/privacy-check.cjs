// Runs against the fixture host in Recovery_pages_in_browser (TRACEBACK_BROWSER=1).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const { base, member } = JSON.parse(fs.readFileSync(0, 'utf8'));
const executablePath = process.env.CHROME_PATH || undefined;
const policy = [
  'TraceBack displays the item details you choose to publish, including its description, location, date, image, and contact preference. Keep personal contact details out of those public fields.',
  'Your account email is used to sign in and is not shown in public reports. Ownership claim messages are available only to authorized claim reviewers and the person who submitted the claim.',
  'If you need a report corrected or removed, sign in and manage it from your dashboard or contact your campus service desk.'
];
const footerLink = page => page.getByRole('contentinfo').getByRole('link', { name: 'Privacy', exact: true });

async function verify(page, theme, width, signedIn) {
  const response = await page.goto(`${base}/Home/Privacy`);
  assert.equal(response.status(), 200);
  assert.equal(response.request().redirectedFrom(), null);
  assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
  const main = page.getByRole('main');
  assert.equal(await main.getByRole('heading').count(), 1);
  assert.equal(await main.getByRole('heading', { level: 1 }).textContent(), 'Privacy');
  const article = main.getByRole('article');
  assert.deepEqual(await article.locator('p').allTextContents(), policy);
  assert.equal(await article.locator('strong').count(), 3);
  assert.equal(await article.locator('a, button, [tabindex]').count(), 0);
  assert.equal(await footerLink(page).getAttribute('aria-current'), 'page');
  const art = page.locator('[data-route-art=privacy]');
  assert.equal(await art.getAttribute('aria-hidden'), 'true');
  await art.evaluate((e, color) => {
    const style = getComputedStyle(e, '::before');
    if (!style.backgroundImage.includes(`-${color}-a.svg`) || style.animationName !== 'none') {
      throw new Error(`Expected static ${color} boundary artwork`);
    }
  }, theme);

  const metrics = await page.evaluate(() => {
    const rect = selector => document.querySelector(selector).getBoundingClientRect().toJSON();
    return { article: rect('article'), header: rect('.site-header'), footer: rect('footer'),
      last: rect('article .tb-privacy__entry:last-child p'), width: document.documentElement.clientWidth,
      scrollWidth: document.documentElement.scrollWidth };
  });
  assert.equal(metrics.width, width);
  assert.equal(metrics.scrollWidth, width, 'Privacy layout must reflow without horizontal scroll');
  assert(Math.abs((metrics.article.left + metrics.article.right) / 2 - width / 2) < 1);
  if (width === 1440 || width === 390) assert.equal(metrics.article.width, width === 1440 ? 720 : 350);
  assert(metrics.article.top > metrics.header.bottom);
  assert(metrics.footer.top - metrics.last.bottom >= 32, 'Last paragraph needs space before the footer');
  const screenshot = async (suffix = '') => {
    if (process.env.PRIVACY_SCREENSHOTS && (width === 390 || width === 1440)) {
      await page.screenshot({ path: path.join(process.env.PRIVACY_SCREENSHOTS,
        `privacy-${signedIn ? 'member' : 'guest'}-${theme}-${width}${suffix}.png`) });
    }
  };
  await screenshot();

  await page.keyboard.press('Tab');
  assert(await page.getByRole('link', { name: 'Skip to content' }).evaluate(e => e === document.activeElement));
  await page.keyboard.press('Enter');
  assert(await main.evaluate(e => e === document.activeElement));
  await page.keyboard.press('Tab');
  const link = footerLink(page);
  assert(await link.evaluate(e => e === document.activeElement), 'Tab from main should reach the footer');
  await page.keyboard.press('End');
  await page.waitForFunction(() => innerHeight + scrollY >= document.documentElement.scrollHeight - 1);
  const dock = page.locator('.tb-member-dock');
  const dockVisible = await dock.isVisible();
  assert.equal(dockVisible, signedIn && width < 896);
  const upper = (await page.getByRole('banner').boundingBox()).y + metrics.header.height;
  const lower = dockVisible ? (await dock.boundingBox()).y : await page.evaluate(() => innerHeight);
  const clearsShell = async element => {
    const { y, height } = await element.boundingBox();
    return y >= upper && y + height <= lower;
  };
  assert(await clearsShell(link), 'Footer link must clear the floating shell at End');
  if (width === 390 && signedIn) await screenshot('-end');

  // At 200% zoom, the final paragraph and footer need not fit onscreen together.
  // Scroll back using the keyboard and verify the whole paragraph is readable.
  const finalParagraph = article.locator('p').last();
  for (let i = 0; i < 12 && !await clearsShell(finalParagraph); i++) {
    const previousY = await page.evaluate(() => scrollY);
    if (previousY === 0) break;
    const scrollEnd = page.evaluate(() => new Promise(resolve =>
      document.addEventListener('scrollend', resolve, { once: true })));
    await page.keyboard.press('ArrowUp');
    await scrollEnd;
  }
  assert(await clearsShell(finalParagraph), `Last paragraph at ${width}px (${theme}, member=${signedIn}): ${JSON.stringify({
    paragraph: await finalParagraph.boundingBox(), upper, lower })}`);
}

async function fromSharedFooter(page) {
  assert.equal((await page.goto(`${base}/`)).status(), 200);
  const link = footerLink(page);
  assert.equal(await link.getAttribute('href'), '/Home/Privacy');
  await link.focus();
  await page.keyboard.press('Enter');
  await page.waitForURL(`${base}/Home/Privacy`);
  assert(await page.getByRole('heading', { level: 1, name: 'Privacy', exact: true }).isVisible());
}

(async () => {
  const browser = await chromium.launch({ executablePath });
  try {
    for (const theme of ['light', 'dark']) {
      for (const [width, height] of [[1440, 1040], [896, 768], [895, 768], [390, 844], [320, 600]]) {
        const context = await browser.newContext({ viewport: { width, height }, colorScheme: theme });
        try {
          const page = await context.newPage();
          page.setDefaultTimeout(5000);
          const errors = [];
          page.on('pageerror', error => errors.push(error.message));
          page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
          await verify(page, theme, width, false);
          if (width === 390 || width === 1440) await fromSharedFooter(page);
          await context.addCookies([{ ...member, url: base }]);
          await verify(page, theme, width, true);
          if (width === 390 || width === 1440) await fromSharedFooter(page);
          assert.deepEqual(errors, []);
          console.log(`Privacy ${theme} ${width}px: guest/member passed`);
        } finally { await context.close(); }
      }
    }
  } finally { await browser.close(); }

  const profile = fs.mkdtempSync(path.join(process.cwd(), 'src', 'TraceBack', 'bin', 'privacy-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath, channel: executablePath ? undefined : 'chromium',
      viewport: { width: 640, height: 900 }, deviceScaleFactor: 1
    });
    try {
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      for (const signedIn of [false, true]) {
        if (signedIn) await context.addCookies([{ ...member, url: base }]);
        for (const theme of ['light', 'dark']) {
          await page.emulateMedia({ colorScheme: theme });
          await verify(page, theme, 320, signedIn);
          assert.deepEqual(await page.evaluate(() => [innerWidth, innerHeight, devicePixelRatio]), [320, 450, 2]);
          console.log(`Privacy ${theme}: native 200% zoom at 640px (320 CSS px), ${signedIn ? 'member' : 'guest'} passed`);
        }
      }
    } finally { await context.close(); }
  } finally { fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); }
})().catch(error => { console.error(error); process.exitCode = 1; });
