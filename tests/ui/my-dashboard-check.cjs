const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const fixture = JSON.parse(process.env.TRACEBACK_DASHBOARD_FIXTURE);
const evidence = process.env.TRACEBACK_DASHBOARD_EVIDENCE;
const referencePhotos = process.env.DASHBOARD_REFERENCE_PHOTOS;
const baselineCapture = process.env.DASHBOARD_CAPTURE_BASELINE === '1';
const states = ['overview', 'reports', 'claims', 'empty'];
const details = id => `/Items/Details/${id}`;
const claimRoute = `/Claims/Create?itemId=${fixture.target}`;
const deleteRoute = id => `/Items/Delete/${id}`;
const url = route => new URL(route, base).href;

async function visit(page, route) {
  assert.equal((await page.goto(url(route))).status(), 200, route);
  assert.equal(new URL(page.url()).pathname, new URL(url(route)).pathname);
}

async function capture(page, name, fullPage = true) {
  if (!evidence) return;
  await fs.mkdir(evidence, { recursive: true });
  if (fullPage) {
    await page.evaluate(() => document.fonts.ready.then(() => true));
    await page.locator('main img[data-report-photo]').evaluateAll(imgs => Promise.all(imgs.map(img => {
      img.loading = 'eager';
      return img.decode().catch(() => {});
    })));
    await page.screenshot({ path: path.join(evidence, `${name}-viewport.png`) });
    const geometry = await page.locator('main').evaluate(main => Object.fromEntries([
      'h1', '#my-reports', '#my-claims', '.tb-report-card', '.tb-report-card__media',
      '.tb-claim-row', '.tb-claim-row .tb-status', '.tb-recovery-content', '.tb-feedback'
    ].map(selector => [selector, [...main.querySelectorAll(selector)].map(el => {
      const { x, y, width, height } = el.getBoundingClientRect();
      return { x, y: y + scrollY, width, height, font: getComputedStyle(el).fontSize };
    })])));
    await fs.writeFile(path.join(evidence, `${name}.json`), JSON.stringify(geometry));
  }
  await page.screenshot({ path: path.join(evidence, `${name}.png`), fullPage });
}

async function dashboardPhotos(context, width, theme) {
  await context.route('**/*', route => new URL(route.request().url()).origin === new URL(base).origin
    ? route.continue() : route.abort());
  await context.route(url('/__dashboard-photo/*'), async route => {
    const photo = new URL(route.request().url()).pathname.split('/').at(-1);
    if (photo === 'failure') return route.fulfill({ status: 404 });
    if (referencePhotos && [1440, 390].includes(width)) {
      return route.fulfill({ path: path.join(referencePhotos, `${photo}-${width}-${theme}.png`) });
    }
    return route.fulfill({ contentType: 'image/svg+xml', body: '<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300"><rect width="400" height="300" fill="#b5bdde"/></svg>' });
  });
}

async function session(browser, member, width, colorScheme, javaScriptEnabled = true) {
  const context = await browser.newContext({ viewport: { width, height: width > 900 ? 1040 : 844 },
    colorScheme, reducedMotion: 'reduce', hasTouch: width < 896, javaScriptEnabled });
  await dashboardPhotos(context, width, colorScheme);
  await context.addCookies([{ name: fixture.name, value: fixture.members[member], url: base }]);
  const page = await context.newPage();
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => { if (response.status() >= 500) errors.push(`${response.status()} ${response.url()}`); });
  return { context, page, errors };
}

async function keyboardOpen(page, link, target) {
  await link.focus();
  await page.keyboard.press('Tab');
  await page.keyboard.press('Shift+Tab');
  assert(await link.evaluate(el => document.activeElement === el && el.matches(':focus-visible')));
  await inFrontOfShell(link);
  await Promise.all([page.waitForURL(url(target)), page.keyboard.press('Enter')]);
}

async function inFrontOfShell(link) {
  assert(await link.evaluate(el => {
    const rect = el.getClientRects()[0];
    if (!rect) return false;
    return el.contains(document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2));
  }), 'keyboard focus must not be covered by the fixed shell');
}

