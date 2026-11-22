// Exercise the real editor against a disposable local database (see docs/report-editor.md).
const assert = require('node:assert/strict');
const { randomUUID } = require('node:crypto');
const { join } = require('node:path');
const { mkdirSync, mkdtempSync, rmSync } = require('node:fs');
const { chromium } = require('playwright');
const media = require('./report-editor-media.cjs');
const base = process.env.TRACEBACK_URL || 'http://127.0.0.1:5187';
assert.match(base, /^http:\/\/127\.0\.0\.1:\d+$/);
const names = ['What happened', 'Where and when', 'How to reconnect', 'Image'];
const tracker = page => page.getByRole('navigation', { name: 'In this report' });

async function openEditor(page) {
  await page.goto(`${base}/Items/Create`);
  assert.equal(new URL(page.url()).pathname, '/Items/Create');
}

async function current(page, name) {
  await page.waitForFunction(name => document.querySelector('[data-report-tracker] [aria-current="step"]')?.textContent.includes(name), name, { timeout: 5000 });
  assert.equal(await tracker(page).locator('[aria-current]').count(), 1);
}

async function clearOfShell(page, element) {
  const handle = await element.elementHandle();
  try {
    // Native anchor scrolling can animate even when page JavaScript is disabled.
    await page.waitForFunction(node => {
      const rect = node.getBoundingClientRect();
      const header = document.querySelector('.site-header').getBoundingClientRect();
      const dock = document.querySelector('.tb-member-dock').getBoundingClientRect();
      const progress = document.querySelector('.tb-report-tracker.is-tracking')?.getBoundingClientRect();
      const top = progress && dock.height ? Math.max(header.bottom, progress.bottom) : header.bottom;
      return rect.top >= top && rect.bottom <= (dock.height ? dock.top : innerHeight);
    }, handle, { timeout: 5000 });
  } finally { await handle.dispose(); }
}

async function nativeZoom(storageState) {
  const bin = join(process.cwd(), 'src', 'TraceBack', 'bin');
  mkdirSync(bin, { recursive: true });
  const profile = mkdtempSync(join(bin, 'editor-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath: process.env.CHROME_PATH || undefined,
      channel: process.env.CHROME_PATH ? undefined : 'chromium',
      deviceScaleFactor: 1, viewport: { width: 640, height: 900 }, reducedMotion: 'reduce'
    });
    try {
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      await context.addCookies(storageState.cookies);
      await media.fixture(context);
      await openEditor(page);
      assert.deepEqual(await page.evaluate(() => [innerWidth, innerHeight, devicePixelRatio, document.documentElement.scrollWidth]), [320, 450, 2, 320]);
      await tracker(page).getByRole('link', { name: names[2] }).press('Enter');
      await current(page, names[2]);
      await page.keyboard.press('Tab');
      await clearOfShell(page, page.locator('#contact'));
      await page.getByRole('button', { name: 'Publish report' }).focus();
      await clearOfShell(page, page.getByRole('button', { name: 'Publish report' }));
      await page.getByRole('button', { name: 'Take photo' }).click();
      const camera = page.getByRole('dialog', { name: 'Take a photo' });
      await camera.getByRole('button', { name: 'Capture photo' }).click();
      await camera.getByRole('button', { name: 'Use this photo' }).waitFor();
      assert(await camera.evaluate(el => el.scrollWidth === el.clientWidth));
      await page.keyboard.press('Escape');
      await camera.waitFor({ state: 'hidden' });
      console.log('Native Chromium 200% zoom: 320 CSS px reflow and contact/publish focus clearance passed');
    } finally { await context.close(); }
  } finally { rmSync(profile, { force: true, recursive: true, maxRetries: 5, retryDelay: 100 }); }
}

