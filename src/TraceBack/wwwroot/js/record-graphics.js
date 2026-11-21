(() => {
  const records = [...document.querySelectorAll('[data-record-id]')];
  const highlight = (record, active) => {
    records.filter(other => other.dataset.recordId === record.dataset.recordId)
      .forEach(other => other.classList.toggle('is-linked', active));
  };
  records.forEach(record => {
    record.addEventListener('pointerenter', () => highlight(record, true));
    record.addEventListener('pointerleave', () => highlight(record, record.contains(document.activeElement)));
    record.addEventListener('focusin', () => highlight(record, true));
    record.addEventListener('focusout', event => {
      if (!record.contains(event.relatedTarget)) highlight(record, false);
    });
  });

  const draft = document.querySelector('[data-draft-canvas]');
  const form = document.querySelector('.report-form');
  if (!draft || !form) return;
  const mirror = (name, target, fallback = '') => {
    const input = form.elements.namedItem(name);
    const output = draft.querySelector(target);
    if (!input || !output) return;
    const update = () => { output.textContent = input.value.trim() || fallback; };
    input.addEventListener('input', update);
    input.addEventListener('change', update);
    update();
  };
  mirror('Type', '[data-draft-type]');
  mirror('Title', '[data-draft-title]', 'Your report preview');
  mirror('Location', '[data-draft-location]', 'Not entered yet');

  // Reuse the image editor's validated preview and URL ownership, including camera/remove/Undo.
  const imageEditor = form.querySelector('[data-image-editor]');
  const source = imageEditor?.querySelector('[data-preview-image]');
  const preview = imageEditor?.querySelector('[data-image-preview]');
  const image = draft.querySelector('[data-draft-image]');
  const aperture = draft.querySelector('[data-draft-aperture]');
  const syncImage = () => {
    const visible = source && preview && !preview.hidden && !!source.getAttribute('src');
    image.hidden = true;
    aperture.hidden = false;
    if (!visible) { image.removeAttribute('src'); return; }
    const url = source.getAttribute('src');
    image.src = url;
    image.decode().then(() => {
      if (image.getAttribute('src') === url && !preview.hidden) {
        image.hidden = false;
        aperture.hidden = true;
      }
    }).catch(() => { /* Keep the aperture if the selected image cannot decode. */ });
  };
  if (imageEditor) {
    new MutationObserver(syncImage).observe(imageEditor, { subtree: true, attributes: true, attributeFilter: ['src', 'hidden'] });
    syncImage();
  }
  const links = [...draft.querySelectorAll('nav a')];
  const selectSection = section => links.forEach(link => {
    if (link.hash === '#' + section.querySelector('h2')?.id) link.setAttribute('aria-current', 'step');
    else link.removeAttribute('aria-current');
  });
  form.querySelectorAll('.form-section').forEach(section => {
    section.addEventListener('focusin', () => selectSection(section));
    if (section.querySelector('.field-validation-error')) {
      const link = links.find(link => link.hash === '#' + section.querySelector('h2')?.id);
      if (link) link.dataset.error = 'true';
    }
  });
  links.forEach(link => link.addEventListener('click', () => {
    const heading = document.getElementById(link.hash.slice(1));
    if (heading) { heading.tabIndex = -1; heading.focus({ preventScroll: true }); }
  }));
})();