async function dashboardMatrix(browser) {
  for (const width of [1440, 390, 896, 895, 320]) {
    for (const theme of width === 1440 || width === 390 ? ['light', 'dark'] : ['dark']) {
      for (const member of width === 1440 || width === 390 ? states : ['empty', 'overview']) {
        const { context, page, errors } = await session(browser, member, width, theme);
        await visit(page, '/Dashboard');
        const reports = page.getByRole('region', { name: /^My reports/ });
        const claims = page.getByRole('region', { name: /^My claims/ });
        assert.deepEqual(await page.locator('main h2').allTextContents(), ['My reports', 'My claims']);
        assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
        assert(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches));
        assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width, `overflow at ${width} ${member}`);
        assert.equal(await reports.getByRole('link', { name: 'Report an item' }).count() > 0 &&
          await reports.getByRole('link', { name: 'Report an item' }).first().isVisible(), width >= 896);
        assert.equal(await page.locator('.tb-member-dock').isVisible(), width < 896);
        assert.equal(await page.locator('.tb-member-dock a[href="/Items/Create"]').getAttribute('href'), '/Items/Create');

        const hasReports = member === 'overview' || member === 'reports';
        const hasClaims = member === 'overview' || member === 'claims';
        const ownIds = member === 'overview' ? fixture.owned : member === 'reports' ? fixture.reports : [];
        assert.deepEqual(await reports.locator('.tb-report-card--dashboard a').evaluateAll(links => links.map(a => a.getAttribute('href'))), ownIds.map(details));
        assert.equal(await reports.getByRole('heading', { name: 'No reports yet', level: 3 }).count(), hasReports ? 0 : 1);
        assert.equal(await claims.getByRole('heading', { name: 'No claims yet', level: 3 }).count(), hasClaims ? 0 : 1);
        if (!hasClaims) {
          const browse = claims.getByRole('link', { name: 'Browse found items' });
          assert.equal(new URL(await browse.getAttribute('href'), base).searchParams.get('Type'), 'Found');
        } else {
          const rows = claims.locator('.tb-claim-row');
          assert.deepEqual(await rows.locator('.tb-status').allTextContents().then(text => text.map(value => value.trim())),
            [...(member === 'claims' ? ['Pending'] : []), 'Approved', 'Rejected', 'Rejected']);
          const link = claims.getByRole('link', { name: fixture.title });
          assert.equal(await link.getAttribute('href'), details(fixture.umbrella));
          assert.equal(await link.locator('..').locator('..').locator('time').textContent(), fixture.date);
          assert(await link.evaluate(el => {
            const range = document.createRange();
            range.selectNodeContents(el);
            return [...range.getClientRects()].every(r => r.right <= document.documentElement.clientWidth);
          }));
          assert.equal(await rows.last().getByText('Item no longer available').count(), 1);
          assert.equal(await rows.last().getByRole('link').count(), 0);
          assert.equal(await claims.getByText(fixture.privateMessage).count(), 0);
          assert.equal(await claims.locator(`a[href="${details(fixture.missing)}"]`).count(), 0);
        }
        assert.equal(await page.locator('main a[href="/Items/Create"]:visible').count(), width >= 896 ? 1 : 0);
        if (width === 1440 || width === 390) await capture(page, `${member}-${width}-${theme}`);
        if (!baselineCapture && [1440, 390].includes(width)) await parityGeometry(page, width, hasReports, hasClaims);
        if (member === 'overview' && width === 1440) {
          const own = fixture.owned[0];
          await keyboardOpen(page, reports.locator(`a[href="${details(own)}"]`), details(own));
          await keyboardOpen(page, page.getByRole('link', { name: 'Edit report' }), `/Items/Edit/${own}`);
          await visit(page, '/Dashboard');
          await keyboardOpen(page, claims.getByRole('link', { name: fixture.title }), details(fixture.umbrella));
        }
        if (member === 'overview' && width === 390) {
          const link = claims.getByRole('link', { name: fixture.title });
          await keyboardOpen(page, link, details(fixture.umbrella));
          await visit(page, '/Dashboard/Index');
          await link.tap();
          await page.waitForURL(url(details(fixture.umbrella)));
        }
        if (member === 'empty' && width === 390) {
          const privacy = page.getByRole('link', { name: 'Privacy' });
          await privacy.scrollIntoViewIfNeeded();
          await inFrontOfShell(privacy);
          await capture(page, `empty-scrolled-${width}-${theme}`, false);
          const browse = claims.getByRole('link', { name: 'Browse found items' });
          await keyboardOpen(page, browse, await browse.getAttribute('href'));
          assert.equal(new URL(page.url()).searchParams.get('Type'), 'Found');
          assert(await page.locator(`.tb-report-card--browse[data-record-id="${fixture.target}"]`).isVisible());
          assert.equal(await page.locator(`.tb-report-card--browse[data-record-id="${fixture.owned[1]}"]`).count(), 0);
          await visit(page, '/Items?Type=Found&Query=not-an-existing-report');
          await page.getByRole('heading', { name: 'No reports match these filters' }).waitFor();
          await keyboardOpen(page, page.getByRole('link', { name: 'Clear filters' }), '/Items');
          await page.getByRole('heading', { name: 'Results' }).waitFor();
          assert(await page.locator(`.tb-report-card--browse[data-record-id="${fixture.owned[1]}"]`).isVisible());
          await visit(page, '/Dashboard');
          await page.locator('.tb-member-dock a[href="/Items/Create"]').tap();
          await page.waitForURL(url('/Items/Create'));
        }
        if (member === 'empty' && width === 1440) {
          await keyboardOpen(page, reports.getByRole('link', { name: 'Report an item' }), '/Items/Create');
        }
        assert.deepEqual(errors, []);
        await context.close();
      }
    }
  }
}

