const assert = require('node:assert/strict');
const fs = require('node:fs');
const { join } = require('node:path');
const media = require('./report-editor-media.cjs');

const variants = {
  '1440-light': [[29165, 29328, 29491, 29663, 29801, 29965, 30132, 30295], [40776, 40993, 41211, 41431, 41650]],
  '390-light': [[30468, 30625, 30782, 30948, 31080, 31238, 31399, 31556], [41867, 42076, 42286, 42498, 42709]],
  '1440-dark': [[31723, 31894, 32065, 32245, 32391, 32563, 32738, 32909], [42918, 43143, 43369, 43597, 43824]],
  '390-dark': [[33090, 33255, 33420, 33594, 33734, 33900, 34069, 34234], [44049, 44266, 44484, 44704, 44923]],
};
const values = {
  title: 'Blue water bottle', category: 'Accessories', location: 'Main library entrance',
  description: 'I found a blue metal bottle on the bench just inside the library. It has a black screw-top lid and a small dent near its base.',
  contact: 'Use TraceBack discussion', date: '2025-01-20', latitude: '12.971600', longitude: '77.594600',
};
const invalidPhoto = { ...media.photo, name: 'invalid.txt', mimeType: 'text/plain' };
const map = page => page.locator('[data-report-map="editor"]');
const photo = page => page.locator('#image');
const form = page => page.locator('main form');
const action = page => form(page).locator('button[type="submit"]');

async function fixturePhoto(page, color) {
  const base64 = await page.evaluate(color => {
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 270;
    const ctx = canvas.getContext('2d');
    ctx.fillStyle = '#e4e5ff'; ctx.fillRect(0, 0, 270, 270);
    ctx.fillStyle = color; ctx.fillRect(101, 52, 68, 187);
    ctx.fillStyle = '#202635'; ctx.fillRect(110, 31, 50, 23);
    return canvas.toDataURL().split(',')[1];
  }, color);
  return { name: 'fixture.png', mimeType: 'image/png', buffer: Buffer.from(base64, 'base64') };
}

async function capture(page, id, label) {
  const dir = process.env.EDITOR_EVIDENCE;
  if (!dir) return;
  await page.evaluate(() => document.fonts.ready);
  const selectors = ['h1', '.tb-report-tracker', '#type-locked', '#title', '#description', '#location',
    '.map-selection', '[data-map-canvas]', '#latitude', '#longitude', '#date', '#contact',
    '[data-image-preview]', '.image-preview-frame', '.tb-report-image-choices', '[data-image-removal]',
    '#image-error', '.report-form-actions', '.tb-report-camera'];
  const boxes = await page.evaluate(selectors => Object.fromEntries(selectors.map(selector => {
    const element = document.querySelector(selector);
    if (!element?.checkVisibility()) return [selector, null];
    const b = element.getBoundingClientRect();
    const s = getComputedStyle(element);
    return [selector, { x: b.x, y: b.y, w: b.width, h: b.height, font: s.fontSize, color: s.color, background: s.backgroundColor }];
  })), selectors);
  fs.writeFileSync(join(dir, `frame-${id}-${label}.json`), JSON.stringify(boxes, null, 2));
  await page.screenshot({ path: join(dir, `frame-${id}-${label}.png`), animations: 'disabled' });
}

async function track(page, label) {
  await page.getByRole('navigation', { name: 'In this report' }).getByRole('link', { name: label }).click();
}

async function fill(page) {
  await page.locator('label[for="type-found"]').click();
  for (const [id, value] of Object.entries(values)) await page.locator(`#${id}`).fill(value);
  await page.locator('#longitude').blur();
}

async function capturePending(page, id) {
  // The production window listener applies pending first. Hold that rendering without a navigation;
  // report-editor-media.cjs independently exercises the native multipart request and duplicate guard.
  const pending = await action(page).evaluate(button => {
    window.addEventListener('submit', event => event.preventDefault(), { once: true });
    button.click();
    return button.getAttribute('aria-disabled');
  });
  assert.equal(pending, 'true');
  await capture(page, id, 'pending');
  await page.evaluate(() => window.scrollTo(0, 0));
  await capture(page, id, 'top');
}

