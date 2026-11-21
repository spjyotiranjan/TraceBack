(() => {
  const disclosures = [...document.querySelectorAll('[data-shell-disclosure]')];
  const mobile = document.querySelector('.tb-mobile-menu');

  for (const disclosure of disclosures) {
    const trigger = disclosure.querySelector('summary');
    trigger.addEventListener('click', () => {
      // Normalize focus for pointer activation and keep native disclosure semantics.
      trigger.focus({ preventScroll: true });
      if (!disclosure.open) {
        disclosures.filter(other => other !== disclosure).forEach(other => { other.open = false; });
      }
    });
  }

  document.addEventListener('click', event => {
    for (const disclosure of disclosures) {
      if (!disclosure.open || disclosure.contains(event.target)) continue;
      const active = document.activeElement;
      disclosure.open = false;
      // Do not steal focus from an outside control that the user just activated.
      if (active === document.body || (active.tabIndex < 0 && !active.isContentEditable) || disclosure.contains(active)) {
        disclosure.querySelector('summary').focus({ preventScroll: true });
      }
    }
  });

  document.addEventListener('focusin', event => {
    // A plain content click may focus a tabindex=-1 ancestor (e.g. main).
    // Let the click handler restore focus in that case, but not for real controls or drawers.
    if (event.target.tabIndex < 0 && !event.target.isContentEditable &&
        !event.target.closest('dialog, [role="dialog"]')) return;
    disclosures.filter(disclosure => !disclosure.contains(event.target))
      .forEach(disclosure => { disclosure.open = false; });
  });

  document.addEventListener('keydown', event => {
    if (event.key !== 'Escape' || event.defaultPrevented) return;
    const open = disclosures.find(disclosure => disclosure.open && disclosure.contains(event.target));
    if (!open) return;
    event.preventDefault();
    open.open = false;
    open.querySelector('summary').focus({ preventScroll: true });
  });

  // A previously open phone menu must not reappear after a resize back to mobile.
  if (mobile) matchMedia('(min-width: 56rem)').addEventListener('change', event => {
    if (event.matches) mobile.open = false;
  });
})();