async function parityGeometry(page, width, hasReports, hasClaims) {
  const desktop = width === 1440;
  const heading = page.getByRole('heading', { level: 1 });
  assert.equal((await heading.boundingBox()).x, desktop ? 120 : 20, 'align intro with the reference content grid');
  assert.equal(await heading.evaluate(el => getComputedStyle(el).fontSize), desktop ? '48px' : '34px');
  assert.equal(await page.locator('#my-reports').evaluate(el => getComputedStyle(el).fontSize), '26px');
  const reports = page.getByRole('region', { name: 'My reports' });
  const claims = page.getByRole('region', { name: 'My claims' });
  if (desktop) {
    const create = await reports.getByRole('link', { name: 'Report an item' }).boundingBox();
    assert.deepEqual([create.width, create.height], [hasReports ? 171 : 205, 48]);
  }
  if (hasReports) {
    const card = await reports.locator('.tb-report-card').first().boundingBox();
    assert.equal(card.width, desktop ? 384 : 350);
    assert.equal(card.height, 408);
    assert.equal((await reports.locator('.tb-report-card__media').first().boundingBox()).height, 212);
  } else {
    assert.equal((await reports.locator('.tb-recovery-content').boundingBox()).height, 148);
  }
  if (hasClaims) {
    const [first, second] = await claims.locator('.tb-claim-row').evaluateAll(rows => rows.map(row => row.getBoundingClientRect().toJSON()));
    assert.equal(second.top - first.bottom, 12);
    assert.equal((await claims.locator('.tb-status').first().boundingBox()).height, 28);
  } else {
    assert.equal((await claims.locator('.tb-recovery-content').boundingBox()).height, desktop ? 148 : 214);
    const browse = await claims.getByRole('link', { name: 'Browse found items' }).boundingBox();
    assert.deepEqual([browse.width, browse.height], [205, 48]);
  }
}

async function edgeCases(browser) {
  const { context, page, errors } = await session(browser, 'edge', 320, 'dark');
  await visit(page, '/Dashboard/Index');
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), 320);
  assert(await page.getByText('No photo provided').isVisible());
  const failure = page.getByRole('article').filter({ hasText: 'Failed photo' });
  await failure.scrollIntoViewIfNeeded();
  await failure.getByText('Photo unavailable').waitFor();
  await keyboardOpen(page, page.getByRole('link', { name: fixture.longTitle }), details(fixture.longTarget));
  await visit(page, '/Dashboard');
  const link = page.locator(`main a[href="${details(fixture.longReport)}"]`);
  await keyboardOpen(page, link, details(fixture.longReport));
  assert.equal(await page.getByRole('link', { name: 'Edit report' }).count(), 0);
  const denied = await context.request.get(url(`/Items/Edit/${fixture.owned[0]}`), { maxRedirects: 0 });
  assert.equal(denied.status(), 302);
  assert.equal(new URL(denied.headers().location, base).pathname, '/Account/Denied');
  assert.deepEqual(errors, []);
  await context.close();
}

async function nativeZoom() {
  const bin = path.join(process.cwd(), 'src', 'TraceBack', 'bin');
  await fs.mkdir(bin, { recursive: true });
  const profile = await fs.mkdtemp(path.join(bin, 'dashboard-zoom-'));
  try {
    const context = await chromium.launchPersistentContext(profile, {
      executablePath: process.env.CHROME_PATH || undefined,
      channel: process.env.CHROME_PATH ? undefined : 'chromium',
      viewport: { width: 640, height: 1000 }, deviceScaleFactor: 1, colorScheme: 'dark', reducedMotion: 'reduce'
    });
    try {
      await dashboardPhotos(context, 320, 'dark');
      await context.addCookies([{ name: fixture.name, value: fixture.members.overview, url: base }]);
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      await visit(page, '/Dashboard');
      assert.deepEqual(await page.evaluate(() => [innerWidth, devicePixelRatio, document.documentElement.scrollWidth]), [320, 2, 320]);
      assert(await page.locator('.tb-member-dock').isVisible());
      await keyboardOpen(page, page.getByRole('link', { name: fixture.title }), details(fixture.umbrella));
    } finally {
      await context.close();
    }
  } finally {
    await fs.rm(profile, { force: true, recursive: true, maxRetries: 5, retryDelay: 100 });
  }
}

