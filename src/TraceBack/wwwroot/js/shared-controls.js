(() => {
  const summarySelector = '[data-tb-validation-summary]';
  const fieldMessages = '[data-valmsg-for], .field-validation-error';
  let focusedServerSummary = false;

  for (const form of document.forms) {
    const summary = form.querySelector(summarySelector);
    if (!summary && !form.querySelector(fieldMessages)) continue;
    const list = summary?.querySelector('ul');
    const invalidFields = new Set();
    let clientErrors;

    function syncFieldErrors() {
      const fields = Array.from(form.elements).filter(e => e.matches('input:not([type=hidden]), select, textarea'));
      const invalid = new Set();
      for (const message of form.querySelectorAll(fieldMessages)) {
        if (!message.classList.contains('field-validation-error')) continue;
        const name = message.dataset.valmsgFor;
        const id = message.id || message.closest('[id]')?.id;
        for (const field of fields) {
          if (name ? name.toLowerCase() === field.name.toLowerCase()
            : id && field.getAttribute('aria-describedby')?.split(/\s+/).includes(id)) invalid.add(field);
        }
      }
      for (const field of invalidFields) {
        if (!invalid.has(field)) {
          field.removeAttribute('aria-invalid');
          invalidFields.delete(field);
        }
      }
      for (const field of invalid) {
        field.setAttribute('aria-invalid', 'true');
        invalidFields.add(field);
      }
      return invalid;
    }

    syncFieldErrors();
    if (summary && !summary.hidden && !focusedServerSummary) {
      summary.focus();
      focusedServerSummary = true;
    }

    const observer = new MutationObserver(records => {
      if (!records.some(record => record.target instanceof Element &&
          (record.target.matches(fieldMessages) || record.target.closest('[data-valmsg-for], [id$="-error"]')))) return;
      const invalid = syncFieldErrors();
      // Clear corrected client errors without discarding unrelated server-level errors.
      if (!clientErrors) return;
      for (const [item, field] of clientErrors) {
        if (!invalid.has(field) && !field.classList.contains('input-validation-error')) {
          item.remove();
          clientErrors.delete(item);
        }
      }
      summary.hidden = !list.childElementCount;
    });
    observer.observe(form, { subtree: true, childList: true, attributes: true, attributeFilter: ['class'] });

    if (!summary) continue;
    window.jQuery?.(form).on('invalid-form.validate', (_event, validator) => {
      list.replaceChildren();
      clientErrors = new Map();
      for (const error of validator.errorList) {
        const item = document.createElement('li');
        const field = error.element;
        if (field.id) {
          const link = document.createElement('a');
          link.href = '#' + encodeURIComponent(field.id);
          link.textContent = error.message;
          item.append(link);
        } else {
          item.textContent = error.message;
        }
        list.append(item);
        clientErrors.set(item, field);
      }
      summary.hidden = !list.childElementCount;
      // jQuery Validate may focus the first invalid control after firing this event.
      window.setTimeout(() => { if (!summary.hidden) summary.focus(); }, 0);
    });

    summary.addEventListener('click', event => {
      const link = event.target.closest('a[href^="#"]');
      if (!link) return;
      const field = document.getElementById(decodeURIComponent(link.hash.slice(1)));
      if (field?.form === form && !field.matches(':disabled, [type=hidden]')) {
        event.preventDefault();
        field.focus();
      }
    });
  }

  const pending = new Map();
  // A capture-phase guard stops subsequent submits before form validation can run again.
  document.addEventListener('submit', event => {
    if (!pending.has(event.target)) return;
    event.preventDefault();
    event.stopImmediatePropagation();
  }, true);
  document.addEventListener('click', event => {
    if (!(event.target instanceof Element)) return;
    const submitter = event.target.closest('button, input[type=submit], input[type=image]');
    if (submitter?.type === 'submit' || submitter?.type === 'image') {
      if (pending.has(submitter.form)) event.preventDefault();
    }
  }, true);

  // Run after validation and allow the first native submission to carry its original payload.
  window.addEventListener('submit', event => {
    const form = event.target;
    const submitter = event.submitter;
    const message = submitter?.dataset.tbPending?.trim();
    if (!(form instanceof HTMLFormElement) || !message || event.defaultPrevented) return;
    if ((submitter.hasAttribute('formmethod') ? submitter.formMethod : form.method) !== 'post') return;
    const target = submitter.getAttribute('formtarget') ?? form.getAttribute('target')
      ?? document.querySelector('base[target]')?.getAttribute('target');
    if (target && target.toLowerCase() !== '_self') return;
    const status = form.querySelector('[data-tb-pending-status]');
    if (!status) return;
    const previous = submitter.getAttribute('aria-disabled');
    submitter.setAttribute('aria-disabled', 'true');
    submitter.setAttribute('data-tb-is-pending', '');
    status.textContent = message;
    pending.set(form, () => {
      if (previous === null) submitter.removeAttribute('aria-disabled');
      else submitter.setAttribute('aria-disabled', previous);
      submitter.removeAttribute('data-tb-is-pending');
      status.textContent = '';
    });
  });

  // Back/forward can restore the previous document with its in-memory lock intact.
  window.addEventListener('pageshow', event => {
    if (!event.persisted) return;
    for (const reset of pending.values()) reset();
    pending.clear();
  });
})();
