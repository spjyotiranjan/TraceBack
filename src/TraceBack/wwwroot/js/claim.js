(() => {
  const form = document.querySelector('[data-claim-form]');
  if (!form) return;

  const submit = form.querySelector('[data-claim-submit]');
  const status = form.querySelector('[data-claim-status]');
  form.addEventListener('submit', event => {
    if (event.defaultPrevented) return;
    submit.disabled = true;
    submit.textContent = 'Submitting claim…';
    status.textContent = 'Submitting claim…';
  });
})();
