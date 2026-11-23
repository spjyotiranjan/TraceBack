const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const fixture = JSON.parse(process.env.TRACEBACK_DASHBOARD_FIXTURE);
const evidence = process.env.TRACEBACK_DASHBOARD_EVIDENCE;
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
  await page.screenshot({ path: path.join(evidence, `${name}.png`), fullPage });
}

async function session(browser, member, width, colorScheme, javaScriptEnabled = true) {
  const context = await browser.newContext({ viewport: { width, height: width > 900 ? 1040 : 844 },
    colorScheme, reducedMotion: 'reduce', hasTouch: width < 896, javaScriptEnabled });
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
      for (const member of width === 1440 || width === 390 ? Object.keys(fixture.members) : ['empty', 'overview']) {
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
        assert.equal(await reports.locator('.tb-report-card--dashboard').count(), hasReports ? 1 : 0);
        if (hasReports) {
          assert.equal(await reports.locator('.tb-report-card--dashboard a').getAttribute('href'),
            details(member === 'overview' ? fixture.owned : fixture.report));
        }
        assert.equal(await reports.getByRole('heading', { name: 'No reports yet', level: 3 }).count(), hasReports ? 0 : 1);
        assert.equal(await claims.getByRole('heading', { name: 'No claims yet', level: 3 }).count(), hasClaims ? 0 : 1);
        if (!hasClaims) {
          const browse = claims.getByRole('link', { name: 'Browse found items' });
          assert.equal(new URL(await browse.getAttribute('href'), base).searchParams.get('Type'), 'Found');
        } else {
          const rows = claims.locator('.tb-claim-row');
          assert.deepEqual(await rows.locator('.tb-status').allTextContents().then(text => text.map(value => value.trim())),
            [...(member === 'overview' ? ['Pending'] : []), 'Approved', 'Rejected', 'Rejected']);
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
        if (member === 'overview' && width === 1440) {
          await keyboardOpen(page, reports.locator(`a[href="${details(fixture.owned)}"]`), details(fixture.owned));
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
          assert.equal(await page.locator('.tb-report-card--browse').count(), 3);
          await visit(page, '/Items?Type=Found&Query=not-an-existing-report');
          await page.getByRole('heading', { name: 'No reports match these filters' }).waitFor();
          await keyboardOpen(page, page.getByRole('link', { name: 'Clear filters' }), '/Items');
          await page.getByRole('heading', { name: 'Results' }).waitFor();
          assert.equal(await page.locator('.tb-report-card--browse').count(), 5);
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
  assert(await status.evaluate(el => el.compareDocumentPosition(document.querySelector('#my-reports')) & Node.DOCUMENT_POSITION_FOLLOWING));
  await capture(page, `${prefix}-${page.viewportSize().width}-${initialTheme}`);
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
  for (const route of [claimRoute, deleteRoute(fixture.owned)]) {
    const response = await anonymous.request.post(url(route), raw);
    assert.equal(response.status(), 302);
    assert.equal(new URL(response.headers().location, base).pathname, '/Account/Login');
  }
  await anonymous.close();

  const claim = await session(browser, 'claims', 390, 'dark', false);
  await visit(claim.page, claimRoute);
  assert.equal((await claim.context.request.post(url(claimRoute), raw)).status(), 400);
  await claim.page.getByLabel(/Ownership details/).fill('There is a crescent-shaped scratch near the lid.');
  await Promise.all([claim.page.waitForURL(url('/Dashboard')), claim.page.getByRole('button', { name: 'Submit claim' }).click()]);
  const submitted = claim.page.locator('.tb-claim-row').filter({ hasText: 'Blue water bottle' });
  assert.equal(await submitted.getByRole('link').getAttribute('href'), details(fixture.target));
  assert.equal((await submitted.locator('.tb-status').textContent()).trim(), 'Pending');
  await notice(claim.page, 'Your claim was submitted.', 'claim-submitted', 'dark');
  assert.deepEqual(claim.errors, []);
  await claim.context.close();

  const owner = await session(browser, 'overview', 1440, 'light');
  await visit(owner.page, details(fixture.owned));
  const dialog = owner.page.getByRole('dialog', { name: 'Delete this report?' });
  await owner.page.getByRole('button', { name: 'Delete report' }).click();
  const token = await dialog.locator('input[name="__RequestVerificationToken"]').inputValue();
  assert(token);
  assert.equal((await owner.context.request.post(url(deleteRoute(fixture.owned)), raw)).status(), 400);
  const denied = await owner.context.request.post(url(deleteRoute(fixture.target)), { form: { __RequestVerificationToken: token }, maxRedirects: 0 });
  assert.equal(denied.status(), 302);
  assert.equal(new URL(denied.headers().location, base).pathname, '/Account/Denied');
  await Promise.all([owner.page.waitForURL(url('/Dashboard')), dialog.getByRole('button', { name: 'Delete report' }).click()]);
  await owner.page.getByRole('heading', { name: 'No reports yet' }).waitFor();
  assert.equal(await owner.page.locator('.tb-claim-row').count(), 4);
  assert.equal(await owner.page.locator(`main a[href="${details(fixture.owned)}"]`).count(), 0);
  await notice(owner.page, 'The report and its claims were deleted.', 'report-deleted', 'light');
  assert.deepEqual(owner.errors, []);
  await owner.context.close();
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    await dashboardMatrix(browser);
    await nativeZoom();
    await writeJourneys(browser);
    console.log('Dashboard: four real member states at 1440/390 light/dark; 896/895/320 layouts and native 200% zoom; keyboard/touch, filtered Browse, and one-time claim/delete notices passed.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