async function notice(page, message, prefix, initialTheme) {
  const status = page.getByRole('status').filter({ hasText: message });
  await status.waitFor();
  assert.equal(await status.count(), 1);
  const dark = await page.evaluate(() => getComputedStyle(document.documentElement).colorScheme === 'dark');
  assert.equal(dark, initialTheme === 'dark');
  assert(await status.evaluate(el => el.compareDocumentPosition(document.querySelector('#my-reports')) & Node.DOCUMENT_POSITION_FOLLOWING));
  await capture(page, `${prefix}-${page.viewportSize().width}-${initialTheme}`);
  if (!baselineCapture) {
    assert(await status.evaluate(el => document.querySelector('main h1').compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING),
      'dashboard notices belong after the page introduction');
    const box = await status.locator('..').boundingBox();
    assert.equal(box.height, page.viewportSize().width === 1440 ? 56 : 64);
  }
  await page.reload();
  assert.equal(await status.count(), 0, 'success notice is consumed by the visible Dashboard GET');
}

async function writeJourneys(browser) {
  const anonymous = await browser.newContext();
  const raw = { form: {}, maxRedirects: 0 };
  for (const route of ['/Dashboard', '/Dashboard/Index']) {
    const response = await anonymous.request.get(url(route), { maxRedirects: 0 });
    assert.equal(response.status(), 302);
    assert.equal(new URL(response.headers().location, base).pathname, '/Account/Login');
  }
  for (const route of [claimRoute, deleteRoute(fixture.owned[0])]) {
    const response = await anonymous.request.post(url(route), raw);
    assert.equal(response.status(), 302);
    assert.equal(new URL(response.headers().location, base).pathname, '/Account/Login');
  }
  await anonymous.close();

  for (const width of [1440, 390]) {
    for (const theme of ['light', 'dark']) await writeVariant(browser, width, theme);
  }
}

async function writeVariant(browser, width, theme) {
  const variant = `${width}-${theme}`;
  const raw = { form: {}, maxRedirects: 0 };
  const claim = await session(browser, `submit-${variant}`, width, theme, theme !== 'dark');
  await visit(claim.page, claimRoute);
  assert.equal((await claim.context.request.post(url(claimRoute), raw)).status(), 400);
  await claim.page.getByLabel(/Ownership details/).fill('There is a crescent-shaped scratch near the lid.');
  await Promise.all([claim.page.waitForURL(url('/Dashboard')), claim.page.getByRole('button', { name: 'Submit claim' }).click()]);
  const submitted = claim.page.locator('.tb-claim-row').filter({ hasText: 'Blue water bottle' });
  assert.equal(await submitted.getByRole('link').getAttribute('href'), details(fixture.target));
  assert.equal((await submitted.locator('.tb-status').textContent()).trim(), 'Pending');
  await notice(claim.page, 'Your claim was submitted.', 'claim-submitted', theme);
  assert.deepEqual(claim.errors, []);
  await claim.context.close();

  const [deleted, ...remaining] = fixture.deletes[variant];
  const owner = await session(browser, `delete-${variant}`, width, theme);
  await visit(owner.page, details(deleted));
  const dialog = owner.page.getByRole('dialog', { name: 'Delete this report?' });
  await owner.page.getByRole('button', { name: 'Delete report' }).click();
  const token = await dialog.locator('input[name="__RequestVerificationToken"]').inputValue();
  assert(token);
  assert.equal((await owner.context.request.post(url(deleteRoute(deleted)), raw)).status(), 400);
  const denied = await owner.context.request.post(url(deleteRoute(fixture.target)), { form: { __RequestVerificationToken: token }, maxRedirects: 0 });
  assert.equal(denied.status(), 302);
  assert.equal(new URL(denied.headers().location, base).pathname, '/Account/Denied');
  await Promise.all([owner.page.waitForURL(url('/Dashboard')), dialog.getByRole('button', { name: 'Delete report' }).click()]);
  await owner.page.getByRole('heading', { name: 'No claims yet' }).waitFor();
  assert.deepEqual(await owner.page.locator('.tb-report-card a').evaluateAll(links => links.map(a => a.getAttribute('href'))), remaining.map(details));
  await notice(owner.page, 'The report and its claims were deleted.', 'report-deleted', theme);
  assert.equal((await owner.context.request.get(url(details(deleted)), { maxRedirects: 0 })).status(), 404);
  assert.deepEqual(owner.errors, []);
  await owner.context.close();
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    await dashboardMatrix(browser);
    await nativeZoom();
    await writeJourneys(browser);
    await edgeCases(browser);
    console.log('Dashboard: six real states at 1440/390 light/dark; 896/895/320 and native 200% zoom; keyboard/touch, filtered Browse, owner Edit, unavailable targets and photos, and one-time claim/delete notices passed.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
