// Run via Report_components_pass_the_browser_matrix (see docs/report-components.md).
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');
const browseJourneys = require('./browse-journeys.cjs');

const base = process.env.TRACEBACK_URL;
const cookie = process.env.TRACEBACK_TEST_COOKIE;
const evidence = process.env.TRACEBACK_REPORT_EVIDENCE;
const cardSelector = '.tb-report-card';
const initialFilters = { Query: 'old', Type: 'Lost', Status: 'Claimed', Category: 'Gear', Location: 'Hall', Page: '3' };

function contrast(fg, bg) {
  const luminance = color => color.match(/\d+/g).slice(0, 3).reduce((sum, value, index) => {
    const s = Number(value) / 255;
    return sum + [0.2126, 0.7152, 0.0722][index] * (s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4);
  }, 0);
  const levels = [luminance(fg), luminance(bg)].sort((a, b) => b - a);
  return (levels[0] + 0.05) / (levels[1] + 0.05);
}

async function visit(page, pathname) {
  const response = await page.goto(new URL(pathname, base).href);
  assert.equal(response.status(), 200, pathname);
  assert.equal(new URL(page.url()).pathname, new URL(pathname, base).pathname);
}

async function cardMatrix(page, route, variant, width, colorScheme, imageResponses) {
  imageResponses.clear();
  await visit(page, route);
  const cards = page.locator(`${cardSelector}--${variant}`);
  await cards.last().waitFor();
  assert.equal(await cards.count(), 4);
  assert.equal(await page.locator('html').getAttribute('data-theme'), colorScheme);
  await page.evaluate(() => document.fonts.ready);
  assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), width, `${route} overflow`);

  const sizes = [];
  for (const [index, card] of (await cards.all()).entries()) {
    await card.scrollIntoViewIfNeeded();
    const id = await card.getAttribute('data-record-id');
    const href = `/Items/Details/${id}`;
    const imageUrl = `/Items/Image/${id}`;
    assert.equal(await card.locator('a,button,[tabindex]').count(), 1);
    assert.equal(await page.locator(`a[href="${href}"]`).count(), 1);
    const photo = card.locator('img');
    const media = card.locator('.tb-report-card__media');
    const before = await media.boundingBox();
    if (index === 1) {
      assert.equal(await photo.count(), 0);
      assert(!imageResponses.has(imageUrl));
      await card.getByText('No photo provided', { exact: true }).waitFor();
      assert.equal(await card.locator('svg use[href$="#image"]').count(), 1);
    } else {
      assert.equal(await photo.getAttribute('src'), imageUrl);
      if (index === 0) {
        await photo.waitFor();
        await photo.evaluate(img => img.decode());
        assert.equal(await card.getByText('Photo unavailable').isVisible(), false);
      } else {
        await card.getByText('Photo unavailable', { exact: true }).waitFor();
        assert.equal(await photo.isVisible(), false);
        assert.equal(await card.locator('svg use[href$="#image-off"]').count(), 1);
      }
      assert.equal(imageResponses.get(imageUrl), index === 2 ? 404 : 200);
    }
    const after = await media.boundingBox();
    assert.deepEqual([after.width, after.height], [before.width, before.height]);
    if (variant === 'home') assert(Math.abs(after.width / after.height - 4 / 3) < 0.002, 'Home uses 4:3 media');
    else assert.equal(after.height, 212, `${variant} media matches the 212px frame`);
    sizes.push([after.width, after.height]);

    if (variant === 'browse') {
      for (const selector of ['.tb-report-card__title', '.tb-report-card__location span']) {
        const [height, lineHeight, clamp, scrollHeight] = await card.locator(selector).evaluate(el => {
          const style = getComputedStyle(el);
          return [el.getBoundingClientRect().height, parseFloat(style.lineHeight), style.webkitLineClamp, el.scrollHeight];
        });
        assert(Math.abs(height - 2 * lineHeight) < 0.08, `${selector} reserves two lines`);
        assert.equal(clamp, '2');
        if (index === 0) assert(scrollHeight > height, `${selector} fixture must overflow`);
      }
    }
  }
  assert(sizes.every(([w, h]) => Math.abs(w - sizes[0][0]) < 1 && Math.abs(h - sizes[0][1]) < 1));
  const badges = await page.locator('.tb-status').evaluateAll(elements => elements.map(element => {
    const style = getComputedStyle(element);
    let background = style.backgroundColor;
    for (let ancestor = element.parentElement; background === 'rgba(0, 0, 0, 0)' && ancestor; ancestor = ancestor.parentElement) {
      background = getComputedStyle(ancestor).backgroundColor;
    }
    return [element.textContent.trim(), style.color, background];
  }));
  const expected = ['Found', 'Lost', 'Active', 'Claimed', 'Resolved'];
  if (variant === 'dashboard') expected.push('Pending', 'Approved', 'Rejected');
  assert.deepEqual([...new Set(badges.map(([text]) => text))].sort(), expected.sort());
  for (const [text, fg, bg] of badges) assert(contrast(fg, bg) >= 4.5, `${text} contrast in ${colorScheme}`);

  if (evidence) {
    await page.locator('.tb-report-grid').screenshot({ path: path.join(evidence, `${variant}-${width}-${colorScheme}.png`) });
  }
  // The next card is reached through the normal tab order, then Enter opens its true ID.
  const link = cards.first().getByRole('link');
  await cards.nth(1).getByRole('link').focus();
  await page.keyboard.press('Shift+Tab');
  assert(await link.evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
  assert.equal(await cards.first().evaluate(el => getComputedStyle(el).outlineStyle), 'solid');
  const href = await link.getAttribute('href');
  const fullTitle = await link.textContent();
  const fullLocation = await cards.first().locator('.tb-report-card__location span').textContent();
  await Promise.all([page.waitForURL(new URL(href, base).href), page.keyboard.press('Enter')]);
  assert.equal(await page.getByRole('heading', { level: 1 }).textContent(), fullTitle);
  assert(await page.getByText(fullLocation, { exact: false }).first().isVisible());
}

