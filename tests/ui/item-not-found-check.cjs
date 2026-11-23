// Run by ReportAuthorizationTests.Recovery_pages_in_browser (TRACEBACK_BROWSER=1).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const { base, member } = JSON.parse(fs.readFileSync(0, 'utf8'));
const missing = `${base}/Items/Details/000000000000000000000000`;
const executablePath = process.env.CHROME_PATH || undefined;

async function exercise(page, label, member, hasDock, screenshot = false) {
  page.setDefaultTimeout(8000);
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  const response = await page.goto(missing);
  assert.equal(response.status(), 404, label);
  assert.equal(new URL(page.url()).pathname, new URL(missing).pathname);
  const main = page.getByRole('main');
  const title = main.getByRole('heading', { name: 'Item not found', level: 1, exact: true });
  const browse = main.getByRole('link', { name: 'Browse items', exact: true });
  assert(await title.isVisible());
  assert.equal(await page.locator('[data-route-art="not-found"] [tabindex]').count(), 0);
  assert.equal(await page.locator('.tb-recovery-content__art--search').getAttribute('aria-hidden'), 'true');
  assert.equal(await browse.getAttribute('href'), '/Items');
  assert.equal(await page.locator('.tb-member-dock').isVisible(), hasDock);
  assert(await page.evaluate(() => document.documentElement.scrollWidth === document.documentElement.clientWidth), 'Horizontal overflow');
  if (screenshot && process.env.ITEM_RECOVERY_SCREENSHOTS) {
    await page.screenshot({ path: path.join(process.env.ITEM_RECOVERY_SCREENSHOTS, `${label}.png`) });
  }

  await page.keyboard.press('Tab');
  assert(await page.getByRole('link', { name: 'Skip to content' }).evaluate(e => e === document.activeElement));
  await page.keyboard.press('Enter');
  await page.keyboard.press('Tab');
  assert(await browse.evaluate(e => e === document.activeElement));
  const reachable = await browse.evaluate(e => {
    const rect = e.getBoundingClientRect();
    const target = document.elementFromPoint(rect.x + rect.width / 2, rect.y + rect.height / 2);
    return rect.height >= 44 && !!target && e.contains(target);
  });
  assert(reachable, `${label}: focused Browse target is obscured or too small`);
  const footer = page.getByRole('contentinfo').getByRole('link', { name: 'Privacy', exact: true });
  await page.keyboard.press('Tab');
  assert(await footer.evaluate(e => e === document.activeElement));
  await page.keyboard.press('End');
  await page.waitForFunction(() => innerHeight + scrollY >= document.documentElement.scrollHeight - 1);
  const privacy = page.waitForNavigation();
  await page.keyboard.press('Enter');
  assert.equal((await privacy).status(), 200);
  assert.equal(new URL(page.url()).pathname, '/Home/Privacy');

  assert.equal((await page.goto(missing)).status(), 404);
  await page.keyboard.press('Tab');
  await page.keyboard.press('Enter');
  await page.keyboard.press('Tab');
  assert(await browse.evaluate(e => e === document.activeElement));
  const navigation = page.waitForNavigation();
  await page.keyboard.press('Enter');
  const destination = await navigation;
  assert.equal(destination.status(), 200);
  assert.equal(new URL(page.url()).pathname, member ? '/Items' : '/Account/Login');
  if (!member) assert.equal(new URL(page.url()).searchParams.get('ReturnUrl'), '/Items');
  assert.deepEqual(errors, []);
  console.log(`${label}: HTTP 404, keyboard Privacy 200 and Browse ${member ? '200' : 'sign-in challenge'} passed`);
}

(async () => {
  const browser = await chromium.launch({ executablePath });
  try {
    for (const colorScheme of ['light', 'dark']) {
      for (const [width, height] of [[1440, 1040], [390, 844], [390, 340]]) {
        const context = await browser.newContext({ viewport: { width, height }, colorScheme, reducedMotion: 'reduce' });
        try {
          await exercise(await context.newPage(), `missing-guest-${colorScheme}-${width}x${height}`, false, false, height > 340);
          await context.addCookies([{ ...member, url: base }]);
          await exercise(await context.newPage(), `missing-member-${colorScheme}-${width}x${height}`, true, width < 896, height > 340);
        } finally { await context.close(); }
      }
    }
  } finally { await browser.close(); }

  const output = path.join(process.cwd(), 'src', 'TraceBack', 'bin');
  fs.mkdirSync(output, { recursive: true });
  const profile = fs.mkdtempSync(path.join(output, 'recovery-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath, channel: executablePath ? undefined : 'chromium',
      viewport: { width: 640, height: 800 }, deviceScaleFactor: 1, reducedMotion: 'reduce'
    });
    try {
      const settings = context.pages()[0];
      await settings.goto('chrome://settings/');
      await settings.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await settings.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      await context.addCookies([{ ...member, url: base }]);
      for (const colorScheme of ['light', 'dark']) {
        const page = await context.newPage();
        await page.emulateMedia({ colorScheme });
        await exercise(page, `missing-native-200-percent-${colorScheme}`, true, true);
        assert.deepEqual(await page.evaluate(() => [innerWidth, innerHeight, devicePixelRatio]), [320, 400, 2]);
        await page.close();
      }
    } finally { await context.close(); }
  } finally { fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); }
})().catch(error => { console.error(error); process.exitCode = 1; });
