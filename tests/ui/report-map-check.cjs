// Run against an isolated local TraceBack instance with Playwright available via NODE_PATH.
// The intercepted PNG only drives tile load events; coordinates come from inputs and Leaflet.
const assert = require('node:assert/strict');
const { randomUUID } = require('node:crypto');
const { mkdirSync } = require('node:fs');
const { join } = require('node:path');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL || 'http://127.0.0.1:5187';
const evidence = process.env.TRACEBACK_MAP_EVIDENCE;
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/, 'Use an isolated local instance');
const tile = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLbtAAAAABJRU5ErkJggg==', 'base64');
const point = page => Promise.all(['#latitude', '#longitude'].map(id => page.locator(id).inputValue()));
const canvas = page => page.locator('[data-map-canvas]');
const status = page => page.locator('[data-map-status]');
const saved = page => page.locator('[data-report-map="preview"]');
const retry = page => page.locator('[data-map-retry]');
const until = async (page, predicate) => { await page.waitForFunction(predicate); };

async function signUp(page) {
  await page.goto(`${base}/Account/Register`);
  const token = await page.locator('input[name="__RequestVerificationToken"]').inputValue();
  const id = randomUUID();
  const response = await page.request.post(`${base}/Account/Register`, {
    form: { __RequestVerificationToken: token, Name: 'Map QA', Email: `${id}@example.test`, Password: randomUUID() },
    maxRedirects: 0,
  });
  assert.equal(response.status(), 302);
}

async function fillReport(page) {
  await page.locator('#title').fill(`Map browser check ${randomUUID()}`);
  await page.locator('#category').fill('Keys');
  await page.locator('#description').fill('A set of keys for a local test');
  await page.locator('#location').fill('East library entrance');
  await page.locator('[name="ContactPreference"]').fill('Help desk');
}

