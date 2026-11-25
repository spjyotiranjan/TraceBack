// Run via Item_details_pass_the_browser_matrix on the loopback test host.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const ids = process.env.TRACEBACK_TEST_ITEM_IDS.split(',');
const [name, value] = process.env.TRACEBACK_TEST_COOKIE.split(/=(.*)/s);
const evidence = process.env.TRACEBACK_DETAILS_EVIDENCE;
const executablePath = process.env.CHROME_PATH || undefined;
const tile = '**/tile.openstreetmap.org/**';
const tilePng = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aB1sAAAAASUVORK5CYII=', 'base64');

async function visit(page, index = 0) {
  const response = await page.goto(new URL(`/Items/Details/${ids[index]}`, base).href, { waitUntil: 'domcontentloaded' });
  assert.equal(response.status(), 200);
}

async function snapshot(page, state, suffix, scroll = 0) {
  if (!evidence) return;
  await fs.mkdir(evidence, { recursive: true });
  await page.evaluate(top => window.scrollTo({ top, behavior: 'instant' }), scroll);
  await page.screenshot({ path: path.join(evidence, `${state}-${suffix}.png`) });
}

async function referenceStates(page, suffix) {
  if (!evidence) return;
  const comments = [
    { id: '1', authorName: 'Mira S.', authorId: 'mira', createdAt: '2026-11-18T12:00:00Z', body: 'Thanks for posting this. Was it near the main doors?' },
    { id: '2', authorName: 'Jordan L.', authorId: 'jordan', createdAt: '2026-11-18T12:00:00Z', body: 'I checked the library front desk this morning.' }
  ];
  let thread = 'ready';
  let release;
  await page.route('**/Comments?*', async route => {
    if (thread === 'loading') await new Promise(resolve => { release = resolve; });
    if (thread === 'error') return route.fulfill({ status: 503 });
    const parentId = new URL(route.request().url()).searchParams.get('parentId');
    return route.fulfill({ json: { nextCursor: null, comments: parentId ? [] : comments } });
  });
  await page.route('**/Items/Image/*', route => route.fulfill({ contentType: 'image/png', body: tilePng }));
  for (const [index, state] of [[4, 'found'], [5, 'owner'], [11, 'lost'], [7, 'resolved'], [6, 'claimed'], [8, 'no-photo'], [10, 'legacy']]) {
    await visit(page, index);
    await page.locator('[data-discussion-root]').getByText(comments[0].body).waitFor();
    await snapshot(page, state, suffix);
    if (state === 'found' || state === 'owner') {
      const top = await page.locator('.detail-report__facts').evaluate(el => el.getBoundingClientRect().top + scrollY - 125);
      await snapshot(page, `${state}-facts`, suffix, top);
    }
    if (state === 'owner') {
      for (const [button, scenario] of [['Mark resolved', 'confirm-resolve'], ['Delete report', 'confirm-delete']]) {
        await page.getByRole('button', { name: button, exact: true }).click();
        await page.getByRole('dialog').waitFor();
        await snapshot(page, scenario, suffix, await page.evaluate(() => scrollY));
        await page.keyboard.press('Escape');
        await page.getByRole('dialog').waitFor({ state: 'hidden' });
      }
    }
  }
  await page.unroute('**/Items/Image/*');
  await visit(page, 9);
  await page.getByText('The report photo could not load.').waitFor();
  await snapshot(page, 'photo-error', suffix);
  for (const state of ['loading', 'error']) {
    thread = state;
    await visit(page, 4);
    const root = page.locator('[data-discussion-root]');
    await (state === 'loading' ? page.locator('[data-discussion-root][aria-busy="true"]') : root.getByRole('button', { name: 'Retry comments' })).waitFor();
    const top = await page.locator('.discussion').evaluate(el => el.getBoundingClientRect().top + scrollY - 125);
    await snapshot(page, `thread-${state}`, suffix, top);
    if (release) { release(); release = null; }
    if (state === 'loading') await root.getByText(comments[0].body).waitFor();
  }
  await page.unroute('**/Comments?*');
}

async function reflow(page) {
  assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'no horizontal overflow');
  const title = await page.locator('h1').evaluate(el => [getComputedStyle(el).overflowY, el.scrollHeight - el.clientHeight]);
  assert(title[0] === 'visible' && title[1] <= 2, `title not clipped; overflow and subpixel difference: ${title}`);
}