async function verifyGeometry(page, width) {
  const geometry = await page.evaluate(() => {
    const rect = s => document.querySelector(s).getBoundingClientRect();
    const row = (...selectors) => selectors.map(s => [rect(s).width, rect(s).height, rect(s).y === rect(selectors[0]).y]);
    return { x: rect('#title').x, input: rect('#title').height, description: rect('#description').height,
      tracker: rect('.tb-report-tracker').height, heading: getComputedStyle(document.querySelector('h1')).fontSize,
      lock: !!document.querySelector('.tb-report-type-locked use[href$="#lock-keyhole"]'),
      stacked: rect('#longitude').y > rect('#latitude').y,
      mapHeight: rect('[data-map-canvas]').height,
      photo: [rect('.image-preview-frame').width, rect('.image-preview-frame').height],
      photoActions: row('[data-photo-browse]', '[data-take-photo]'),
      formActions: row('.report-form-actions button', '.report-form-actions a'),
      mapBefore: !!(document.querySelector('[data-map-canvas]').compareDocumentPosition(document.querySelector('#latitude')) & Node.DOCUMENT_POSITION_FOLLOWING) };
  });
  assert.deepEqual(geometry, { x: width === 1440 ? 360 : 20, input: 48, description: 136,
    tracker: width === 1440 ? 236 : 48, heading: width === 1440 ? '44px' : '32px', lock: true, stacked: true,
    mapHeight: 184, photo: [270, 270], photoActions: [[width === 1440 ? 184 : 170, 48, true], [width === 1440 ? 178 : 168, 48, true]],
    formActions: [[width === 1440 ? 210 : 200, 48, true], [118, 48, true]], mapBefore: true });
}