async function main() {
  if (evidence) mkdirSync(evidence, { recursive: true });
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH });
  try {
    for (const [width, theme] of [[1440, 'light'], [390, 'dark']]) {
      const context = await browser.newContext({ viewport: { width, height: 844 }, colorScheme: theme, hasTouch: width === 390 });
      const page = await context.newPage();
      page.setDefaultTimeout(8000);
      const errors = [];
      page.on('pageerror', error => errors.push(error.stack));
      let state = 'hold';
      let release;
      const held = new Promise(resolve => { release = resolve; });
      await page.route('**://tile.openstreetmap.org/**', async route => {
        if (state === 'hold') await held;
        if (state === 'fail') await route.abort();
        else await route.fulfill({ contentType: 'image/png', body: tile });
      });
      await signUp(page);
      await page.goto(`${base}/Items/Create`, { waitUntil: 'domcontentloaded' });
      assert.match(await status(page).textContent(), /Loading map/);
      await fillReport(page);
      state = 'ok'; release();
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      assert.deepEqual(await point(page), ['', '']);
      const position = { x: 92, y: 115 };
      if (width === 390) await canvas(page).tap({ position });
      else await canvas(page).click({ position });
      const first = await point(page);
      assert(first.every(v => /^-?\d+\.\d{6}$/.test(v)));
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      const marker = page.locator('.leaflet-marker-icon');
      const pin = await marker.boundingBox();
      await page.mouse.move(pin.x + pin.width / 2, pin.y + pin.height / 2);
      await page.mouse.down();
      await page.mouse.move(pin.x + pin.width / 2 + 36, pin.y + pin.height / 2 + 18, { steps: 5 });
      await page.mouse.up();
      const moved = await point(page);
      assert.notDeepEqual(moved, first);
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      state = 'fail';
      await page.locator('.leaflet-control-zoom-in').click();
      await retry(page).waitFor({ state: 'visible' });
      assert.match(await status(page).textContent(), /tiles unavailable/i);
      const title = await page.locator('#title').inputValue();
      assert.deepEqual(await point(page), moved);
      await canvas(page).click({ position });
      assert.deepEqual(await point(page), moved, 'No pin changes against an unavailable basemap');
      state = 'ok';
      await retry(page).focus();
      await page.keyboard.press('Enter');
      await retry(page).waitFor({ state: 'hidden' });
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      assert.deepEqual(await point(page), moved);
      assert.equal(await page.locator('#title').inputValue(), title);
      assert.equal(await page.locator('#location').inputValue(), 'East library entrance');
      assert.equal(await marker.count(), 1);
      await page.locator('#latitude').fill('20.2960580');
      await page.locator('#longitude').fill('85.824539');
      await page.getByRole('button', { name: 'Publish report' }).click();
      await page.waitForURL('**/Items/Details/**');
      const detailUrl = page.url();
      assert.equal(await saved(page).getAttribute('data-latitude'), '20.296058');
      assert.equal(await saved(page).getAttribute('data-longitude'), '85.824539');
      assert.equal(Math.round((await canvas(page).boundingBox()).height), 184);
      assert.equal(await saved(page).locator('details').getAttribute('open'), null);
      await saved(page).locator('summary').focus();
      await page.keyboard.press('Enter');
      assert.match(await saved(page).locator('details[open]').innerText(), /20\.296058, 85\.824539/);
      assert.match(await saved(page).innerText(), /tile provider/i);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);
      state = 'fail';
      await page.reload({ waitUntil: 'domcontentloaded' });
      await retry(page).waitFor({ state: 'visible' });
      assert.match(await status(page).textContent(), /preview unavailable/i);
      if (evidence) await page.locator('.detail-report__map').screenshot({ path: join(evidence, `preview-outage-${width}.png`) });
      state = 'ok';
      await retry(page).click();
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      const editUrl = await page.getByRole('link', { name: 'Edit report' }).getAttribute('href');
      await page.goto(new URL(editUrl, base).href, { waitUntil: 'domcontentloaded' });
      assert.deepEqual((await point(page)).map(Number), [20.296058, 85.824539]);
      await page.locator('#longitude').fill('');
      await page.getByRole('button', { name: 'Save changes' }).click();
      await page.getByText(/Latitude must be between/).first().waitFor();
      assert.equal(await page.locator('#longitude').inputValue(), '');
      assert.deepEqual(errors, []);
      console.log(`${width}px ${theme}: loading, placement, drag, outage, retry, persisted preview, disclosure and partial edit passed`);

      await page.route('**/lib/leaflet/leaflet.js', route => route.abort(), { times: 1 });
      await page.goto(detailUrl, { waitUntil: 'domcontentloaded' });
      await retry(page).waitFor({ state: 'visible' });
      assert.equal(await page.locator('.leaflet-marker-icon').count(), 0);
      await retry(page).click();
      await page.locator('.leaflet-marker-icon').waitFor({ state: 'visible' });
      await until(page, () => document.querySelector('[data-map-status]').textContent === '');
      assert.deepEqual(errors, []);
      console.log(`${width}px: missing Leaflet asset recovered on retry`);

      const noScript = await browser.newContext({ javaScriptEnabled: false, storageState: await context.storageState() });
      const manual = await noScript.newPage();
      await manual.goto(detailUrl);
      assert.equal(await manual.getByText(/JavaScript is off/).isVisible(), true);
      await manual.goto(`${base}/Items/Create`);
      await fillReport(manual);
      await manual.locator('#latitude').fill('not-a-coordinate');
      await manual.locator('#longitude').fill('0');
      await manual.getByRole('button', { name: 'Publish report' }).click();
      assert.equal(await manual.locator('#latitude').inputValue(), 'not-a-coordinate');
      await manual.locator('#latitude').fill('0');
      await manual.getByRole('button', { name: 'Publish report' }).click();
      await manual.waitForURL('**/Items/Details/**');
      assert.equal(await saved(manual).getAttribute('data-latitude'), '0.000000');
      await noScript.close();
      await context.close();
      console.log(`${width}px: no-JS fallback, raw validation value and manual publish passed`);
    }
  } finally {
    await browser.close();
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
