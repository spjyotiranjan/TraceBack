// Run against a local TraceBack instance with Playwright available via NODE_PATH.
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL || 'http://127.0.0.1:5187';
const palette = {
  light: ['#f2f4ff','#ffffff','#151932','#505a7b','#3447d8','#e76458','#cbd2eb','#eaeefa','#7b84a1'],
  dark: ['#111326','#1c203a','#f5f5ff','#bcc4e0','#aab6ff','#ff9b8f','#515c7d','#1c203a','#7f8bae'],
};
const roles = ['canvas','surface','ink','ink-muted','action','accent','divider','field','control-border'];
const luminance = rgb => rgb.match(/[\d.]+/g).map(Number).slice(0, 3).map(c => {
  c /= 255;
  return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
}).reduce((sum, c, i) => sum + c * [0.2126, 0.7152, 0.0722][i], 0);
const contrast = (a, b) => {
  const [low, high] = [luminance(a), luminance(b)].sort((x, y) => x - y);
  return (high + 0.05) / (low + 0.05);
};
const open = async page => {
  page.setDefaultTimeout(5000);
  assert.equal((await page.goto(`${base}/Account/Login`)).status(), 200);
};
async function verify(page, mode) {
  // The Appearance menu can close with the pointer over a field behind it.
  // Measure the resting border, not the hover or focus border.
  await page.mouse.move(0, 0);
  await page.locator('.form-control').first().blur();
  const data = await page.evaluate(roles => {
    const root = getComputedStyle(document.documentElement);
    const field = getComputedStyle(document.querySelector('.form-control'));
    const heading = getComputedStyle(document.querySelector('h1'));
    const statuses = ['active', 'claimed', 'resolved', 'pending', 'approved', 'rejected'].map(status => {
      const badge = document.createElement('span');
      badge.className = 'badge tb-status';
      badge.dataset.status = status;
      badge.textContent = status;
      document.body.append(badge);
      const styles = getComputedStyle(badge);
      const colors = [styles.color, styles.backgroundColor];
      badge.remove();
      return [status, ...colors];
    });
    return { colors: roles.map(r => root.getPropertyValue(`--tb-${r}`).trim().toLowerCase()),
      field: field.backgroundColor, border: field.borderTopColor, scheme: root.colorScheme,
      width: document.documentElement.scrollWidth, heading: [heading.fontSize, heading.lineHeight, heading.fontWeight],
      family: field.fontFamily, ink: field.color, statuses };
  }, roles);
  const rgb = hex => `rgb(${hex.slice(1).match(/../g).map(v => parseInt(v, 16)).join(', ')})`;
  assert.deepEqual(data.colors, palette[mode]);
  assert.equal(data.field, rgb(palette[mode][7]));
  assert.equal(data.border, rgb(palette[mode][8]));
  assert.equal(data.scheme, mode);
  assert.match(data.family, /Inter/);
  assert.deepEqual(data.heading, ['32px','40px','700']);
  assert.equal(data.width, page.viewportSize().width);
  assert(contrast(data.ink, data.field) >= 4.5, `${mode}: field text`);
  assert(contrast(data.border, data.field) >= 3, `${mode}: field border`);
  assert(contrast(data.border, rgb(palette[mode][1])) >= 3, `${mode}: adjacent surface/field border`);
  for (const [status, ink, surface] of data.statuses) assert(contrast(ink, surface) >= 4.5, `${mode}: ${status} text`);
  const field = page.locator('.form-control').first();
  await page.keyboard.press('Tab');
  await field.focus();
  const focus = await field.evaluate(e => [getComputedStyle(e).outlineColor, getComputedStyle(e).outlineWidth]);
  assert.equal(focus[1], '3px');
  assert(contrast(focus[0], rgb(palette[mode][1])) >= 3, `${mode}: focus indicator`);
  await field.evaluate(e => e.blur());
}
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const width of [1440, 390]) {
      const viewport = { width, height: 900 };
      const defaults = { viewport, reducedMotion: 'reduce' };
      const page = await browser.newPage({ ...defaults, colorScheme: 'light' });
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      await open(page);
      const appearance = page.locator('summary[aria-label="Appearance"]');
      await appearance.click();
      const system = page.getByRole('radio', { name: 'System' });
      assert(await system.isChecked());
      assert.equal(await page.getByRole('link', {name: 'TraceBack Home'}).count(), 1);
      assert.equal(await page.locator('svg.tb-icon:not([aria-hidden=true])').count(), 0);
      await page.waitForFunction(() => [...document.querySelectorAll('.brand-symbol use, .tb-appearance use')]
        .every(icon => icon.getBBox().width > 0));
      await page.evaluate(() => document.fonts.ready);
      assert(await page.evaluate(() => [...document.fonts].some(font => font.family === 'Inter' && font.status === 'loaded')));
      await verify(page, 'light');
      await appearance.click();
      await system.focus();
      await page.keyboard.press('ArrowRight');
      assert(await page.getByRole('radio', { name: 'Light' }).isChecked());
      await page.emulateMedia({ colorScheme: 'dark' });
      await verify(page, 'light');
      await appearance.click();
      await page.getByRole('radio', { name: 'Light' }).focus();
      await page.keyboard.press('ArrowRight');
      assert(await page.getByRole('radio', { name: 'Dark' }).isChecked());
      await verify(page, 'dark');
      assert.equal(await page.evaluate(() => localStorage.getItem('traceback.theme.v1')), 'dark');
      await page.emulateMedia({ colorScheme: 'light' });
      await page.reload();
      await verify(page, 'dark');
      await appearance.click();
      await page.locator('label[for=appearance-system]').click();
      await verify(page, 'light');
      await page.emulateMedia({ colorScheme: 'dark' });
      await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
      await verify(page, 'dark');
      if (process.env.THEME_SCREENSHOTS) await page.screenshot({path: `${process.env.THEME_SCREENSHOTS}/theme-dark-${width}.png`});
      assert.deepEqual(errors, []);
      await page.close();

      const blocked = await browser.newPage({ ...defaults, colorScheme: 'dark' });
      await blocked.addInitScript(() => Object.defineProperty(window, 'localStorage', { get() { throw new Error('Storage blocked'); } }));
      await open(blocked);
      await verify(blocked, 'dark');
      await blocked.locator('summary[aria-label="Appearance"]').click();
      await blocked.locator('label[for=appearance-light]').click();
      await verify(blocked, 'light');
      if (process.env.THEME_SCREENSHOTS) await blocked.screenshot({path: `${process.env.THEME_SCREENSHOTS}/theme-light-${width}.png`});
      await blocked.close();

      for (const mode of ['light', 'dark']) {
        const fallback = await browser.newPage({...defaults, colorScheme: mode, javaScriptEnabled: false});
        await open(fallback);
        assert.equal(await fallback.getByRole('radio').count(), 0);
        await verify(fallback, mode);
        const other = mode === 'dark' ? 'light' : 'dark';
        await fallback.emulateMedia({ colorScheme: other });
        await verify(fallback, other);
        await fallback.close();
      }
      const early = await browser.newPage({ ...defaults, colorScheme: 'light' });
      await early.addInitScript(() => localStorage.setItem('traceback.theme.v1', 'dark'));
      await early.route('**/js/theme.js*', route => route.abort());
      await open(early);
      assert.equal(await early.getByRole('radio').count(), 0);
      await verify(early, 'dark');
      await early.close();
      console.log(`Theme, storage and no-JS checks passed at ${width}px`);
    }
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