async function keyboardSearch(page, width, colorScheme) {
  await visit(page, `/?${new URLSearchParams(initialFilters)}`);
  const home = page.getByRole('search');
  const input = home.getByRole('searchbox', { name: 'Search reports' });
  assert.equal(await input.inputValue(), '');
  await input.fill('backpack & tag');
  await Promise.all([page.waitForURL(url => url.pathname === '/Items'), input.press('Enter')]);
  assert.deepEqual(Object.fromEntries(new URL(page.url()).searchParams), { Query: 'backpack & tag' });

  await visit(page, `/Items?${new URLSearchParams(initialFilters)}`);
  const search = page.getByRole('search');
  const browse = search.getByRole('searchbox', { name: 'Search words' });
  const clear = search.getByRole('button', { name: 'Clear search words' });
  assert.equal(await browse.inputValue(), initialFilters.Query);
  await browse.focus();
  await page.keyboard.press('Tab');
  assert(await clear.evaluate(button => button === document.activeElement && button.matches(':focus-visible')));
  await page.keyboard.press('Space');
  assert.equal(await browse.inputValue(), '');
  assert(await browse.evaluate(el => el === document.activeElement));
  assert.equal(await clear.isVisible(), false);
  assert.deepEqual(Object.fromEntries(new URL(page.url()).searchParams), initialFilters);
  for (const key of ['Type', 'Status', 'Category', 'Location']) {
    assert.equal(await search.locator(`[name="${key}"]`).inputValue(), initialFilters[key]);
  }
  await Promise.all([page.waitForURL(url => !url.searchParams.has('Page')), browse.press('Enter')]);
  const { Page: _page, ...filters } = initialFilters;
  assert.deepEqual(Object.fromEntries(new URL(page.url()).searchParams), { ...filters, Query: '' });
  assert.match(await page.locator('.browse-count').textContent(), /on page 1$/);
  if (evidence) await search.screenshot({ path: path.join(evidence, `search-${width}-${colorScheme}.png`) });
}

async function main() {
  assert(base && cookie, 'Run with the loopback report test host.');
  if (evidence) await fs.mkdir(evidence, { recursive: true });
  const i = cookie.indexOf('=');
  const auth = { name: cookie.slice(0, i), value: cookie.slice(i + 1), url: base };
  const chromiumPath = process.env.TRACEBACK_CHROMIUM_PATH;
  const browser = await chromium.launch(chromiumPath ? { executablePath: chromiumPath } : {});
  try {
    for (const width of [1440, 390]) {
      for (const colorScheme of ['light', 'dark']) {
        const context = await browser.newContext({ viewport: { width, height: width === 390 ? 844 : 1050 }, colorScheme, reducedMotion: 'reduce', hasTouch: width === 390 });
        try {
          await context.addCookies([auth]);
          const page = await context.newPage();
          page.setDefaultTimeout(5500);
          const pageErrors = [];
          const unexpectedResponses = [];
          page.on('pageerror', error => pageErrors.push(error.message));
          const imageResponses = new Map();
          page.on('response', response => {
            const url = new URL(response.url());
            if (url.pathname.startsWith('/Items/Image/')) imageResponses.set(url.pathname, response.status());
            else if (url.origin === base && response.status() >= 400) unexpectedResponses.push(`${response.status()} ${url.pathname}`);
          });
          await keyboardSearch(page, width, colorScheme);
          for (const [route, variant] of [['/', 'home'], ['/Items', 'browse'], ['/Dashboard', 'dashboard']]) {
            await cardMatrix(page, route, variant, width, colorScheme, imageResponses);
          }
          await browseJourneys(page, base, width, colorScheme, evidence);
          assert.deepEqual(pageErrors, []);
          assert.deepEqual(unexpectedResponses, ['503 /Items', '503 /Items']);
          console.log(`${width}px ${colorScheme}: Home/Browse search, three card variants, keyboard, media, clamp and badges passed`);
        } finally {
          await context.close();
        }
      }
    }
    const context = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 844 } });
    await context.addCookies([auth]);
    const page = await context.newPage();
    await visit(page, '/');
    const input = page.getByRole('searchbox');
    await input.fill('keys');
    await Promise.all([page.waitForURL('**/Items?Query=keys'), input.press('Enter')]);
    assert.match(await page.locator('.browse-count').textContent(), /on page 1$/);
    await page.getByLabel('Type', { exact: true }).selectOption('Found');
    await page.getByRole('button', { name: 'Apply filters' }).click();
    assert.equal(new URL(page.url()).searchParams.get('Type'), 'Found');
    console.log('No JavaScript: keyboard GET search and mobile filters passed');
    await context.close();
  } finally {
    await browser.close();
  }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