async function main() {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH });
  try {
    const account = await browser.newContext({ javaScriptEnabled: false });
    const manual = await account.newPage();
    await manual.goto(`${base}/Account/Register`);
    const registered = await manual.request.post(`${base}/Account/Register`, {
      form: { __RequestVerificationToken: await manual.locator('input[name="__RequestVerificationToken"]').first().inputValue(),
        Name: 'Editor QA', Email: `${randomUUID()}@example.test`, Password: randomUUID() }, maxRedirects: 0,
    });
    assert.equal(registered.status(), 302);
    await openEditor(manual);
    const form = manual.locator('main form');
    assert.equal(await form.count(), 1);
    assert.equal(await form.getAttribute('enctype'), 'multipart/form-data');
    assert.equal(await form.locator('[name="__RequestVerificationToken"]').count(), 1);
    assert.deepEqual(await tracker(manual).getByRole('link').allTextContents().then(texts => texts.map(text => names.find(name => text.includes(name)))), names);
    const lost = manual.getByRole('radio', { name: 'Lost' });
    const found = manual.getByRole('radio', { name: 'Found' });
    assert(await lost.isChecked());
    for (const [name, icon] of [['Lost', 'search-x'], ['Found', 'package-check']]) {
      const href = await manual.locator('label').filter({ hasText: new RegExp(`^\\s*${name}\\s*$`) }).locator('use').getAttribute('href');
      assert(href.endsWith(`#${icon}`));
    }
    await manual.locator('label[for="type-found"]').click();
    assert(await found.isChecked());
    const values = { title: 'Editor browser fixture', category: 'Keys', description: 'A distinct set of keys', location: 'Library', contact: 'Campus desk', latitude: 'not-a-coordinate', longitude: '1.25' };
    for (const [id, value] of Object.entries(values)) await manual.locator(`#${id}`).fill(value);
    await tracker(manual).getByRole('link', { name: 'Where and when' }).click();
    assert.equal(new URL(manual.url()).hash, '#report-place-heading');
    await clearOfShell(manual, manual.getByRole('heading', { name: 'Where and when' }));
    await form.getByRole('button', { name: 'Publish report' }).click();
    assert.equal(new URL(manual.url()).pathname, '/Items/Create');
    assert(await found.isChecked());
    assert(await manual.locator('#latitude-error').isVisible());
    for (const [id, value] of Object.entries(values)) assert.equal(await manual.locator(`#${id}`).inputValue(), value);
    await manual.locator('#latitude').fill('20.5');
    await manual.locator('#date').fill('');
    await form.getByRole('button', { name: 'Publish report' }).click();
    assert.equal(new URL(manual.url()).pathname, '/Items/Create', 'A missing date must not publish');
    assert(await manual.locator('#date-error').isVisible());
    assert.equal(await manual.locator('#date').inputValue(), '');
    await manual.locator('#date').fill('2024-01-01');
    if (process.env.EDITOR_STORED_PHOTO === '1') {
      assert(!await manual.getByRole('button', { name: 'Take photo' }).isVisible());
      await manual.getByLabel('Photo (optional)').setInputFiles(media.photo);
    }
    await form.getByRole('button', { name: 'Publish report' }).click();
    await manual.waitForURL(/\/Items\/Details\/[a-f0-9]{24}$/);
    assert.equal(await manual.locator('[data-report-map="preview"]').getAttribute('data-latitude'), '20.500000');
    await manual.getByRole('link', { name: 'Edit report' }).click();
    const editUrl = manual.url();
    assert.equal(await manual.getByRole('radio').count(), 0);
    const locked = manual.getByRole('textbox', { name: 'Type' });
    assert.equal(await locked.inputValue(), 'Found');
    assert(await locked.evaluate(input => input.readOnly));
    await manual.locator('#title').fill('Editor saved without scripts');
    await form.getByRole('button', { name: 'Save changes' }).click();
    await manual.waitForURL('**/Items/Details/**');
    assert(await manual.getByRole('heading', { name: 'Editor saved without scripts' }).isVisible());
    const storageState = await account.storageState();
    await account.close();
    console.log('No JavaScript: anchors, icons, validation redisplay, manual pin, publish and locked Edit save passed');

    const fallback = await browser.newContext({ storageState });
    await fallback.addInitScript(() => { Object.defineProperty(navigator, 'mediaDevices', { value: undefined }); });
    const fallbackPage = await fallback.newPage();
    await openEditor(fallbackPage);
    assert(!await fallbackPage.getByRole('button', { name: 'Take photo' }).isVisible());
    await fallbackPage.getByLabel('Photo (optional)').setInputFiles(media.photo);
    await fallbackPage.getByText('Image ready to publish').waitFor();
    await fallbackPage.getByRole('button', { name: 'Remove selected photo' }).click();
    await media.pendingPublish(fallbackPage);
    await fallback.close();

    for (const width of [1440, 896, 895, 390, 320]) for (const colorScheme of ['light', 'dark']) {
      const mobile = width < 896;
      const context = await browser.newContext({ storageState, viewport: { width, height: 844 }, colorScheme, hasTouch: mobile, reducedMotion: 'reduce' });
      const referenceWidth = [1440, 390].includes(width);
      if (referenceWidth) await media.fixture(context);
      await context.route('**://tile.openstreetmap.org/**', route => route.abort());
      const page = await context.newPage();
      const errors = [];
      page.on('pageerror', error => errors.push(error.message));
      await openEditor(page);
      await current(page, names[0]);
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width);
      if (process.env.EDITOR_EVIDENCE && [1440, 390].includes(width)) {
        await page.screenshot({ path: join(process.env.EDITOR_EVIDENCE, `create-${width}-${colorScheme}.png`) });
      }
      if (mobile) {
        assert.equal(await tracker(page).locator('[data-report-current]').innerText(), names[0]);
        assert.equal(await tracker(page).locator('a span.visually-hidden, a .tb-report-tracker__link-label').first().evaluate(node => node.getBoundingClientRect().width), 1);
      }
      const link = tracker(page).getByRole('link', { name: names[1] });
      if (mobile) await link.tap();
      else { await link.focus(); await page.keyboard.press('Enter'); }
      await current(page, names[1]);
      const heading = page.getByRole('heading', { name: names[1] });
      assert(await heading.evaluate(node => document.activeElement === node));
      await clearOfShell(page, heading);
      await page.keyboard.press('Tab');
      assert(await page.locator('#location').evaluate(node => document.activeElement === node));
      await clearOfShell(page, page.locator('#location'));
      await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight));
      await current(page, names[3]);
      if (mobile) assert.equal(await tracker(page).locator('[data-report-current]').innerText(), names[3]);
      if (referenceWidth) await media.review(page, width, colorScheme);
      await page.goto(new URL('#report-contact-heading', editUrl).href);
      await current(page, names[2]);
      await clearOfShell(page, page.getByRole('heading', { name: names[2] }));
      if (referenceWidth && process.env.EDITOR_STORED_PHOTO === '1') {
        await tracker(page).getByRole('link', { name: 'Image' }).click();
        const mutations = [];
        page.on('request', request => { if (request.method() !== 'GET') mutations.push(request.method()); });
        await media.currentPhoto(page);
        assert.deepEqual(mutations, []);
      }
      assert.deepEqual(errors, []);
      if (process.env.EDITOR_EVIDENCE && [1440, 390].includes(width)) {
        await page.screenshot({ path: join(process.env.EDITOR_EVIDENCE, `edit-${width}-${colorScheme}.png`) });
      }
      console.log(`${width}px ${colorScheme}: tracker, anchors, focus clearance, scroll and Edit deep link passed`);
      await context.close();
    }
    await nativeZoom(storageState);
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
