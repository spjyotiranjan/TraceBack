// Run via SharedControlsBrowserTests against its loopback-only MVC fixture server.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL;

const luminance = rgb => rgb.match(/[\d.]+/g).slice(0, 3).reduce((sum, value, i) => {
  const c = Number(value) / 255;
  return sum + (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4) * [0.2126, 0.7152, 0.0722][i];
}, 0);
async function checkContrast(locator, surface = locator) {
  const ink = await locator.evaluate(e => getComputedStyle(e).color);
  const fill = await surface.evaluate(e => getComputedStyle(e).backgroundColor);
  assert.match(fill, /^rgb\(/, 'Artwork must not show through the text surface');
  const [low, high] = [luminance(ink), luminance(fill)].sort((a, b) => a - b);
  assert((high + 0.05) / (low + 0.05) >= 4.5, `${ink} against ${fill}`);
}

async function checkForm(page, route, width, theme) {
  const login = route === 'Login';
  const target = `${base}/Account/${route}${login ? '?returnUrl=%2FItems' : ''}`;
  assert.equal((await page.goto(target)).status(), 200);
  const title = page.getByRole('heading', { level: 1, name: login ? 'Sign in' : 'Create an account' });
  const intro = page.locator('.tb-auth__context');
  const form = page.getByRole('form', { name: login ? 'Sign in' : 'Create an account' });
  const [context, panel] = await Promise.all([title.boundingBox(), form.boundingBox()]);
  assert(context && panel, 'The page must expose a visible, named auth form');
  if (width >= 896) {
    assert(context.x + context.width <= panel.x);
    assert.equal(panel.width, 440);
  } else {
    assert(context.y + context.height <= panel.y);
    assert.equal(panel.width, Math.min(350, width - 40));
  }
  assert(await page.evaluate(() => document.documentElement.scrollWidth === innerWidth));
  const art = page.locator('[data-route-art="auth"]');
  assert.equal(await art.getAttribute('aria-hidden'), 'true');
  assert.match(await art.evaluate(e => getComputedStyle(e, '::before').backgroundImage), /auth-(desktop|phone)-(dark|light)-a\.svg/);
  assert.equal(await art.evaluate(e => getComputedStyle(e, '::before').animationName), 'none');
  assert.equal(await art.evaluate(e => getComputedStyle(e, '::after').display), 'none');
  await checkContrast(title, intro);
  await checkContrast(intro.locator('p'), intro);
  for (const label of await form.locator('label').all()) await checkContrast(label, form);
  assert.equal(await form.getAttribute('method'), 'post');
  assert.equal(await form.evaluate(e => e.action), target);
  assert.equal(await form.locator('input[name=__RequestVerificationToken]').count(), 1);

  const email = form.getByLabel('Email address');
  const password = form.getByLabel('Password', { exact: login });
  const submit = form.getByRole('button', { name: login ? 'Sign in' : 'Create account' });
  const alternate = page.locator('.tb-auth__alternate').getByRole('link');
  assert.equal(await alternate.getAttribute('href'), `/Account/${login ? 'Register' : 'Login'}`);
  await checkContrast(alternate, form);
  assert.equal(await email.getAttribute('autocomplete'), 'email');
  assert.equal(await password.getAttribute('autocomplete'), login ? 'current-password' : 'new-password');
  assert.equal(await password.getAttribute('type'), 'password');
  assert.equal(await password.getAttribute('value'), null);
  if (!login) assert.equal(await form.getByLabel('Full name').getAttribute('autocomplete'), 'name');
  await email.focus();
  await page.keyboard.press('Tab');
  assert(await password.evaluate(e => e === document.activeElement && getComputedStyle(e).outlineStyle === 'solid'));
  if (process.env.AUTH_SCREENSHOTS && [1440, 390].includes(width)) {
    await email.blur();
    await password.blur();
    await page.screenshot({ path: path.join(process.env.AUTH_SCREENSHOTS, `${route}-${theme}-${width}.png`), fullPage: true });
  }

  // The invalid client submission must remain on this page with every error and a usable summary.
  await submit.focus();
  await page.keyboard.press('Enter');
  const summary = form.locator('[data-tb-validation-summary]');
  await page.waitForFunction(() => document.activeElement.matches('[data-tb-validation-summary]'));
  assert.equal(await summary.getByRole('link').count(), login ? 2 : 3);
  assert.equal(await email.getAttribute('aria-invalid'), 'true');
  await summary.getByRole('link').last().click();
  assert(await password.evaluate(e => e === document.activeElement));
  if (width <= 390) {
    await page.setViewportSize({ width, height: 360 }); // Reduced space as with a software keyboard.
    const fixedHeader = await page.locator('.site-header').boundingBox();
    for (const control of [password, submit, alternate]) {
      await control.focus();
      await control.evaluate(e => e.scrollIntoView({ block: 'center' }));
      const box = await control.boundingBox();
      assert(box.y >= fixedHeader.y + fixedHeader.height && box.y + box.height <= 360);
    }
    assert(await page.evaluate(() => scrollY > 0 && document.documentElement.scrollWidth === innerWidth));
    await page.setViewportSize({ width, height: 844 });
  }
  if (process.env.AUTH_SCREENSHOTS && width === 390 && theme === 'dark' && !login) {
    await alternate.blur();
    await page.evaluate(() => scrollTo(0, 0));
    await page.screenshot({ path: path.join(process.env.AUTH_SCREENSHOTS, 'Register-validation-dark-390.png'), fullPage: true });
  }
  await alternate.click();
  assert.equal(new URL(page.url()).pathname, `/Account/${login ? 'Register' : 'Login'}`);
}

async function checkNativeZoom() {
  const bin = path.resolve(__dirname, '../../src/TraceBack/bin');
  await fs.mkdir(bin, { recursive: true });
  const profile = await fs.mkdtemp(path.join(bin, 'auth-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath: process.env.CHROME_PATH || undefined,
      channel: process.env.CHROME_PATH ? undefined : 'chromium',
      viewport: { width: 640, height: 840 }, deviceScaleFactor: 1, reducedMotion: 'reduce'
    });
    try {
      const page = context.pages()[0];
      page.setDefaultTimeout(5000);
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      for (const colorScheme of ['light', 'dark']) {
        await page.emulateMedia({ colorScheme });
        for (const route of ['Login', 'Register']) {
          assert.equal((await page.goto(`${base}/Account/${route}`)).status(), 200);
          assert.deepEqual(await page.evaluate(() => [innerWidth, devicePixelRatio, document.documentElement.scrollWidth]), [320, 2, 320]);
          const submit = page.locator('.tb-auth__submit button');
          await submit.click();
          const summary = page.locator('[data-tb-validation-summary]');
          assert.equal(await summary.getByRole('link').count(), route === 'Login' ? 2 : 3);
          await page.locator('.tb-auth__alternate a').click();
          assert.equal(new URL(page.url()).pathname, `/Account/${route === 'Login' ? 'Register' : 'Login'}`);
        }
      }
      console.log('Auth layout: both forms passed at native 200% Chromium zoom, light and dark');
    } finally { await context.close(); }
  } finally { await fs.rm(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); }
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const width of [1440, 896, 895, 390, 320]) {
      for (const theme of ['light', 'dark']) {
        const page = await browser.newPage({ viewport: { width, height: width >= 896 ? 1000 : 844 }, colorScheme: theme,
          reducedMotion: 'reduce', isMobile: width <= 390, hasTouch: width <= 390 });
        page.setDefaultTimeout(5000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        for (const route of ['Login', 'Register']) await checkForm(page, route, width, theme);
        assert.deepEqual(errors, []);
        await page.close();
        console.log(`Auth layout: both forms passed at ${width}px in ${theme}`);
      }
    }
  } finally { await browser.close(); }
  await checkNativeZoom();
})().catch(error => { console.error(error); process.exitCode = 1; });
