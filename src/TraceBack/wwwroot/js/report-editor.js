(() => {
  const tracker = document.querySelector('[data-report-tracker]');
  if (!tracker) return;
  const editor = tracker.closest('.tb-report-editor');
  const links = Array.from(tracker.querySelectorAll('a'));
  const headings = links.map(link => document.getElementById(link.hash.slice(1)));
  if (!headings.length || headings.some(heading => !heading)) return;

  const caption = tracker.querySelector('[data-report-current]');
  const header = document.querySelector('.site-header');
  const dock = document.querySelector('.tb-member-dock');
  const mobile = matchMedia('(width < 56rem)');
  let active = -1;
  let tick = 0;
  let jump = null;
  const activate = index => {
    if (index === active) return;
    links[active]?.removeAttribute('aria-current');
    links[index].setAttribute('aria-current', 'step');
    caption.textContent = links[index].textContent.trim();
    active = index;
  };
  const clearance = () => {
    const top = parseFloat(getComputedStyle(tracker).top) || header?.getBoundingClientRect().bottom || 0;
    return top + (mobile.matches ? tracker.offsetHeight : 0);
  };
  const update = () => {
    tick = 0;
    if (jump && Math.abs(scrollY - jump.top) <= 2) {
      activate(jump.index);
      return;
    }
    jump = null;
    const readingLine = clearance() + 12;
    let index = 0;
    for (let i = 1; i < headings.length; i++) {
      if (headings[i].getBoundingClientRect().top <= readingLine) index = i;
    }
    const page = document.scrollingElement;
    const atEnd = page.scrollTop > 0 && page.scrollHeight - page.clientHeight - page.scrollTop <= 2;
    activate(atEnd ? headings.length - 1 : index);
  };
  const scheduleUpdate = () => { if (!tick) tick = requestAnimationFrame(update); };
  // An anchor near the end can stop short of the reading line. Keep its label
  // until the reader actually moves away from the anchor's scroll position.
  const rememberJump = index => {
    jump = { index, top: scrollY };
    activate(index);
  };
  const followHash = () => {
    const index = links.findIndex(link => link.hash === location.hash);
    if (index === -1) { jump = null; scheduleUpdate(); return; }
    headings[index].scrollIntoView({ block: 'start', behavior: 'instant' });
    rememberJump(index);
  };

  links.forEach((link, i) => {
    const heading = headings[i];
    if (heading.parentElement.querySelector('.field-validation-error')) {
      link.dataset.error = '';
      link.setAttribute('aria-label', `${link.textContent.trim()}, has errors`);
    }
    link.addEventListener('click', event => {
      if (event.defaultPrevented || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
      heading.focus({ preventScroll: true });
      activate(i);
      // Leave fragment navigation and browser history to the real link.
      requestAnimationFrame(() => { if (location.hash === link.hash) rememberJump(i); });
    });
  });

  editor.addEventListener('focusin', event => {
    const control = event.target;
    if (tracker.contains(control) || headings.includes(control)) return;
    requestAnimationFrame(() => {
      if (document.activeElement !== control) return;
      const rect = control.getBoundingClientRect();
      const dockBounds = dock?.getBoundingClientRect();
      const bottom = dockBounds?.height ? dockBounds.top : innerHeight;
      if (rect.top < clearance() || rect.bottom > bottom) {
        control.scrollIntoView({ block: 'center', behavior: 'instant' });
      }
    });
  });

  tracker.classList.add('is-tracking');
  followHash();
  addEventListener('scroll', scheduleUpdate, { passive: true });
  addEventListener('resize', () => { jump = null; scheduleUpdate(); });
  addEventListener('hashchange', followHash);
  addEventListener('pageshow', scheduleUpdate);
})();
