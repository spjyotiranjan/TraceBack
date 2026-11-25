const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const cookie = process.env.TRACEBACK_TEST_COOKIE;
const evidence = process.env.TRACEBACK_BROWSE_EVIDENCE;
const evidencePhotos = evidence && process.env.TRACEBACK_BROWSE_EVIDENCE_PHOTOS;
const ids = process.env.TRACEBACK_TEST_ITEM_IDS.split(',');
const executablePath = process.env.TRACEBACK_CHROMIUM_PATH;
const auth = { name: cookie.slice(0, cookie.indexOf('=')), value: cookie.slice(cookie.indexOf('=') + 1), url: base };
const launch = executablePath ? { executablePath } : {};
const applied = '/Items?Query=Headphones&Type=Lost&Category=Electronics&Location=Engineering';
const selectors = ['.tb-result-board', '.tb-result-board h1', '.tb-result-board .btn', '.browse-filters', '#query', '.browse-filter-toggle', '.browse-filter-actions button', '.browse-applied', '#browse-results-title', '.tb-report-card--browse', '.tb-report-card__media', '.tb-recovery-content', '.browse-pagination'];

async function visit(page, route, status = 200) {
  assert.equal((await page.goto(new URL(route, base).href)).status(), status);
}

async function capture(page, column, row) {
  if (!evidence) return;
  await page.evaluate(() => document.fonts.ready);
  await page.screenshot({ path: path.join(evidence, `app-${row}-${column}.png`) });
  const metrics = await page.evaluate(selectors => Object.fromEntries(selectors.map(selector => {
    const el = document.querySelector(selector);
    if (!el || !el.getClientRects().length) return [selector, null];
    const { x, y, width, height } = el.getBoundingClientRect();
    const css = getComputedStyle(el);
    return [selector, { x, y, width, height, font: css.font, background: css.backgroundColor }];
  })), selectors);
  await fs.writeFile(path.join(evidence, `app-${row}-${column}.json`), JSON.stringify(metrics, null, 2));
}

