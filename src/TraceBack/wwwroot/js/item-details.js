const description = document.querySelector('[data-detail-description]');
if (description) {
  const text = description.querySelector('p');
  const toggle = description.querySelector('button');
  let expanded = false;

  const update = () => {
    const lineHeight = parseFloat(getComputedStyle(text).lineHeight);
    const overflows = text.scrollHeight > Math.ceil(8 * lineHeight);
    // Do not hide the focused control during a resize. Let focus leave naturally.
    toggle.hidden = !overflows && document.activeElement !== toggle;
    toggle.setAttribute('aria-expanded', String(expanded));
    toggle.textContent = expanded ? 'Show less' : 'Read more';
    description.classList.toggle('is-collapsed', overflows && !expanded);
  };

  toggle.addEventListener('click', () => {
    expanded = !expanded;
    update();
  });
  toggle.addEventListener('blur', update);
  new ResizeObserver(update).observe(description);
  document.fonts.ready.then(update);
  update();
}

const mediaColumn = document.querySelector('.detail-report__media-column');
if (mediaColumn) {
  const update = () => {
    const top = parseFloat(getComputedStyle(mediaColumn).top);
    const bottom = parseFloat(getComputedStyle(document.documentElement).scrollPaddingBottom);
    // Let the whole rail remain reachable between the shell's actual clearances.
    mediaColumn.classList.toggle('is-sticky', mediaColumn.offsetHeight + top + bottom <= window.innerHeight);
  };

  new ResizeObserver(update).observe(mediaColumn);
  window.addEventListener('resize', update);
  update();
}
