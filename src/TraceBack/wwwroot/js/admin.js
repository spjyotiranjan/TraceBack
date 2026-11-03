document.querySelectorAll('[data-decision-form]').forEach(form => {
  form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) {
      event.preventDefault();
      return;
    }

    const approve = form.querySelector('[name="approve"]').value === 'true';
    const button = form.querySelector('button[type="submit"]');
    const status = form.closest('.tb-decision-row').querySelector('[data-decision-status]');
    button.disabled = true;
    button.textContent = approve ? 'Approving…' : 'Rejecting…';
    status.textContent = approve ? 'Approving claim.' : 'Rejecting claim.';
  });
});
