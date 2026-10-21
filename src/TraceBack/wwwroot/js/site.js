document.querySelectorAll('[data-confirm]').forEach(button => {
  button.addEventListener('click', event => {
    if (!window.confirm(button.dataset.confirm)) event.preventDefault();
  });
});
const errorSummary = document.getElementById('form-errors');
if (errorSummary) errorSummary.focus();
document.querySelectorAll('.field-validation-error[data-valmsg-for]').forEach(message => {
  const field = document.querySelector(`[name="${CSS.escape(message.dataset.valmsgFor)}"]`);
  if (field) field.setAttribute('aria-invalid', 'true');
});
document.querySelectorAll('[id$="-error"] .field-validation-error').forEach(message => {
  const field = document.getElementById(message.parentElement.id.slice(0, -6));
  if (field) field.setAttribute('aria-invalid', 'true');
});
