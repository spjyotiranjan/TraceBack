// Driven by ReportAuthorizationTests.Access_denied_in_browser (TRACEBACK_BROWSER=1).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require('playwright');
const { base, member } = JSON.parse(fs.readFileSync(0, 'utf8'));

async function tabTo(page, locator) {
  for (let steps = 0; steps < 20; steps++) {
    await page.keyboard.press('Tab');
    if (await locator.evaluate(el => el === document.activeElement)) return;
  }
  assert.fail(`Not reachable with Tab: ${await locator.textContent()}`);
}

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROME_PATH || undefined });
  try {
    for (const colorScheme of ['light', 'dark']) {
      for (const [width, height] of [[1440, 1040], [390, 844], [390, 440]]) {
        const context = await browser.newContext({ viewport: { width, height }, colorScheme });
        try {
          await context.addCookies([{ ...member, url: base }]);
          const page = await context.newPage();
          page.setDefaultTimeout(5000);
          const errors = [];
          page.on('pageerror', error => errors.push(error.message));
          page.on('response', response => {
            if (response.url().startsWith(`${base}/`) && response.status() >= 400) {
              errors.push(`${response.status()} ${response.url()}`);
            }
          });
          const home = page.getByRole('main').getByRole('link', { name: 'Go home', exact: true });
          const privacy = page.getByRole('contentinfo').getByRole('link', { name: 'Privacy' });
          for (const entry of ['/Account/Denied', '/Admin']) {
            const response = await page.goto(`${base}${entry}`);
            assert.equal(response.status(), 200);
            const url = new URL(response.url());
            assert.equal(url.pathname, '/Account/Denied');
            if (entry === '/Admin') {
              assert.equal(url.searchParams.get('ReturnUrl'), '/Admin');
              const restrictedRequest = response.request().redirectedFrom();
              assert.equal(new URL(restrictedRequest.url()).pathname, '/Admin');
              assert.equal((await restrictedRequest.response()).status(), 302);
            }
            assert.equal(await page.getByRole('heading', { level: 1 }).textContent(), 'Access denied');
            const routeArt = page.locator('[data-route-art="denied"][aria-hidden="true"]');
            const asset = `denied-${width === 390 ? 'phone' : 'desktop'}-${colorScheme}-a.svg`;
            assert((await routeArt.evaluate(el => getComputedStyle(el, '::before').backgroundImage)).includes(asset));
            assert.equal(await page.locator('.tb-recovery-content__art--access').getAttribute('aria-hidden'), 'true');
            assert.equal(await home.getAttribute('href'), '/');
            const layout = await page.evaluate(() => {
              const main = document.querySelector('main');
              const footer = document.querySelector('footer');
              const action = main.querySelector('.tb-recovery-content__actions a');
              return {
                noOverflow: document.documentElement.scrollWidth === document.documentElement.clientWidth,
                inFlow: [main, footer, action].every(el => ['static', 'relative'].includes(getComputedStyle(el).position)),
                actionBeforeFooter: action.getBoundingClientRect().bottom <= footer.getBoundingClientRect().top
              };
            });
            assert.deepEqual(layout, { noOverflow: true, inFlow: true, actionBeforeFooter: true });
            const dock = page.locator('.tb-member-dock');
            assert.equal(await dock.isVisible(), width === 390);
            if (entry === '/Admin' && height > 440 && process.env.DENIED_SCREENSHOTS) {
              await page.screenshot({ path: path.join(process.env.DENIED_SCREENSHOTS, `denied-${colorScheme}-${width}.png`) });
            }

            const skip = page.getByRole('link', { name: 'Skip to content' });
            await page.keyboard.press('Tab');
            assert(await skip.evaluate(el => el === document.activeElement));
            await page.keyboard.press('Enter');
            assert(await page.getByRole('main').evaluate(el => el === document.activeElement));
            await tabTo(page, home);
            assert(await home.evaluate(el => el.matches(':focus-visible')));
            const actionDockBox = await dock.boundingBox();
            const actionBox = await home.boundingBox();
            assert(actionBox.y >= 0 && actionBox.y + actionBox.height <= (actionDockBox?.y ?? height),
              `Keyboard-focused Go home is obscured (${entry}, ${width}x${height}, ${colorScheme}): ${JSON.stringify({ actionBox, actionDockBox })}`);
            await tabTo(page, privacy);
            const footerBox = await privacy.boundingBox();
            const dockBox = await dock.boundingBox();
            assert(footerBox.y >= 0 && footerBox.y + footerBox.height <= (dockBox?.y ?? height),
              'Keyboard-focused Privacy link is obscured');
            await page.keyboard.press('Shift+Tab');
            assert(await home.evaluate(el => el === document.activeElement));
            await Promise.all([page.waitForURL(`${base}/`), page.keyboard.press('Enter')]);
          }
          assert.deepEqual(errors, []);
          console.log(`Access denied: direct and member /Admin, ${width}x${height}, ${colorScheme}`);
        } finally {
          await context.close();
        }
      }
    }
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
