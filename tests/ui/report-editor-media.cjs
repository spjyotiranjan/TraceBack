const assert = require('node:assert/strict');
const { join } = require('node:path');
const photo = { name: 'item.gif', mimeType: 'image/gif', buffer: Buffer.from('R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7', 'base64') };

// Real video frames and MediaStream tracks without depending on a physical camera.
async function fixture(context) {
  await context.addInitScript(() => {
    window.cameraTracks = [];
    window.revokedPreviews = [];
    const revoke = URL.revokeObjectURL.bind(URL);
    URL.revokeObjectURL = url => { revokedPreviews.push(url); revoke(url); };
    Object.defineProperty(navigator.mediaDevices, 'getUserMedia', { value: async () => {
      if (window.cameraDenied) throw new DOMException('Denied by test', 'NotAllowedError');
      if (window.cameraWait) await new Promise(resolve => { window.allowCamera = resolve; });
      const canvas = document.createElement('canvas');
      canvas.width = 640;
      canvas.height = 480;
      const ctx = canvas.getContext('2d');
      const stream = canvas.captureStream(15);
      ctx.fillStyle = '#536bc1';
      ctx.fillRect(0, 0, 640, 480);
      cameraTracks.push(...stream.getTracks());
      return stream;
    } });
  });
}

async function review(page, width, colorScheme) {
  const opener = page.getByRole('button', { name: 'Take photo', exact: true });
  const modal = page.getByRole('dialog', { name: 'Take a photo' });
  const input = page.locator('#image');
  await opener.focus();
  await page.keyboard.press('Enter');
  assert(await modal.isVisible(), 'Camera must open in a named modal dialog');
  assert(await modal.evaluate(el => el.matches(':modal')));
  const capture = modal.getByRole('button', { name: 'Capture photo' });
  await capture.click();
  await modal.getByRole('button', { name: 'Use this photo' }).waitFor();
  assert.equal(await input.evaluate(el => el.files.length), 0, 'Review does not attach the file');
  assert(await page.evaluate(() => cameraTracks.every(track => track.readyState === 'ended')));
  assert.equal(await modal.evaluate(el => getComputedStyle(el).animationName), 'none');
  await page.keyboard.press('Tab');
  assert(await modal.evaluate(el => el.contains(document.activeElement)));
  if (process.env.EDITOR_EVIDENCE) await page.screenshot({ path: join(process.env.EDITOR_EVIDENCE, `camera-${width}-${colorScheme}.png`) });
  await modal.getByRole('button', { name: 'Retake photo' }).click();
  await capture.click();
  await modal.getByRole('button', { name: 'Use this photo' }).click();
  assert(!await modal.isVisible());
  assert(await opener.evaluate(el => el === document.activeElement));
  assert.equal(await input.evaluate(el => el.files[0].type), 'image/jpeg');
  const preview = page.locator('[data-preview-image]');
  const previousPreview = await preview.getAttribute('src');
  await input.setInputFiles(photo);
  assert(await page.evaluate(url => revokedPreviews.includes(url), previousPreview));
  await page.getByRole('button', { name: 'Remove selected photo' }).click();
  assert.equal(await input.evaluate(el => el.files.length), 0);
  await page.locator('#title').fill('Keep this draft');
  await page.evaluate(() => { window.cameraDenied = true; });
  await opener.click();
  await modal.getByText(/camera access is blocked/i).waitFor();
  assert(await modal.getByRole('button', { name: 'Upload image' }).isVisible());
  await page.keyboard.press('Escape');
  await modal.waitFor({ state: 'hidden' });
  assert.equal(await page.locator('#title').inputValue(), 'Keep this draft');
  assert(await opener.evaluate(el => el === document.activeElement));
  await page.evaluate(() => { window.cameraDenied = false; window.cameraWait = true; });
  await opener.click();
  await page.waitForFunction(() => window.allowCamera);
  await modal.getByRole('button', { name: 'Cancel' }).click();
  await page.evaluate(() => window.allowCamera());
  await page.waitForFunction(() => cameraTracks.length === 3 && cameraTracks.every(track => track.readyState === 'ended'));
  console.log(`${width}px ${colorScheme}: camera review, retake, confirmation, track cleanup, denial, cancellation and focus passed`);
}

async function currentPhoto(page) {
  const input = page.locator('#image');
  const preview = page.locator('[data-preview-image]');
  const src = await preview.getAttribute('src');
  assert.match(src, /\/Items\/Image\/[a-f0-9]{24}$/);
  await page.getByRole('button', { name: 'Remove current image' }).press('Enter');
  assert(await page.locator('#remove-image').isChecked());
  await page.getByRole('button', { name: 'Undo remove' }).press('Enter');
  assert(!await page.locator('#remove-image').isChecked());
  assert(await page.locator('[data-preview-status]').evaluate(el => el === document.activeElement));
  await page.getByRole('button', { name: 'Remove current image' }).click();
  await input.setInputFiles(photo);
  assert(!await page.locator('#remove-image').isChecked());
  assert.match(await preview.getAttribute('src'), /^blob:/);
  await input.setInputFiles({ ...photo, name: 'large.gif', buffer: Buffer.alloc(5 * 1024 * 1024 + 1) });
  await page.getByText(/no larger than 5 MiB/).waitFor();
  assert.equal(await preview.getAttribute('src'), src);
  await input.setInputFiles({ ...photo, mimeType: 'text/plain', name: 'not-a-photo.txt' });
  assert.equal(await preview.getAttribute('src'), src);
  assert.equal(await input.evaluate(el => el.files.length), 0);
}

async function pendingPublish(page) {
  for (const [id, value] of Object.entries({ title: 'Guarded publish', category: 'Keys', description: 'Distinct set of keys',
    location: 'Library entrance', contact: 'Campus desk', latitude: '20.5', longitude: '85.5' })) {
    await page.locator(`#${id}`).fill(value);
  }
  let posts = 0;
  await page.route('**/Items/Create', async route => {
    if (route.request().method() === 'POST') {
      posts++;
      assert.match(route.request().headers()['content-type'], /^multipart\/form-data/);
    }
    await route.continue();
  });
  const state = await page.locator('main form').evaluate(form => {
    const publish = form.querySelector('button[type=submit]');
    publish.click();
    const pending = publish.getAttribute('aria-disabled');
    form.requestSubmit(publish);
    publish.click();
    return [pending, form.querySelector('[data-tb-pending-status]').textContent, publish.querySelector('.tb-report-submit-pending').checkVisibility()];
  });
  assert.deepEqual(state, ['true', 'Publishing…', true]);
  await page.waitForURL(/\/Items\/Details\/[a-f0-9]{24}$/);
  assert.equal(posts, 1);
  assert(await page.getByRole('heading', { name: 'Guarded publish' }).isVisible());
}

module.exports = { fixture, review, photo, currentPhoto, pendingPublish };
