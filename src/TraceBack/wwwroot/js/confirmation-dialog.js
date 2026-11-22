document.querySelectorAll('[data-confirmation-open]').forEach(opener => {
  const dialog = document.getElementById(opener.dataset.confirmationOpen);
  const cancel = dialog.querySelector('[data-confirmation-cancel]');
  const form = dialog.querySelector('form');
  const submit = form.querySelector('[type="submit"]');
  if (typeof dialog.showModal !== 'function') return;
  opener.hidden = false;
  let pending = false;

  opener.addEventListener('click', () => {
    if (pending || dialog.open) return;
    dialog.showModal();
    cancel.focus();
  });
  cancel.addEventListener('click', () => dialog.close());
  dialog.addEventListener('close', () => opener.focus());
  dialog.addEventListener('cancel', event => {
    if (pending) event.preventDefault();
  });
  dialog.addEventListener('keydown', event => {
    if (event.key !== 'Tab') return;
    if (pending || (event.shiftKey && document.activeElement === cancel) ||
        (!event.shiftKey && document.activeElement === submit)) {
      event.preventDefault();
      if (!pending) (event.shiftKey ? submit : cancel).focus();
    }
  });
  form.addEventListener('submit', event => {
    if (pending || !dialog.open) {
      event.preventDefault();
      return;
    }
    pending = true;
    cancel.disabled = submit.disabled = true;
    submit.textContent = 'Working…';
  });
  window.addEventListener('pageshow', event => {
    if (!event.persisted || !pending) return;
    pending = cancel.disabled = submit.disabled = false;
    submit.textContent = opener.textContent;
    if (dialog.open) dialog.close();
  });
});