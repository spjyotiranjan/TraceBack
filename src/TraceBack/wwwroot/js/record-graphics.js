(() => {
  const draft = document.querySelector('[data-draft-canvas]');
  const form = document.querySelector('.report-form');
  if (!draft || !form) return;
  const mirror = (name, target, fallback = '') => {
    const input = form.elements.namedItem(name);
    const output = draft.querySelector(target);
    if (!input || !output) return;
    const update = () => { output.textContent = input.value.trim() || fallback; };
    form.addEventListener('input', event => { if (event.target.name === name) update(); });
    form.addEventListener('change', event => { if (event.target.name === name) update(); });
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
})();