async function discussionJourney(page, touch, suffix) {
  let cursorRequests = 0;
  const comments = Array.from({ length: 15 }, (_, i) => ({
    id: String(i), authorName: 'Fixture reader', authorId: 'reader',
    createdAt: '2026-11-18T12:00:00Z', deleted: i === 0, body: `Comment ${i}`
  }));
  await page.route('**/Comments?*', route => {
    const query = new URL(route.request().url()).searchParams;
    if (query.has('parentId')) return route.fulfill({ json: {
      comments: query.get('parentId') === '0' ? [{ ...comments[1], id: 'reply', body: 'Surviving reply' }] : [], nextCursor: null
    } });
    if (!query.has('cursor')) return route.fulfill({ json: { comments, nextCursor: 'older' } });
    assert.equal(query.get('cursor'), 'older');
    if (++cursorRequests === 1) return route.fulfill({ status: 503 });
    return route.fulfill({ json: { comments: [{ ...comments[1], id: 'last', body: 'Last page' }], nextCursor: null } });
  });
  await visit(page, 1);
  const region = page.getByRole('region', { name: 'Comment thread' });
  await region.getByText('Deleted comment', { exact: true }).waitFor({ timeout: 2000 });
  assert.equal(await region.getByText('Deleted comment', { exact: true }).locator('..').locator('.discussion-avatar').count(), 0);
  await region.focus();
  await region.getByText('Surviving reply').waitFor();
  await page.keyboard.press('Shift+Tab');
  assert(await page.getByRole('button', { name: 'Post comment' }).evaluate(el => el === document.activeElement));
  await page.keyboard.press('Tab');
  assert.equal(cursorRequests, 0);
  await region.press('End');
  const retry = region.getByRole('button', { name: 'Retry comments' });
  await retry.waitFor();
  assert.equal(cursorRequests, 1);
  assert(await region.evaluate(el => el.scrollTop > 0 && el === document.activeElement));
  await retry.press('Enter');
  await region.getByText('Last page', { exact: true }).waitFor();
  await region.press('End');
  assert.equal(cursorRequests, 2);
  assert.equal(await region.getByRole('button', { name: /load more/i }).count(), 0);
  await region.getByText('Comment 1', { exact: true }).locator('..').getByRole('button', { name: 'Reply' }).click();
  const composer = page.getByRole('form', { name: 'Comment composer' });
  const attachment = composer.getByRole('button', { name: 'Attach image' });
  const post = composer.getByRole('button', { name: 'Post reply' });
  const [left, right] = await Promise.all([attachment.boundingBox(), post.boundingBox()]);
  const sameRow = await attachment.evaluate(el => el.closest('.discussion-input-actions').clientWidth);
  assert(right.x - left.x >= sameRow / 2, 'attachment and send are at opposite ends of the composer');
  await region.getByText('FR', { exact: true }).first().waitFor();
  const composerTop = () => composer.evaluate(el => el.getBoundingClientRect().top + scrollY - 125);
  await composer.getByLabel('Your comment').fill('Keep this draft');
  await snapshot(page, 'reply', suffix, await composerTop());
  const input = composer.locator('[name="imageFile"]');
  await input.setInputFiles({ name: 'attachment.png', mimeType: 'image/png', buffer: tilePng });
  const alt = composer.getByLabel('Describe the image');
  await alt.fill('An attachment');
  const clear = composer.getByRole('button', { name: 'Remove image' });
  await clear.waitFor();
  if (!touch) {
    await page.mouse.move(0, 0);
    assert.equal(await clear.evaluate(el => getComputedStyle(el).opacity), '0');
    await composer.locator('[data-image-preview]').hover({ position: { x: 1, y: 1 } });
    assert.equal(await clear.locator('span').evaluate(el => getComputedStyle(el).backgroundColor), 'rgb(52, 52, 59)');
    assert.equal(await clear.locator('span').evaluate(el => getComputedStyle(el).boxShadow), 'none');
    await clear.hover();
    await page.waitForFunction(() => document.querySelector('[data-clear-image]:hover')?.matches(':hover') &&
      getComputedStyle(document.querySelector('[data-clear-image] span')).backgroundColor === 'rgb(134, 29, 44)', null, { timeout: 2000 });
    await page.mouse.move(0, 0);
    await alt.press('Shift+Tab');
    assert(await clear.evaluate(el => el === document.activeElement));
    await page.waitForFunction(() => getComputedStyle(document.querySelector('[data-clear-image] span')).backgroundColor === 'rgb(134, 29, 44)', null, { timeout: 2000 });
  }
  assert.equal(await clear.evaluate(el => getComputedStyle(el).opacity), '1');
  await snapshot(page, 'attachment', suffix, await composerTop());
  await clear.click();
  const attach = composer.getByRole('button', { name: 'Attach image' });
  assert(await attach.evaluate(el => el === document.activeElement));
  await input.setInputFiles({ name: 'attachment.png', mimeType: 'image/png', buffer: tilePng });
  await alt.fill('An attachment');
  let writes = 0;
  let postStatus;
  let finishPost;
  const pending = new Promise(resolve => { finishPost = resolve; });
  await page.route('**/Comments', async route => {
    writes++;
    const multipart = route.request().postData();
    for (const field of ['body', 'parentId', 'imageFile', 'imageAlt', '__RequestVerificationToken']) assert(multipart.includes(`name="${field}"`));
    if (postStatus === 400) await pending;
    return route.fulfill({ status: postStatus, json: postStatus === 201
      ? { ...comments[1], id: 'canonical', parentId: '1', body: 'Canonical reply' }
      : { error: `Fixture error ${postStatus}` } });
  });
  const send = composer.getByRole('button', { name: 'Post reply' });
  for (postStatus of [400, 413, 503, 201]) {
    const done = page.waitForResponse(response => response.url().endsWith('/Comments') && response.request().method() === 'POST');
    await send.evaluate(el => { el.form.requestSubmit(); el.form.requestSubmit(); });
    if (postStatus === 400) {
      await composer.getByText('Posting…').waitFor();
      await snapshot(page, 'posting', suffix, await composerTop());
      finishPost();
    }
    await done;
    if (postStatus !== 201) {
      await composer.getByRole('alert').waitFor();
      assert.equal(await composer.getByLabel('Your comment').inputValue(), 'Keep this draft');
      assert.equal(await composer.locator('[name="parentId"]').inputValue(), '1');
      assert.equal(await alt.inputValue(), 'An attachment');
      assert.equal(await input.evaluate(el => el.files[0].name), 'attachment.png');
      if (postStatus === 413) await snapshot(page, 'post-error', suffix, await composerTop());
    }
  }
  await region.getByText('Canonical reply', { exact: true }).waitFor();
  assert.equal(await composer.getByLabel('Your comment').inputValue(), '');
  assert.equal(await input.evaluate(el => el.files.length), 0);
  postStatus = 409;
  await composer.getByLabel('Your comment').fill('Preserve this after resolution');
  await composer.getByRole('button', { name: 'Post comment' }).click();
  await page.locator('[data-comment-readonly]').waitFor();
  assert.equal(await composer.getByLabel('Your comment').inputValue(), 'Preserve this after resolution');
  assert.equal(await composer.getByRole('button', { name: 'Post comment' }).isEnabled(), false);
  assert.equal(writes, 5);
  await page.unroute('**/Comments?*');
}

