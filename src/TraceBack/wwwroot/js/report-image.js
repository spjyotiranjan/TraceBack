import { connectCamera, maxPhotoBytes } from './report-camera.js';

const editor = document.querySelector('[data-image-editor]');
if (editor && window.URL?.createObjectURL) {
  const find = selector => editor.querySelector(selector);
  const input = find('#image');
  const remove = find('#remove-image');
  const current = editor.dataset.currentImageUrl;
  const preview = find('[data-image-preview]');
  const image = find('[data-preview-image]');
  const status = find('[data-preview-status]');
  const details = find('[data-preview-details]');
  const error = find('[data-client-image-error]');
  const announcement = find('[data-photo-status]');
  const removeSelection = find('[data-remove-pending]');
  const removeCurrent = find('[data-remove-current]');
  const undo = find('[data-undo-remove]');
  const types = new Set(input.accept.split(','));
  let url;

  function clearPreview() {
    if (url) URL.revokeObjectURL(url);
    url = null;
  }

  function render() {
    const file = input.files[0];
    const removing = !!remove?.checked;
    const src = file ? (url ??= URL.createObjectURL(file)) : !removing && current;
    if (src) image.setAttribute('src', src);
    else image.removeAttribute('src');
    image.alt = file ? 'Selected image preview' : 'Current report image';
    status.textContent = file ? (current ? 'Replacement selected' : 'Image ready to publish') : 'Current image';
    details.textContent = file ? `${file.name} · ${(file.size / 1024 / 1024).toFixed(2)} MiB` : '';
    preview.hidden = !src;
    find('[data-image-empty]').hidden = !!src || removing;
    find('[data-image-removal]').hidden = !removing;
    removeSelection.hidden = !file;
    removeCurrent.hidden = !!file || !current;
  }

  function photoChanged() {
    clearPreview();
    const file = input.files[0];
    if (file && (!file.size || file.size > maxPhotoBytes || !types.has(file.type))) {
      input.value = '';
      if (remove) remove.checked = false;
      error.textContent = 'Choose a JPEG, PNG, GIF, or WebP image no larger than 5 MiB.';
      input.setAttribute('aria-invalid', 'true');
      render();
      return;
    }
    if (file && remove) remove.checked = false;
    error.textContent = '';
    announcement.textContent = file ? `${file.name} selected.` : '';
    if (!find('.field-validation-error')) input.removeAttribute('aria-invalid');
    render();
  }

  if (remove) find('[data-native-remove]').hidden = true;
  find('[data-preview-actions]').hidden = false;
  input.addEventListener('change', photoChanged);
  find('[data-replace-image]').addEventListener('click', () => { input.focus(); input.click(); });
  removeSelection.addEventListener('click', () => {
    input.value = '';
    photoChanged();
    announcement.textContent = current ? 'Current image restored.' : 'Selected photo removed.';
    input.focus();
  });
  removeCurrent.addEventListener('click', () => {
    input.value = '';
    photoChanged();
    remove.checked = true;
    render();
    undo.focus();
  });
  undo.addEventListener('click', () => {
    remove.checked = false;
    render();
    status.focus();
  });
  connectCamera(find('[data-take-photo]'), input, photoChanged);
  window.addEventListener('pagehide', clearPreview);
  window.addEventListener('pageshow', event => { if (event.persisted) render(); });
  render();
}