async function check() {
  if (evidence) await fs.mkdir(evidence, { recursive: true });
  const browser = await chromium.launch(launch);
  let noActive;
  const geometry = [];
  try {
    for (const [column, [width, height, colorScheme]] of [
      [1440, 1050, 'light'], [1440, 1050, 'dark'], [390, 844, 'light'], [390, 844, 'dark']
    ].entries()) {
      const context = await browser.newContext({ viewport: { width, height }, colorScheme, reducedMotion: 'reduce', hasTouch: width === 390 });
      try {
        await context.addCookies([auth]);
        if (evidencePhotos) {
          for (const [index, id] of ids.slice(0, 4).entries()) {
            await context.route(new URL(`/Items/Image/${id}`, base).href, async route => {
              const photo = path.join(evidencePhotos, `${index}-${colorScheme}.png`);
              if (await fs.stat(photo).catch(() => null)) await route.fulfill({ path: photo });
              else await route.continue();
            });
          }
        }
        const page = await context.newPage();
        page.setDefaultTimeout(5000);
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await visit(page, '/Items');
        assert.equal(await page.locator('html').getAttribute('data-theme'), colorScheme);
        assert.equal(await page.getByLabel('Status', { exact: true }).inputValue(), 'Active');
        noActive = await page.getByRole('heading', { name: 'No active reports yet' }).count() !== 0;
        if (noActive) {
          await capture(page, column, 3);
          assert(await page.locator('.tb-recovery-content').getByRole('link', { name: 'Report an item' }).isVisible());
          await visit(page, '/Items?Page=2');
          assert(await page.getByText('Showing 0 reports on page 2', { exact: true }).isVisible());
          assert(await page.getByRole('link', { name: 'Back to page 1' }).isVisible());
          await capture(page, column, 4);
        } else {
          const cards = page.locator('.tb-report-card--browse');
          assert.equal(await cards.count(), 6);
          await cards.last().scrollIntoViewIfNeeded();
          await cards.last().getByText('Photo unavailable', { exact: true }).waitFor({ state: 'visible' });
          await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'instant' }));
          await capture(page, column, 0);
          geometry.push({ width, ...await page.evaluate(() => {
            const rect = selector => document.querySelector(selector).getBoundingClientRect();
            const filter = rect('.browse-filters');
            return {
              boardFill: getComputedStyle(document.querySelector('.tb-result-board')).backgroundColor,
              filterWidth: Math.round(filter.width), filterHeight: Math.round(filter.height),
              searchHeight: rect('#query').height, reportWidth: rect('.tb-result-board .btn').width,
              mediaHeight: Math.round(rect('.tb-report-card__media').height),
              toggleWidth: innerWidth < 896 ? Math.round(rect('.browse-filter-toggle').width) : null,
              applyWidth: Math.round(rect('.browse-filter-actions button').width)
            };
          }) });
          for (const [row, route, state] of [
            [1, applied, 'Showing 1 report on page 1'],
            [2, applied.replace('Headphones', 'Green+headphones'), 'No reports match these filters'],
            [5, `${applied}&Page=3`, 'Reports could not load'],
            [6, '/Items?Page=2', 'Showing 2 reports on page 2'],
            [8, '/Items?Type=Found', 'Showing 1 report on page 1']
          ]) {
            await visit(page, route, row === 5 ? 503 : 200);
            assert(await page.getByText(state, { exact: true }).isVisible());
            await capture(page, column, row);
            if (row === 6) {
              const pager = page.getByRole('navigation', { name: 'Result pages' });
              assert.equal(await pager.getByRole('link', { name: 'Previous', exact: true }).count(), 1);
              assert.equal(await pager.getByRole('link', { name: 'Next', exact: true }).count(), 0);
              assert.equal(await pager.getByText('Next', { exact: true }).getAttribute('aria-disabled'), 'true');
            }
          }
          if (width === 390) {
            await visit(page, '/Items');
            await page.getByRole('button', { name: 'Filters', exact: true }).tap();
            assert.equal(await page.getByRole('button', { name: 'Hide filters' }).getAttribute('aria-expanded'), 'true');
            await page.mouse.move(0, 0);
            await capture(page, column, 7);
          }
          await visit(page, '/Items');
          const links = await cards.evaluateAll(items => items.map(item => {
            const link = item.querySelector('h3 a');
            return { id: item.dataset.recordId, href: link.getAttribute('href'), title: link.textContent.trim() };
          }));
          assert.equal(new Set(links.map(link => link.href)).size, 6);
          for (const link of links) {
            assert(link.href.endsWith(`/${link.id}`));
            await visit(page, link.href);
            assert(await page.getByRole('heading', { level: 1, name: link.title, exact: true }).isVisible());
          }
        }
        assert.deepEqual(errors, []);
      } finally {
        await context.close();
      }
    }
    if (!noActive) {
      const context = await browser.newContext({ viewport: { width: 390, height: 844 }, javaScriptEnabled: false });
      await context.addCookies([auth]);
      const page = await context.newPage();
      await visit(page, '/Items');
      await page.getByLabel('Type', { exact: true }).selectOption('Found');
      await page.getByRole('button', { name: 'Apply filters' }).click();
      assert.equal(new URL(page.url()).searchParams.get('Type'), 'Found');
      assert.equal(await page.locator('.tb-report-card--browse').count(), 1);
      await context.close();
    }
  } finally {
    await browser.close();
  }
  if (!noActive) {
    const profile = await fs.mkdtemp(path.join(evidence || os.tmpdir(), 'browse-zoom-'));
    try {
      const context = await chromium.launchPersistentContext(profile, { ...launch, channel: 'chromium', viewport: { width: 1440, height: 1050 }, reducedMotion: 'reduce' });
      try {
        await context.addCookies([auth]);
        const page = context.pages()[0];
        await page.goto('chrome://settings/');
        await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
        assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
        await visit(page, applied);
        assert.deepEqual(await page.evaluate(() => [innerWidth, document.documentElement.scrollWidth, devicePixelRatio]), [720, 720, 2]);
        await page.getByRole('button', { name: 'Filters', exact: true }).press('Enter');
        await page.keyboard.press('Tab');
        assert(await page.getByLabel('Type', { exact: true }).evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
      } finally {
        await context.close();
      }
    } finally {
      await fs.rm(profile, { recursive: true, force: true, maxRetries: 6, retryDelay: 200 });
    }
  }
  for (const { width, boardFill, filterWidth, filterHeight, searchHeight, reportWidth, mediaHeight, toggleWidth, applyWidth } of geometry) {
    assert.equal(boardFill, 'rgba(0, 0, 0, 0)', 'Browse intro should be unframed');
    assert.equal(filterWidth, width === 390 ? 350 : 1200);
    assert.equal(filterHeight, width === 390 ? 154 : 186);
    assert.equal(searchHeight, 48);
    assert.equal(reportWidth, 180);
    assert.equal(mediaHeight, 212);
    if (toggleWidth !== null) assert(Math.abs(toggleWidth - applyWidth) <= 2);
  }
  console.log(`Browse ${noActive ? 'no-active' : 'populated'} viewport/theme matrix passed`);
}

check().catch(error => { console.error(error); process.exitCode = 1; });
