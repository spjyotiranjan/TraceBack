(() => {
  const header = document.querySelector('.site-header');
  const dock = document.querySelector('.tb-member-dock');

  document.addEventListener('focusin', ({ target }) => {
    if (!target.matches('.tb-recovery-content__actions a:focus-visible, .site-footer a:focus-visible')) return;

    // Native Tab scrolling can stop inside the viewport but behind the fixed shell.
    const { top, bottom } = target.getBoundingClientRect();
    const visibleTop = header.getBoundingClientRect().bottom;
    const visibleBottom = dock?.getClientRects().length
      ? dock.getBoundingClientRect().top
      : document.documentElement.clientHeight;
    if (top < visibleTop || bottom > visibleBottom) {
      target.scrollIntoView({ block: 'center', inline: 'nearest', behavior: 'instant' });
    }
  });
})();