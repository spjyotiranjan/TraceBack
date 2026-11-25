// Loopback MVC and disposable Mongo. Interceptions below simulate transport failure and a stale view.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { chromium } = require('playwright');

const base = process.env.TRACEBACK_URL;
const [photoId, id] = process.env.TRACEBACK_TEST_ITEM_IDS.split(',');
const [name, value] = process.env.TRACEBACK_TEST_COOKIE.split(/=(.*)/s);
const details = `${base}/Items/Details/${id}`;
const evidence = process.env.TRACEBACK_CLAIM_EVIDENCE;
const claimRoute = '**/Claims/Create?*';
const focused = locator => locator.evaluate(el => el === document.activeElement);
const filled = 'It has a small dent near the base and my initials are scratched under the lid. I last had it at the library on Tuesday.';

async function capture(page, name) {
  if (!evidence) return;
  await fs.mkdir(evidence, { recursive: true });
  await page.screenshot({ path: path.join(evidence, `${name}.png`), animations: 'disabled' });
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  const referenceGeometry = [];
  try {
    for (const width of [1440, 390]) {
      for (const theme of ['light', 'dark']) {
        const context = await browser.newContext({
          viewport: { width, height: width === 390 ? 844 : 1040 },
          colorScheme: theme, reducedMotion: theme === 'dark' ? 'reduce' : 'no-preference',
          hasTouch: width === 390,
        });
        await context.addCookies([{ name, value, url: base }]);
        const page = await context.newPage();
        const suffix = `${width}-${theme}`;
        // Do not request external tile imagery during claim journeys.
        await page.route('**/tile.openstreetmap.org/**', route => route.abort());
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        await page.goto(details);
        const opener = page.getByRole('link', { name: 'Submit a claim' });
        await opener.press('Enter');
        const drawer = page.getByRole('dialog', { name: 'Submit a claim' });
        await drawer.waitFor({ timeout: 3000 });
        await drawer.evaluate(el => Promise.all(el.getAnimations().map(animation => animation.finished)));
        assert.equal(await drawer.evaluate(el => el.matches(':modal')), true);
        assert.equal(await drawer.evaluate(el => el.offsetWidth), width === 390 ? 390 : 432);
        assert.equal(await drawer.evaluate(el => el.scrollWidth === el.clientWidth), true);
        assert.equal(await drawer.getByText('W'.repeat(120)).count(), 1);
        await drawer.getByText('No photo').waitFor();
        const close = drawer.getByRole('button', { name: 'Close claim drawer' });
        const submit = drawer.getByRole('button', { name: 'Submit claim' });
        const message = drawer.getByRole('textbox', { name: /ownership details/i });
        await close.press('Shift+Tab');
        assert(await submit.evaluate(el => el === document.activeElement));
        await page.keyboard.press('Tab');
        assert(await close.evaluate(el => el === document.activeElement));
        await page.locator('main > a').first().evaluate(el => el.focus());
        assert(await close.evaluate(el => el === document.activeElement), 'background is inert');
        const scroll = drawer.getByRole('region', { name: 'Claim form' });
        if (width === 390) await page.setViewportSize({ width, height: 560 });
        await scroll.press('End');
        if (width === 390) await page.waitForFunction(() => document.querySelector('[data-claim-scroll]').scrollTop > 0);
        assert(await submit.evaluate(el => {
          const r = el.getBoundingClientRect();
          return document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2)?.closest('button') === el;
        }), 'action remains reachable at short viewport height');
        if (theme === 'dark') assert.equal(await drawer.evaluate(el => getComputedStyle(el).animationName), 'none');
        else assert.equal(await drawer.evaluate(el => getComputedStyle(el).animationName), 'claim-enter');
        await page.keyboard.press('Escape');
        await drawer.waitFor({ state: 'hidden' });
        assert(await opener.evaluate(el => el === document.activeElement));
        if (width === 390) await page.setViewportSize({ width, height: 844 });
        await page.emulateMedia({ reducedMotion: 'reduce' });
        if (width === 390) await opener.tap();
        else await opener.click();
        await capture(page, `long-title-${suffix}`);
        await message.fill('Blue');
        await drawer.getByText('4 / 1,000').waitFor();
        const validation = drawer.locator('[data-tb-validation-summary]');
        const invalidResponse = page.waitForResponse(res => res.url().includes('/Claims/Create?') && res.status() === 422);
        await submit.click();
        await invalidResponse;
        await validation.getByRole('link').waitFor();
        assert.equal(await message.inputValue(), 'Blue');
        assert.equal(await message.getAttribute('aria-invalid'), 'true');
        assert(await focused(validation));
        await validation.getByRole('link').press('Enter');
        assert(await focused(message));
        await capture(page, `validation-long-title-${width}-${theme}`);

        await page.goto(`${base}/Items/Details/${photoId}`);
        await opener.click();
        await drawer.getByText('Blue water bottle').waitFor();
        await page.waitForFunction(() => document.querySelector('[data-claim-drawer] img')?.naturalWidth > 0);
        const visual = await drawer.evaluate(el => {
          const box = selector => { const { x, y, width, height } = el.querySelector(selector).getBoundingClientRect(); return [x - el.getBoundingClientRect().x, y, width, height]; };
          const actions = el.querySelector('.claim-form__actions');
          return { header: box('header'), item: box('.claim-form__item'), privacy: box('.claim-form__privacy'), field: box('textarea'), footer: box('.claim-form__actions'), footerSurface: getComputedStyle(actions).backgroundColor, backdrop: getComputedStyle(el, '::backdrop').backgroundColor };
        });
        referenceGeometry.push({ width, theme, ...visual });
        await capture(page, `drawer-${suffix}`);
        await message.fill('Blue');
        await submit.click();
        await validation.getByRole('link').waitFor();
        await capture(page, `validation-${suffix}`);
        await page.keyboard.press('Escape');
        await drawer.waitFor({ state: 'hidden' });
        await opener.click();
        await message.fill(filled);
        await capture(page, `filled-${suffix}`);

        if (width === 1440 && theme === 'light') {
          let release;
          const barrier = new Promise(resolve => { release = resolve; });
          let posts = 0;
          await page.route(claimRoute, async route => {
            assert.equal(route.request().method(), 'POST');
            assert.equal(route.request().headers()['x-traceback-claim'], 'drawer');
            const body = new URLSearchParams(route.request().postData());
            assert(body.get('__RequestVerificationToken'));
            assert.equal(body.get('Message'), filled);
            posts++;
            await barrier;
            await route.continue();
          });
          assert.deepEqual(await submit.evaluate(el => {
            el.form.requestSubmit();
            el.form.requestSubmit();
            return [el.getAttribute('aria-disabled'), el.querySelector('.claim-form__pending-label').checkVisibility(), el.form.elements.Message.readOnly];
          }), ['true', true, true]);
          await capture(page, 'pending-1440-light');
          release();
          await page.waitForURL('**/Dashboard');
          await page.getByText(/Your claim was submitted/).waitFor();
          await capture(page, 'dashboard-success-1440-light');
          await page.reload();
          assert.equal(await page.getByText(/Your claim was submitted/).count(), 0);
          await page.unroute(claimRoute);
          assert.equal(posts, 1);
          await page.goto(`${base}/Items/Details/${photoId}`);
          await opener.click();
          await message.fill('Private text retained on duplicate.');
          const duplicateResponse = page.waitForResponse(res => res.url().includes('/Claims/Create?') && res.status() === 409);
          await submit.click();
          await duplicateResponse;
          await validation.getByText(/You may already have a claim/).waitFor();
          assert.equal(await message.inputValue(), 'Private text retained on duplicate.');
          await capture(page, 'duplicate-1440-light');
        } else {
          // Hold the real duplicate POST to capture its pending presentation.
          let release;
          const barrier = new Promise(resolve => { release = resolve; });
          await page.route(claimRoute, async route => { await barrier; await route.continue(); }, { times: 1 });
          await submit.evaluate(el => el.form.requestSubmit());
          await drawer.locator('.claim-form__pending-label').waitFor();
          await capture(page, `pending-${suffix}`);
          release();
          await validation.getByText(/You may already have a claim/).waitFor();
          assert.equal(await message.inputValue(), filled);
          await capture(page, `duplicate-${suffix}`);
        }
        if (width === 390 && theme === 'dark') {
          await message.fill('Keep this private draft on failure.');
          await page.route(claimRoute, route => route.abort('failed'), { times: 1 });
          await submit.click();
          await validation.getByText(/could not confirm/).waitFor();
          assert.equal(await message.inputValue(), 'Keep this private draft on failure.');
          assert(await submit.isEnabled());
          await capture(page, 'failed-390-dark');
          await page.route(claimRoute, route => route.fulfill({ status: 404, json: { error: 'This report is no longer available for claims.' } }), { times: 1 });
          await submit.click();
          await validation.getByText(/no longer available/).waitFor();
          assert.equal(await message.inputValue(), 'Keep this private draft on failure.');
          assert(await submit.isDisabled());
          await drawer.getByRole('button', { name: 'Cancel' }).click();
          await drawer.waitFor({ state: 'hidden' });
          assert(await focused(opener));
        }
        assert.deepEqual(errors, []);
        await context.close();
        console.log(`Claim drawer: ${width} ${theme} passed`);
      }
    }
    const noScript = await browser.newContext({ javaScriptEnabled: false, viewport: { width: 390, height: 844 }, colorScheme: 'dark' });
    await noScript.addCookies([{ name, value, url: base }]);
    const page = await noScript.newPage();
    await page.goto(details);
    await page.getByRole('link', { name: 'Submit a claim' }).click();
    await page.waitForURL('**/Claims/Create?*');
    assert.equal(await page.getByRole('heading', { name: 'Submit a claim' }).count(), 1);
    assert.equal(await page.getByRole('dialog').count(), 0);
    const message = page.getByRole('textbox', { name: /ownership details/i });
    const submit = page.getByRole('button', { name: 'Submit claim' });
    await message.fill('  Blue      ');
    await submit.click();
    await page.locator('[data-tb-validation-summary]').getByRole('link').waitFor();
    assert.equal(await message.inputValue(), '  Blue      ');
    await capture(page, 'standalone-no-script-390-dark');
    await message.fill('The other bottle has a faded label.');
    await submit.click();
    await page.waitForURL('**/Dashboard');
    await page.getByText(/Your claim was submitted/).waitFor();
    await noScript.close();
    console.log('Claim fallback: native link, invalid POST, preserved draft, successful POST and Dashboard flash, scripts disabled');

    const standalone = await browser.newContext({ viewport: { width: 1440, height: 1040 }, colorScheme: 'light' });
    await standalone.addCookies([{ name, value, url: base }]);
    const direct = await standalone.newPage();
    await direct.goto(`${base}/Claims/Create?itemId=${id}`);
    const directMessage = direct.getByRole('textbox', { name: /ownership details/i });
    await directMessage.fill('Keep the direct route duplicate draft.');
    let directPosts = 0;
    await direct.route(claimRoute, route => {
      assert.equal(route.request().method(), 'POST');
      assert.equal(route.request().headers()['x-traceback-claim'], undefined);
      directPosts++;
      return route.continue();
    });
    const duplicate = direct.waitForResponse(res => res.url().includes('/Claims/Create?') && res.status() === 409);
    assert.deepEqual(await direct.getByRole('button', { name: 'Submit claim' }).evaluate(el => {
      el.form.requestSubmit(el);
      el.form.requestSubmit(el);
      return [el.getAttribute('aria-disabled'), el.querySelector('.claim-form__pending-label').checkVisibility()];
    }), ['true', true]);
    await duplicate;
    const directSummary = direct.locator('[data-tb-validation-summary]');
    await directSummary.getByText(/You may already have a claim/).waitFor();
    await direct.waitForFunction(() => document.querySelector('[data-tb-validation-summary]') === document.activeElement);
    assert.equal(await directMessage.inputValue(), 'Keep the direct route duplicate draft.');
    assert.equal(directPosts, 1);
    await capture(direct, 'standalone-duplicate-1440-light');
    await direct.unroute(claimRoute);
    await direct.getByRole('link', { name: 'Cancel' }).click();
    await direct.waitForURL(details);
    await standalone.close();
    console.log('Claim direct route: native pending, one POST, duplicate draft/focus and Cancel passed');
  } finally { await browser.close(); }
  console.log(`PDF 08 geometry ${JSON.stringify(referenceGeometry)}`);
  for (const { width, theme, item, privacy, field, footer, footerSurface, backdrop } of referenceGeometry) {
    assert.equal(item[3], 96, `PDF 08 ${width} ${theme} item card height`);
    assert.equal(privacy[3], 103, `PDF 08 ${width} ${theme} privacy card height`);
    assert.equal(field[3], 156, `PDF 08 ${width} ${theme} message box height`);
    assert.equal(footer[3], 92, `PDF 08 ${width} ${theme} footer height`);
    assert.notEqual(footerSurface, 'rgba(0, 0, 0, 0)');
    assert.equal(backdrop, 'rgba(13, 16, 34, 0.58)');
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
