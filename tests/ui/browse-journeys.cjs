const assert = require('node:assert/strict');
const path = require('node:path');

module.exports = async function browseJourneys(page, base, width, colorScheme, evidence) {
  const filters = { Query: 'inventory & tag', Type: 'Found', Category: 'Gear', Location: 'North & Hall', Status: 'Claimed' };
  const resultUrl = (number, overrides = {}) => `/Items?${new URLSearchParams({ ...filters, ...overrides, Page: number })}`;
  const parameters = url => Object.fromEntries(new URL(url, base).searchParams);
  const visit = async (url, status = 200) => assert.equal((await page.goto(new URL(url, base).href)).status(), status);
  const cards = page.locator('.tb-report-card--browse');
  const nav = page.getByRole('navigation', { name: 'Result pages' });
  await visit('/Items?Type=Found');
  assert.equal(await page.getByLabel('Type', { exact: true }).inputValue(), 'Found');
  assert.equal(await page.getByLabel('Status', { exact: true }).inputValue(), 'Active');
  assert.equal(await page.getByRole('link', { name: 'Remove Type filter: Found' }).count(), 1);
  await visit('/Items');
  if (evidence) await page.screenshot({ path: path.join(evidence, `browse-default-${width}-${colorScheme}.png`), animations: 'disabled' });
  await visit(resultUrl(1));
  assert.equal(await cards.count(), 24);
  const cols = await page.locator('.tb-report-grid').evaluate(el => getComputedStyle(el).gridTemplateColumns.split(' ').length);
  assert.equal(cols, width === 390 ? 1 : 3);
  const next = nav.getByRole('link', { name: 'Next' });
  const previous = nav.getByRole('link', { name: 'Previous' });
  assert.equal(await previous.count(), 0);
  assert.deepEqual(parameters(await next.getAttribute('href')), { ...filters, Page: '2' });
  await Promise.all([page.waitForURL(url => url.searchParams.get('Page') === '2'), next.press('Enter')]);
  assert.equal(await cards.count(), 1);
  assert.equal(await next.count(), 0);
  assert.deepEqual(parameters(await previous.getAttribute('href')), { ...filters, Page: '1' });
  await previous.press('Enter');
  await page.waitForURL(url => url.searchParams.get('Page') === '1');
  assert.equal(await cards.count(), 24);
  await visit(resultUrl(2));

  const type = page.getByLabel('Type', { exact: true });
  const toggle = page.getByRole('button', { name: 'Filters', exact: true });
  if (width === 390) {
    assert.equal(await type.isVisible(), false);
    assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
    await toggle.press('Enter');
    const close = page.getByRole('button', { name: 'Hide filters' });
    assert.equal(await close.getAttribute('aria-expanded'), 'true');
    await page.keyboard.press('Tab');
    assert(await type.evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
    if (evidence) {
      await page.evaluate(() => window.scrollTo(0, 0));
      await page.screenshot({ path: path.join(evidence, `browse-expanded-${colorScheme}.png`) });
    }
    await close.click();
    assert.equal(await type.isVisible(), false);
    await toggle.click();
    await page.getByLabel('Location', { exact: true }).press('Escape');
    assert.equal(await type.isVisible(), false);
    assert(await toggle.evaluate(el => el === document.activeElement && el.matches(':focus-visible')));
    await toggle.click();
  } else {
    assert(await type.isVisible());
    assert.equal(await toggle.isVisible(), false);
  }
  await type.selectOption('Lost');
  await Promise.all([page.waitForURL(url => !url.searchParams.has('Page')), page.getByRole('button', { name: 'Apply filters' }).click()]);
  assert.deepEqual(parameters(page.url()), { ...filters, Type: 'Lost' });
  if (width === 390) assert.equal(await type.isVisible(), false);

  for (const [key, label] of [['Query', 'Search'], ['Type', 'Type'], ['Category', 'Category'], ['Location', 'Location'], ['Status', 'Status']]) {
    await visit(resultUrl(2));
    const chip = page.getByRole('link', { name: `Remove ${label} filter: ${filters[key]}`, exact: true });
    await chip.focus();
    assert(await chip.evaluate(el => el.matches(':focus-visible') && getComputedStyle(el).outlineStyle === 'solid'));
    assert(await chip.evaluate(el => parseFloat(getComputedStyle(el).transitionDuration) <= 0.00001));
    const { [key]: _removed, ...rest } = filters;
    await Promise.all([page.waitForURL(url => url.searchParams.get('Page') === '1'), chip.press('Enter')]);
    assert.deepEqual(parameters(page.url()), { ...rest, Page: '1' });
    if (key === 'Status') assert.equal(await page.getByLabel('Status', { exact: true }).inputValue(), 'Active');
  }

  await visit(resultUrl(8));
  const recover = page.getByRole('link', { name: 'Back to page 1' });
  assert.deepEqual(parameters(await recover.getAttribute('href')), { ...filters, Page: '1' });
  await recover.click();
  assert.equal(await cards.count(), 24);
  await visit(resultUrl(1, { Query: 'no matches' }));
  assert(await page.getByRole('heading', { name: 'No reports match these filters' }).isVisible());
  await page.locator('.tb-recovery-content').getByRole('link', { name: 'Clear filters' }).click();
  assert.equal(new URL(page.url()).search, '');

  const failureUrl = resultUrl(2, { Query: 'retry & check' });
  await visit(failureUrl, 503);
  const retry = page.getByRole('link', { name: 'Retry', exact: true });
  assert.equal(await retry.getAttribute('href'), failureUrl);
  await retry.scrollIntoViewIfNeeded();
  if (width === 390) {
    const midpoint = await retry.boundingBox();
    assert(await page.evaluate(({ x, y, width, height }) =>
      document.elementFromPoint(x + width / 2, y + height / 2)?.closest('a')?.textContent.trim() === 'Retry', midpoint));
  }
  if (evidence) await page.screenshot({ path: path.join(evidence, `browse-retry-${width}-${colorScheme}.png`) });
  const [retried] = await Promise.all([page.waitForNavigation(), retry.press('Enter')]);
  assert.equal(retried.status(), 503);
  assert.equal(new URL(page.url()).search, new URL(failureUrl, base).search);

  if (width === 390) {
    await visit('/Items');
    await page.setViewportSize({ width: 896, height: 550 });
    await type.waitFor({ state: 'visible' });
    await page.getByLabel('Location', { exact: true }).focus();
    await page.setViewportSize({ width: 895, height: 550 });
    await toggle.waitFor({ state: 'visible' });
    assert(await toggle.evaluate(el => el === document.activeElement));
    await page.setViewportSize({ width: 320, height: 470 });
    await toggle.tap();
    assert(await type.isVisible());
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth), 320);
    const apply = page.getByRole('button', { name: 'Apply filters' });
    await apply.scrollIntoViewIfNeeded();
    await Promise.all([page.waitForURL(url => url.searchParams.get('Status') === 'Active'), apply.tap()]);
    assert.equal(await type.isVisible(), false);
  }
  console.log(`${width}px ${colorScheme}: Browse full and last pages, filter disclosure, five chip links, GET reset, empty recovery, and same-query 503 passed`);
};
