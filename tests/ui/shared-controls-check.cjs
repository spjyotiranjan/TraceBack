// Run via SharedControlsBrowserTests against its loopback-only MVC fixture server.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const base = process.env.TRACEBACK_URL;
const failure = '/Home/Privacy?fixtureFault=fixture-query-marker';
const privacy = '/Home/Privacy';
const login = '/Account/Login?returnUrl=%2FItems';
const luminance = rgb => rgb.match(/[\d.]+/g).slice(0, 3).map((c, i) => {
  c = Number(c) / 255;
  return (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4) * [0.2126, 0.7152, 0.0722][i];
}).reduce((a, b) => a + b);
const contrast = (a, b) => {
  const [low, high] = [luminance(a), luminance(b)].sort((x, y) => x - y);
  return (high + 0.05) / (low + 0.05);
};
const style = node => node.evaluate(e => {
  const s = getComputedStyle(e);
  const r = e.getBoundingClientRect();
  return { ink: s.color, fill: s.backgroundColor, border: s.borderTopColor, outline: s.outlineColor,
    outlineWidth: s.outlineWidth, outlineStyle: s.outlineStyle, width: r.width, height: r.height };
});
const fits = page => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth);
async function fixture(page, route) {
  const response = await page.request.get(`${base}/ui-controls-test/${route}`);
  assert.equal(response.status(), 200);
  return response.text();
}
async function recoveryAtNativeZoom() {
  const output = path.resolve(__dirname, '../../src/TraceBack/bin');
  const profile = fs.mkdtempSync(path.join(output, 'recovery-focus-zoom-'));
  try {
    const executablePath = process.env.CHROME_PATH || undefined;
    const context = await chromium.launchPersistentContext(profile, {
      executablePath, channel: executablePath ? undefined : 'chromium',
      viewport: { width: 640, height: 800 }, deviceScaleFactor: 1, reducedMotion: 'reduce'
    });
    try {
      const page = context.pages()[0];
      await page.goto('chrome://settings/');
      await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.setDefaultZoom(2, resolve)));
      assert.equal(await page.evaluate(() => new Promise(resolve => chrome.settingsPrivate.getDefaultZoom(resolve))), 2);
      assert.equal((await context.request.post(base + '/ui-controls-test/member-cookie')).status(), 204);

      async function tabTo(link) {
        await page.keyboard.press('Tab');
        assert(await link.evaluate(el => {
          const rect = el.getBoundingClientRect();
          const top = document.querySelector('.site-header').getBoundingClientRect().bottom;
          const bottom = document.querySelector('.tb-member-dock').getBoundingClientRect().top;
          return el === document.activeElement && el.matches(':focus-visible') && rect.top >= top && rect.bottom <= bottom;
        }), `Focused link is obscured or unreachable: ${await link.textContent()}`);
      }

      for (const colorScheme of ['light', 'dark']) {
        await page.emulateMedia({ colorScheme });
        for (const [url, status, label] of [['/Admin', 200, 'Go home'], [failure, 500, 'Try again']]) {
          assert.equal((await page.goto(base + url)).status(), status);
          assert.deepEqual(await page.evaluate(() => [innerWidth, innerHeight, devicePixelRatio]), [320, 400, 2]);
          assert(await fits(page));
          const skip = page.getByRole('link', { name: 'Skip to content' });
          await page.keyboard.press('Tab');
          assert(await skip.evaluate(el => el === document.activeElement));
          await page.keyboard.press('Enter');
          await tabTo(page.getByRole('link', { name: label, exact: true }));
          if (label === 'Try again') await tabTo(page.getByRole('link', { name: 'Go home', exact: true }));
          await tabTo(page.getByRole('contentinfo').getByRole('link', { name: 'Privacy', exact: true }));
          const privacyResponse = page.waitForResponse(r => r.request().isNavigationRequest() && r.url() === base + privacy);
          await page.keyboard.press('Enter');
          assert.equal((await privacyResponse).status(), 200);
          await page.waitForURL(base + privacy, { waitUntil: 'load' });
        }
        console.log(`Denied and Error: native 200% zoom, keyboard actions and Privacy footer in ${colorScheme}`);
      }
    } finally { await context.close(); }
  } finally { fs.rmSync(profile, { recursive: true, force: true, maxRetries: 5, retryDelay: 100 }); }
}

