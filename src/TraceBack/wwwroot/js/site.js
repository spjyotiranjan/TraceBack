document.querySelectorAll('button[data-confirm]').forEach(button => {
  button.addEventListener('click', event => {
    if (!window.confirm(button.dataset.confirm)) event.preventDefault();
  });
});
const errorSummary = document.getElementById('form-errors');
if (errorSummary && !errorSummary.hidden) errorSummary.focus();
document.querySelectorAll('form').forEach(form => {
  const summary = form.querySelector('#form-errors');
  if (!summary || !window.jQuery) return;

  window.jQuery(form).on('invalid-form.validate', (_event, validator) => {
    const list = summary.querySelector('ul');
    list.replaceChildren();
    for (const error of validator.errorList) {
      const item = document.createElement('li');
      const fieldId = error.element.id;
      if (fieldId) {
        const link = document.createElement('a');
        link.href = '#' + fieldId;
        link.textContent = error.message;
        item.append(link);
      } else {
        item.textContent = error.message;
      }
      list.append(item);
    }
    summary.hidden = false;
    // jQuery Validate focuses its first invalid field after this event returns.
    window.setTimeout(() => summary.focus(), 0);
  });
});
document.querySelectorAll('.field-validation-error[data-valmsg-for]').forEach(message => {
  const field = document.querySelector(`[name="${CSS.escape(message.dataset.valmsgFor)}"]`);
  if (field) field.setAttribute('aria-invalid', 'true');
});
document.querySelectorAll('[id$="-error"] .field-validation-error').forEach(message => {
  const field = document.getElementById(message.parentElement.id.slice(0, -6));
  if (field) field.setAttribute('aria-invalid', 'true');
});

document.querySelectorAll('[data-retry]').forEach(button => {
  button.addEventListener('click', () => window.location.reload());
});