async function verifyConflict(page, editUrl, width) {
  const stale = await page.context().newPage();
  try {
    await stale.goto(editUrl);
    await page.locator('#title').fill('Updated blue water bottle');
    await action(page).click();
    await page.waitForURL('**/Items/Details/**');
    await stale.locator('#title').fill('Stale fixture title');
    const conflict = stale.waitForResponse(r => r.url() === editUrl && r.request().method() === 'POST');
    await action(stale).click();
    assert.equal((await conflict).status(), 409);
    await stale.getByRole('heading', { name: 'This report changed while you were editing' }).waitFor();
    assert.equal(await stale.evaluate(() => document.documentElement.scrollWidth), width);
    const reload = stale.getByRole('link', { name: 'Reload report' });
    await reload.press('Enter');
    assert.equal(await stale.locator('#title').inputValue(), 'Updated blue water bottle');
    assert.equal(await stale.locator('#contact').inputValue(), values.contact);
    assert.match(await stale.locator('[data-preview-image]').getAttribute('src'), /\/Items\/Image\//);
  } finally { await stale.close(); }
}

async function parity(browser, storageState, base, width, colorScheme) {
  const [create, edit] = variants[`${width}-${colorScheme}`];
  const context = await browser.newContext({ storageState, colorScheme, reducedMotion: 'reduce',
    viewport: { width, height: width === 1440 ? 1040 : 844 }, hasTouch: width === 390 });
  let tilesFail = false;
  const tile = '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><path fill="#e1e9e3" d="M0 0h256v256H0z"/><path stroke="#faf9f1" stroke-width="10" d="M0 105h256 M138 0v256"/></svg>';
  await context.route('https://tile.openstreetmap.org/**', r => tilesFail ? r.abort() : r.fulfill({ contentType: 'image/svg+xml', body: tile }));
  await media.fixture(context);
  try {
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const createUrl = `${base}/Items/Create`;
    const open = async url => {
      await page.goto(url);
      assert.equal(await page.locator('html').getAttribute('data-theme'), colorScheme);
    };
    await open(createUrl);
    assert(!await page.locator('[data-map-external]').isVisible());
    const original = await fixturePhoto(page, '#4464c2');
    const replacement = await fixturePhoto(page, '#5c8091');
    await capture(page, create[0], 'top');
    await track(page, 'Where and when');
    await capture(page, create[0], 'location');
    await track(page, 'Image');
    await action(page).click();
    assert(await page.locator('#title-error .field-validation-error').isVisible());
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, create[2], 'top');
    await open(createUrl);
    await fill(page);
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, create[1], 'top');
    await track(page, 'Where and when');
    await capture(page, create[1], 'location');
    tilesFail = true;
    await open(createUrl);
    await fill(page);
    await map(page).locator('[data-map-retry]').waitFor();
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, create[3], 'top');
    await track(page, 'Where and when');
    await capture(page, create[3], 'location');
    tilesFail = false;
    await map(page).locator('[data-map-retry]').click();
    await page.waitForFunction(() => document.querySelector('[data-report-map]').dataset.mapState === 'ready');
    assert.equal(Number(await page.locator('#latitude').inputValue()), Number(values.latitude));
    await photo(page).setInputFiles(invalidPhoto);
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, create[4], 'top');
    await track(page, 'Image');
    await capture(page, create[4], 'image');
    await photo(page).setInputFiles(original);
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, create[5], 'top');
    await track(page, 'Image');
    await capture(page, create[5], 'image');
    await page.getByRole('button', { name: 'Take photo', exact: true }).click();
    await page.getByRole('button', { name: 'Capture photo' }).click();
    await page.getByRole('button', { name: 'Use this photo' }).waitFor();
    await capture(page, create[7], 'camera');
    await page.keyboard.press('Escape');
    await action(page).scrollIntoViewIfNeeded();
    await capturePending(page, create[6]);
    await open(createUrl);
    await fill(page);
    await photo(page).setInputFiles(original);
    await action(page).click();
    await page.waitForURL('**/Items/Details/**');
    const editUrl = page.url().replace('/Details/', '/Edit/');
    await open(editUrl);
    assert.equal(await page.locator('[data-map-selection]').innerText(), 'Saved map pin');
    const external = page.getByRole('link', { name: 'View in Google Maps' });
    assert.equal(await external.getAttribute('href'), 'https://www.google.com/maps/search/?api=1&query=12.971600%2C77.594600');
    assert.equal(await external.getAttribute('rel'), 'noopener noreferrer');
    await capture(page, edit[0], 'top');
    await track(page, 'Where and when');
    await capture(page, edit[0], 'location');
    await track(page, 'How to reconnect');
    await capture(page, edit[0], 'contact');
    await track(page, 'Image');
    await page.locator('[data-preview-image]').evaluate(img => img.decode());
    await capture(page, edit[0], 'image');
    await photo(page).setInputFiles(replacement);
    assert.match(await page.locator('[data-preview-image]').getAttribute('src'), /^blob:/);
    await capture(page, edit[1], 'image');
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, edit[1], 'top');
    await page.locator('[data-remove-pending]').click();
    await page.locator('[data-remove-current]').click();
    await capture(page, edit[2], 'image');
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, edit[2], 'top');
    await page.locator('[data-undo-remove]').click();
    await photo(page).setInputFiles(invalidPhoto);
    assert.match(await page.locator('[data-preview-image]').getAttribute('src'), /\/Items\/Image\//);
    await track(page, 'Image');
    await capture(page, edit[3], 'image');
    await page.evaluate(() => window.scrollTo(0, 0));
    await capture(page, edit[3], 'top');
    await open(editUrl);
    await action(page).scrollIntoViewIfNeeded();
    await capturePending(page, edit[4]);
    await open(editUrl);
    await page.waitForFunction(() => document.querySelector('[data-report-map]').dataset.mapState === 'ready');
    await verifyGeometry(page, width);
    await verifyConflict(page, editUrl, width);
    assert.deepEqual(errors, []);
    console.log(`Report editor frame matrix: ${width}px ${colorScheme}, 8 Create and 5 Edit scenarios`);
  } finally { await context.close(); }
}

module.exports = parity;