async function variants(page) {
  const buttons = await Promise.all(['Primary', 'Secondary', 'Quiet', 'Destructive',
    'Secondary?iconOnly=true', 'Primary?disabled=true'].map(tone => fixture(page, `button/${tone}`)));
  const empty = await fixture(page, 'empty');
  await page.locator('main').evaluate((main, html) => main.insertAdjacentHTML('beforeend', html), `
    <section id="variants" class="form-card">
      <div class="d-flex gap-2 flex-wrap">${buttons.join('')}</div>
      <fieldset class="tb-field mt-3" aria-describedby="choice-hint">
        <legend>Report type</legend><p id="choice-hint" class="tb-field__hint">Choose one option.</p>
        <label class="tb-field__choice"><input type="radio" name="fixture-choice" checked>Lost</label>
        <label class="tb-field__choice"><input type="radio" name="fixture-choice">Found</label>
      </fieldset>
      <div class="tb-field mt-2"><label for="fixture-locked">Locked field</label>
        <input id="fixture-locked" class="form-control" value="Cannot change" readonly aria-describedby="locked-help">
        <p id="locked-help" class="tb-field__hint">This value cannot be changed.</p></div>
      <div class="tb-field mt-2"><label for="fixture-disabled">Disabled field</label><input id="fixture-disabled" class="form-control" disabled value="Unavailable"></div>
      <div class="tb-field mt-2"><label for="fixture-date">Date</label><input id="fixture-date" class="form-control" type="date"></div>
      <div class="tb-field mt-2"><label for="fixture-select">Select</label><select id="fixture-select" class="form-select"><option>First option</option></select></div>
      <div class="tb-field mt-2"><label for="fixture-details">Details</label><textarea id="fixture-details" class="form-control" aria-describedby="fixture-hint">Text entered</textarea>
        <p id="fixture-hint" class="tb-field__hint">Explain what happened.</p></div>
    </section>${empty}`);
  const panel = page.locator('#variants');
  const canvas = (await style(panel)).fill;
  for (const button of await panel.getByRole('button', { name: 'Fixture action' }).all()) {
    const colors = await style(button);
    assert(colors.height >= 44 && colors.width >= 44);
    assert(contrast(colors.ink, colors.fill.endsWith(', 0)') ? canvas : colors.fill) >= 4.5);
    if (!(await button.isDisabled())) {
      await button.hover();
      const hover = await style(button);
      assert(contrast(hover.ink, hover.fill.endsWith(', 0)') ? canvas : hover.fill) >= 4.5,
        `${await button.getAttribute('class')}: hover ${hover.ink} on ${hover.fill}`);
    }
  }
  const icon = panel.locator('.tb-button--icon');
  assert.equal(await icon.getAttribute('title'), 'Fixture action');
  await icon.focus();
  await page.keyboard.press('Shift+Tab');
  const focused = await style(page.locator(':focus'));
  assert.equal(focused.outlineWidth, '3px');
  assert(contrast(focused.outline, canvas) >= 3);
  const group = panel.getByRole('group', { name: 'Report type' });
  assert.equal(await group.getAttribute('aria-describedby'), 'choice-hint');
  assert((await style(group.locator('label').first())).height >= 44);
  await group.getByRole('radio', { name: 'Lost', exact: true }).focus();
  await page.keyboard.press('ArrowRight');
  assert(await group.getByRole('radio', { name: 'Found' }).isChecked());
  const locked = panel.getByLabel('Locked field');
  assert.equal(await locked.getAttribute('readonly'), '');
  assert.equal(await locked.getAttribute('aria-describedby'), 'locked-help');
  assert.equal(await locked.evaluate(e => getComputedStyle(e).borderStyle), 'dashed');
  await locked.press('a');
  assert.equal(await locked.inputValue(), 'Cannot change');
  assert(await panel.getByLabel('Disabled field').isDisabled());
  for (const label of ['Locked field', 'Disabled field', 'Date', 'Select', 'Details']) {
    const control = await style(panel.getByLabel(label, { exact: true }));
    assert(control.height >= 44);
    assert(contrast(control.ink, control.fill) >= 4.5);
    assert(contrast(control.border, control.fill) >= 3);
  }
  assert(await page.getByRole('heading', { name: 'No fixture reports', level: 2 }).isVisible());
  assert.equal(await page.getByRole('link', { name: 'Clear filters' }).getAttribute('href'), login);
  assert.equal(await page.locator('.tb-recovery-content__art').getAttribute('aria-hidden'), 'true');
  assert(await fits(page));
}
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const width of [1440, 390, 320]) {
      for (const colorScheme of ['light', 'dark']) {
        const config = { viewport: { width, height: width === 1440 ? 1040 : 844 }, colorScheme, reducedMotion: 'reduce' };
        const page = await browser.newPage(config);
        page.setDefaultTimeout(6000);

        const pageErrors = [];
        page.on('pageerror', error => pageErrors.push(error.message));
        assert.equal((await page.goto(base + login)).status(), 200);
        const email = page.getByLabel('Email address');
        const password = page.getByLabel('Password', { exact: true });
        const submit = page.getByRole('button', { name: 'Sign in', exact: true });
        const summary = page.locator('[data-tb-validation-summary]');
        const status = page.locator('[data-tb-pending-status]');
        let posts = 0;
        await page.route('**/Account/Login**', async route => {
          if (route.request().method() !== 'POST') return route.continue();
          posts++;
          await route.fulfill({ status: 303, headers: { Location: login } });
        });
        await submit.focus();
        await page.keyboard.press('Enter');
        await page.waitForFunction(() => document.activeElement.matches('[data-tb-validation-summary]'));
        assert.equal(await summary.getAttribute('role'), 'alert');
        assert.equal(await summary.getByRole('link').count(), 2);
        const emailLink = summary.locator('a[href="#Email"]');
        await emailLink.focus();
        await page.keyboard.press('Enter');
        assert.equal(await email.evaluate(e => e === document.activeElement), true);
        assert.equal(await email.getAttribute('aria-invalid'), 'true');
        assert.match(await email.getAttribute('aria-describedby'), /\bEmail-validation\b/);
        assert.equal(await page.locator('#Email-validation').count(), 1);
        const invalid = await style(email);
        assert(contrast(invalid.border, invalid.fill) >= 3);
        const error = await style(summary);
        assert(contrast(error.ink, error.fill) >= 4.5);
        assert.equal(posts, 0);
        assert.equal(await status.textContent(), '');
        if (process.env.CONTROL_SCREENSHOTS && width !== 320) {
          await page.screenshot({ path: `${process.env.CONTROL_SCREENSHOTS}/validation-${colorScheme}-${width}.png`, fullPage: true });
        }
        await email.pressSequentially('fixture@example.test');
        await password.pressSequentially('browser-fixture');
        await password.press('Tab');
        await summary.waitFor({ state: 'hidden' });
        assert.notEqual(await email.getAttribute('aria-invalid'), 'true');
        // Sample the old document synchronously before the native navigation commits.
        const [pending, sent] = await Promise.all([
          submit.evaluate(button => {
            button.name = 'intent';
            button.value = 'sign-in';
            const original = button.getBoundingClientRect();
            const status = button.form.querySelector('[data-tb-pending-status]');
            button.click();
            const box = button.getBoundingClientRect();
            const repeatCanceled = !button.form.dispatchEvent(new SubmitEvent('submit', { bubbles: true, cancelable: true, submitter: button }));
            button.click();
            button.form.requestSubmit(button);
            return { text: status.textContent, role: status.getAttribute('role'), shown: status.offsetHeight > 0,
              unavailable: button.getAttribute('aria-disabled'), disabled: button.disabled,
              sizeUnchanged: box.width === original.width && box.height === original.height, repeatCanceled };
          }),
          page.waitForRequest(r => r.isNavigationRequest() && r.method() === 'POST').then(r => new URLSearchParams(r.postData())),
          page.waitForNavigation({ waitUntil: 'domcontentloaded' })
        ]);
        assert.deepEqual(pending, { text: 'Signing in…', role: 'status', shown: true, unavailable: 'true', disabled: false,
          sizeUnchanged: true, repeatCanceled: true });
        assert.equal(posts, 1);
        assert.equal(sent.get('intent'), 'sign-in');
        assert.equal(sent.get('Email'), 'fixture@example.test');
        assert(sent.get('__RequestVerificationToken'));
        assert.equal(await status.textContent(), '');
        assert.equal(await submit.getAttribute('aria-disabled'), null);
        await variants(page);
        assert.equal((await page.goto(base + failure)).status(), 500);
        const retry = page.getByRole('link', { name: 'Try again', exact: true });
        assert.equal(await retry.getAttribute('href'), privacy);
        assert(await fits(page));
        await retry.focus();
        const recovery = await style(retry);
        assert.equal(recovery.outlineStyle, 'solid');
        assert(recovery.height >= 44 && contrast(recovery.ink, recovery.fill) >= 4.5);
        if (process.env.CONTROL_SCREENSHOTS && width !== 320) {
          await page.screenshot({ path: `${process.env.CONTROL_SCREENSHOTS}/recovery-${colorScheme}-${width}.png`, fullPage: true });
        }
        const request = page.waitForResponse(r => r.request().isNavigationRequest() && r.url() === base + privacy);
        await page.keyboard.press('Enter');
        const retried = await request;
        assert.equal(retried.request().method(), 'GET');
        assert.equal(await retried.request().headerValue('referer'), null);
        assert.equal(retried.status(), 200);
        await page.waitForURL(base + privacy);

        for (const role of ['guest', 'member']) {
          if (role === 'member') assert.equal((await page.request.post(base + '/ui-controls-test/member-cookie')).status(), 204);
          const panel = page.locator('.tb-recovery-content--page');
          const response = await page.goto(base + (role === 'member' ? failure : '/ui-controls-test/reference/none'));
          assert.equal(response.status(), role === 'member' ? 500 : 200);
          assert(await panel.getByRole('heading', { level: 1, name: 'Something went wrong' }).isVisible());
          assert.equal(await page.locator('[data-route-art="error"]').getAttribute('aria-hidden'), 'true');
          assert.equal(await page.locator('[data-route-art="error"]').evaluate(e => getComputedStyle(e, '::before').animationName), 'none');
          assert.equal(await panel.locator('code').count(), role === 'member' ? 1 : 0);
          assert.equal(await page.locator('.tb-member-dock').isVisible(), role === 'member' && width < 896);
          assert.equal(await page.locator('.tb-desktop-nav').isVisible(), role === 'member' && width >= 896);
          const { width: panelWidth } = await style(panel);
          if (width === 1440) assert(Math.abs(panelWidth - 590) <= 2, `desktop panel width: ${panelWidth}`);
          if (width === 390) assert(Math.abs(panelWidth - 350) <= 2, `mobile panel width: ${panelWidth}`);
          assert(await fits(page));
          if (process.env.ERROR_SCREENSHOTS && width !== 320) {
            await page.screenshot({ path: `${process.env.ERROR_SCREENSHOTS}/error-${role}-${colorScheme}-${width}.png` });
          }
        }

        await page.goto(base + '/ui-controls-test/reference/long');
        const code = page.locator('.tb-recovery-content__reference code');
        assert.equal(await code.textContent(), 'R'.repeat(512) + '<end>');
        assert(await code.evaluate(e => getComputedStyle(e).userSelect !== 'none'));
        const referenceOverflow = await code.evaluate(e => ({
          viewport: innerWidth, document: document.documentElement.scrollWidth,
          code: e.getBoundingClientRect().width, container: e.parentElement.clientWidth,
          wrap: getComputedStyle(e).overflowWrap, whiteSpace: getComputedStyle(e).whiteSpace
        }));
        assert(referenceOverflow.document <= width && referenceOverflow.code <= referenceOverflow.container + 1,
          `Long reference overflow: ${JSON.stringify(referenceOverflow)}`);
        if (width < 896) {
          const footer = page.getByRole('link', { name: 'Privacy', exact: true });
          await footer.scrollIntoViewIfNeeded();
          assert((await footer.boundingBox()).y + (await footer.boundingBox()).height <= (await page.locator('.tb-member-dock').boundingBox()).y);
        }
        assert.deepEqual(pageErrors, []);
        await page.close();
        console.log(`Shared controls: validation, pending, field/button variants and recovery passed in ${colorScheme} at ${width}px`);
      }
    }
    const noJs = await browser.newPage({ javaScriptEnabled: false, viewport: { width: 390, height: 850 } });
    assert.equal((await noJs.goto(base + failure)).status(), 500);
    const noJsRetry = noJs.getByRole('link', { name: 'Try again' });
    const retryResponse = noJs.waitForResponse(r => r.url() === base + privacy);
    await noJsRetry.click();
    const response = await retryResponse;
    assert.equal(response.request().method(), 'GET');
    assert.equal(response.status(), 200);
    assert.equal((await noJs.goto(base + '/ui-controls-test/notices')).status(), 200);
    assert.equal((await noJs.getByRole('status').filter({ hasText: 'Test saved' }).innerText()).trim(), 'Test saved <sample>');
    assert.equal((await noJs.getByRole('alert').filter({ hasText: 'Test failed' }).innerText()).trim(), 'Test failed');
    await noJs.reload();
    assert.equal(await noJs.getByText('Test saved <sample>', { exact: true }).count(), 0);
    console.log('Shared controls: no-JavaScript GET retry and one-time notices passed');

    const write = await browser.newPage({ viewport: { width: 390, height: 844 }, colorScheme: 'dark' });
    const navigations = [];
    write.on('request', r => { if (r.isNavigationRequest()) navigations.push([r.method(), new URL(r.url()).pathname]); });
    await write.goto(base + '/ui-controls-test/write-form');
    const failedWrite = write.waitForResponse(r => r.request().method() === 'POST');
    await write.getByRole('button', { name: 'Send fixture' }).click();
    assert.equal((await failedWrite).status(), 500);
    const actions = write.locator('.tb-recovery-content__actions');
    assert.equal(await actions.locator(':is(a, button, form)').count(), 1);
    assert.doesNotMatch(await write.content(), /fixture-query-marker|fixture-body-marker|Test recovery rendering/);
    const goHome = actions.getByRole('link', { name: 'Go home' });
    assert.equal(await goHome.getAttribute('href'), '/');
    const homeResponse = write.waitForResponse(r => r.url() === `${base}/`);
    await goHome.click();
    const recovered = await homeResponse;
    assert.equal(recovered.status(), 200);
    assert.equal(await recovered.request().headerValue('referer'), null);
    assert.deepEqual(navigations, [['GET', '/ui-controls-test/write-form'], ['POST', '/ui-controls-test/failure'], ['GET', '/']]);
    console.log('Error recovery: native failed POST stayed 500, then only a GET Home; no data leakage or replay');
  } finally { await browser.close(); }
  await recoveryAtNativeZoom();
})().catch(error => { console.error(error); process.exitCode = 1; });