(async () => {
  const browser = await chromium.launch({ executablePath });
  const referenceGeometry = [];
  try {
    for (const width of [1440, 390]) {
      for (const colorScheme of ['light', 'dark']) {
        const context = await browser.newContext({ viewport: { width, height: width === 390 ? 844 : 1040 }, colorScheme, hasTouch: width === 390, reducedMotion: 'reduce' });
        await context.addCookies([{ name, value, url: base }]);
        const page = await context.newPage();
        const suffix = `${width}-${colorScheme}`;
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const appErrors = [];
        page.on('response', response => {
          if (response.url().startsWith(`${base}/`) && response.status() >= 400 &&
              !(response.status() === 404 && response.url().includes(`/Items/Image/${ids[2]}`))) {
            appErrors.push(`${response.status()} ${response.url()}`);
          }
        });
        let tileFails = true;
        let releaseTiles;
        const tileGate = new Promise(resolve => { releaseTiles = resolve; });
        await page.route(tile, async route => {
          await tileGate;
          return tileFails ? route.abort() : route.fulfill({ contentType: 'image/png', body: tilePng });
        });
        await visit(page);

        const description = page.locator('#report-description');
        const toggle = page.getByRole('button', { name: 'Read more' });
        await toggle.waitFor({ timeout: 4500 });
        assert.equal(await toggle.getAttribute('aria-controls'), 'report-description');
        assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
        assert(await description.evaluate(el => Math.abs(el.clientHeight - 8 * parseFloat(getComputedStyle(el).lineHeight)) <= 1));
        const map = page.locator('[data-report-map="preview"]');
        const canvas = map.locator('[data-map-canvas]');
        await page.locator('[data-map-state="loading"]').waitFor();
        const canvasHeight = (await canvas.boundingBox()).height;
        if (evidence) await snapshot(page, 'map-loading', suffix, width === 390 ? Math.max(0, (await map.boundingBox()).y - 130) : 0);
        releaseTiles();
        await map.getByRole('button', { name: 'Retry map preview' }).waitFor();
        assert.equal((await canvas.boundingBox()).height, canvasHeight);
        if (evidence) await snapshot(page, 'map-error', suffix, await page.evaluate(() => scrollY));
        tileFails = false;
        await map.getByRole('button', { name: 'Retry map preview' }).click();
        await page.locator('[data-map-state="ready"]').waitFor();
        assert.equal((await canvas.boundingBox()).height, canvasHeight);
        const coordinates = map.getByText('Show coordinates');
        assert.equal(await map.getByText('20.296058, 85.824539').isVisible(), false);
        await coordinates.press('Enter');
        await map.getByText('20.296058, 85.824539').waitFor();
        if (evidence) await snapshot(page, 'coordinates', suffix, await page.evaluate(() => scrollY));
        await coordinates.press('Enter');
        await page.getByRole('link', { name: 'Submit a claim' }).waitFor();
        assert.equal(await page.getByRole('link', { name: 'Edit report' }).count(), 0);
        assert.equal(await page.getByRole('link', { name: /Back to results/ }).getAttribute('href'), '/Items');
        assert.equal(await page.getByRole('heading', { level: 1 }).textContent(), 'W'.repeat(120));
        await page.getByText('Message me', { exact: true }).waitFor();
        await page.locator('.detail-report__location-fact').getByText('Library', { exact: false }).waitFor();
        const photo = page.locator('.detail-report__media img');
        await photo.evaluate(el => el.decode());
        const bounds = await page.locator('.detail-report__photo').boundingBox();
        assert.equal(await page.locator('html').getAttribute('data-theme'), colorScheme);
        await reflow(page);
        const rail = page.locator('.detail-report__media-column');
        const [title, preview, body] = await Promise.all([page.locator('h1').boundingBox(), canvas.boundingBox(), description.boundingBox()]);
        const [facts, claim] = await Promise.all([page.locator('.detail-report__facts').boundingBox(), page.getByRole('complementary', { name: 'Think this is yours?' }).boundingBox()]);
        referenceGeometry.push({ width, theme: colorScheme, photo: [bounds.width, bounds.height], map: [preview.width, preview.height], titleBeforeMedia: title.y < bounds.y, mapBeforeDescription: preview.y < body.y, claimBeforeFacts: claim.y < facts.y });
        if (width === 390) {
          assert.equal(await rail.evaluate(el => getComputedStyle(el).position), 'static');
        } else {
          await page.waitForFunction(() => getComputedStyle(document.querySelector('.detail-report__media-column')).position === 'sticky');
          await page.evaluate(() => scrollTo(0, 150));
          await page.waitForFunction(() => {
            const rail = document.querySelector('.detail-report__media-column');
            return Math.abs(rail.getBoundingClientRect().top - parseFloat(getComputedStyle(rail).top)) < 1;
          });
        }
        if (width === 390) await toggle.tap();
        else await toggle.press('Enter');
        const less = page.getByRole('button', { name: 'Show less' });
        assert.equal(await less.getAttribute('aria-expanded'), 'true');
        assert(await description.evaluate(el => el.clientHeight === el.scrollHeight));
        // Expansion moves the button; explicitly bring it back into view before keyboard activation.
        await less.scrollIntoViewIfNeeded();
        await less.focus();
        assert(await less.evaluate(el => el === document.activeElement));
        await page.keyboard.press('Space');
        await toggle.waitFor({ state: 'visible', timeout: 5000 });
        assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
        if (evidence) {
          await fs.mkdir(evidence, { recursive: true });
          await page.evaluate(() => scrollTo(0, 0));
          await page.screenshot({ path: path.join(evidence, `details-${width}-${colorScheme}.png`), fullPage: true });
        }
        for (const [index, status, fallback] of [[1, 'Lost', 'No photo provided'], [2, 'Claimed', 'Photo unavailable'], [3, 'Resolved', 'Photo unavailable']]) {
          await visit(page, index);
          await page.getByText(status, { exact: true }).waitFor();
          await page.getByText(fallback, { exact: true }).waitFor();
          if (index === 2) await page.getByText('The report photo could not load.').waitFor();
          const current = await page.locator('.detail-report__photo').boundingBox();
          assert.deepEqual([current.width, current.height], [bounds.width, bounds.height]);
          assert.equal(await page.getByRole('link', { name: 'Submit a claim' }).count(), 0);
          assert.equal(await page.getByRole('link', { name: 'Edit report' }).count(), index === 1 ? 1 : 0);
          assert.equal(await page.getByRole('button', { name: 'Delete report' }).count(), index === 2 ? 0 : 1);
          assert.equal(await page.locator('[data-comment-form]').isVisible(), index === 1);
          await page.getByText('Map location not added to this older report.').waitFor();
          assert.equal(await page.getByRole('button', { name: 'Read more' }).count(), 0);
          await reflow(page);
          if (index === 1) {
            const opener = page.getByRole('button', { name: 'Mark resolved' });
            await opener.press('Enter');
            const modal = page.getByRole('dialog', { name: 'Mark this report resolved?' });
            await modal.waitFor({ timeout: 2000 });
            const cancel = modal.getByRole('button', { name: 'Cancel' });
            assert(await cancel.evaluate(el => el === document.activeElement));
            await page.keyboard.press('Shift+Tab');
            assert(await modal.getByRole('button', { name: 'Mark resolved' }).evaluate(el => el === document.activeElement));
            await page.keyboard.press('Escape');
            assert(await opener.evaluate(el => el === document.activeElement));
          }
        }
        if (width === 1440) {
          await page.setViewportSize({ width, height: 480 });
          await visit(page);
          assert.equal(await rail.evaluate(el => getComputedStyle(el).position), 'static');
          const discussion = page.getByRole('heading', { name: 'Public discussion' });
          await discussion.scrollIntoViewIfNeeded();
          assert(await page.evaluate(() => scrollY > 0));
          await reflow(page);
          for (const breakpointWidth of [896, 895, 320]) {
            await page.setViewportSize({ width: breakpointWidth, height: 850 });
            await visit(page);
            await reflow(page);
            assert.equal((await rail.boundingBox()).x < (await page.locator('h1').boundingBox()).x, breakpointWidth >= 896);
            await page.getByRole('link', { name: 'Submit a claim' }).click();
            const drawer = page.getByRole('dialog', { name: 'Submit a claim' });
            assert.equal((await drawer.boundingBox()).width, breakpointWidth >= 896 ? 432 : breakpointWidth);
            await page.keyboard.press('Escape');
            await drawer.waitFor({ state: 'hidden' });
          }
          await page.setViewportSize({ width, height: 1040 });
        }
        assert.deepEqual(errors, []);
        assert.deepEqual(appErrors, []);
        await discussionJourney(page, width === 390, suffix);
        await referenceStates(page, suffix);
        if (width === 1440 && colorScheme === 'light') {
          let posts = 0;
          await page.route(`**/Items/Resolve/${ids[1]}`, async route => {
            ++posts;
            assert.equal(route.request().method(), 'POST');
            assert(new URLSearchParams(route.request().postData()).has('__RequestVerificationToken'));
            await route.fulfill({ status: 204 });
          });
          await visit(page, 1);
          await page.getByRole('button', { name: 'Mark resolved' }).click();
          const confirm = page.getByRole('dialog').getByRole('button', { name: 'Mark resolved' });
          const done = page.waitForResponse(r => r.url().includes('/Items/Resolve/') && r.status() === 204);
          await confirm.evaluate(button => { button.form.requestSubmit(); button.form.requestSubmit(); });
          await done;
          assert.equal(posts, 1);
        }
        await context.close();
      }
    }
    const noScript = await browser.newContext({ javaScriptEnabled: false });
    const page = await noScript.newPage();
    const detailPath = `/Items/Details/${ids[0]}`;
    await page.goto(new URL(detailPath, base).href);
    const login = new URL(page.url());
    assert.equal(login.pathname, '/Account/Login');
    assert.equal(login.searchParams.get('ReturnUrl'), detailPath);
    await noScript.addCookies([{ name, value, url: base }]);
    await visit(page);
    assert.equal(await page.getByRole('button', { name: 'Read more' }).count(), 0);
    assert(await page.locator('#report-description').evaluate(el => el.clientHeight === el.scrollHeight));
    await noScript.close();
  } finally {
    await browser.close();
  }

  console.log(`PDF 05 geometry ${JSON.stringify(referenceGeometry)}`);
  for (const { width, theme, photo, map, titleBeforeMedia, mapBeforeDescription, claimBeforeFacts } of referenceGeometry) {
    assert.deepEqual(photo, width === 390 ? [350, 270] : [560, 368], `PDF 05 ${width} ${theme} photo`);
    assert.deepEqual(map, width === 390 ? [310, 184] : [560, 184], `PDF 05 ${width} ${theme} map`);
    if (width === 390) assert(titleBeforeMedia && mapBeforeDescription, 'PDF 05 phone puts title before media and description after map');
    assert.equal(claimBeforeFacts, width === 1440, 'PDF 05 claim placement changes at the layout transition');
  }

  const profile = await fs.mkdtemp(path.join(evidence || os.tmpdir(), 'details-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, { executablePath, channel: executablePath ? undefined : 'chromium', viewport: { width: 1440, height: 850 } });
    try {
      await context.addCookies([{ name, value, url: base }]);
      await context.route(tile, route => route.fulfill({ contentType: 'image/png', body: tilePng }));
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => Promise.race([
        new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)),
        new Promise((_, reject) => setTimeout(() => reject(new Error('Chrome zoom timed out')), 5000))
      ]));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      await visit(page);
      assert.deepEqual(await page.evaluate(() => [innerWidth, devicePixelRatio]), [720, 2]);
      await reflow(page);
      assert.equal(await page.locator('.detail-report__media-column').evaluate(el => getComputedStyle(el).position), 'static');
      await page.getByRole('heading', { name: 'Public discussion' }).scrollIntoViewIfNeeded();
      assert(await page.evaluate(() => scrollY > 0));
      const opener = page.getByRole('link', { name: 'Submit a claim' });
      await opener.press('Enter');
      const drawer = page.getByRole('dialog', { name: 'Submit a claim' });
      assert.equal((await drawer.boundingBox()).width, 720);
      assert(await drawer.evaluate(el => el.scrollWidth === el.clientWidth));
      const submit = drawer.getByRole('button', { name: 'Submit claim' });
      await submit.focus();
      // Normal motion is enabled here; hit-test after the drawer's entrance transform settles.
      await drawer.evaluate(el => Promise.all(el.getAnimations().map(animation => animation.finished)));
      const submitHit = await submit.evaluate(el => {
        const r = el.getBoundingClientRect();
        const hit = document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2);
        return { reachable: hit?.closest('button') === el, rect: r.toJSON(), viewport: [innerWidth, innerHeight], hit: hit?.className };
      });
      assert(submitHit.reachable, `Submit remains reachable at 200%: ${JSON.stringify(submitHit)}`);
      await page.keyboard.press('Escape');
      await drawer.waitFor({ state: 'hidden' });
      assert(await opener.evaluate(el => el === document.activeElement));
    } finally {
      await context.close();
    }
  } finally {
    await fs.rm(profile, { force: true, recursive: true });
  }
  console.log('Details Chromium matrix passed: 1440/390 light/dark, touch/keyboard, short height, native 200% zoom, photo/map fallback and role/status states.');
})().catch(error => { console.error(error); process.exitCode = 1; });
