(() => {
  const form = document.querySelector('[data-claim-form]');
  if (!form) return;
  const message = form.elements.namedItem('Message');
  const count = form.querySelector('[data-claim-count]');
  function updateCount() {
    count.textContent = `${message.value.length.toLocaleString('en-US')} / 1,000`;
    count.hidden = false;
  }
  message.addEventListener('input', updateCount);
  window.addEventListener('pageshow', updateCount);
  updateCount();

  const drawer = form.closest('[data-claim-drawer]');
  const opener = document.querySelector('[data-claim-open]');
  if (!drawer?.showModal || !opener || !window.fetch) return;
  const close = drawer.querySelector('[data-claim-close]');
  const submit = form.querySelector('[data-claim-submit]');
  const status = form.querySelector('[data-tb-pending-status]');
  const summary = form.querySelector('[data-tb-validation-summary]');
  const summaryTitle = summary.querySelector('strong');
  const messages = summary.querySelector('ul');
  const fieldError = form.querySelector('[data-claim-field-error]');
  let pending = false;
  let unavailable = false;

  function showError(error, messageError) {
    summaryTitle.textContent = messageError ? 'Check your ownership details' : 'Claim not confirmed';
    const entry = document.createElement('li');
    if (messageError) {
      const link = document.createElement('a');
      link.href = `#${message.id}`;
      link.textContent = messageError;
      entry.append(link);
      fieldError.textContent = messageError;
      message.setAttribute('aria-invalid', 'true');
    } else {
      entry.textContent = error;
    }
    messages.replaceChildren(entry);
    summary.hidden = false;
    if (drawer.open) summary.focus();
  }

  function clearError() {
    summary.hidden = true;
    messages.replaceChildren();
    fieldError.textContent = '';
    message.removeAttribute('aria-invalid');
  }

  function setPending(active) {
    pending = active;
    message.readOnly = active;
    submit.toggleAttribute('data-tb-is-pending', active);
    if (active) submit.setAttribute('aria-disabled', 'true');
    else submit.removeAttribute('aria-disabled');
    status.textContent = active ? 'Submitting claim…' : '';
    submit.disabled = unavailable;
  }

  opener.setAttribute('aria-haspopup', 'dialog');
  opener.addEventListener('click', event => {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) return;
    drawer.showModal(); // The browser puts the rest of the page into an inert state.
    event.preventDefault();
    (summary.hidden ? close : summary).focus();
  });
  drawer.querySelectorAll('[data-claim-close]').forEach(button => button.addEventListener('click', () => drawer.close()));
  drawer.addEventListener('close', () => opener.focus({ preventScroll: true }));
  drawer.addEventListener('keydown', event => {
    if (event.key !== 'Tab') return;
    const tabbable = [...drawer.querySelectorAll('button:not(:disabled), a[href], textarea, [tabindex="0"]')]
      .filter(element => element.getClientRects().length);
    const boundary = event.shiftKey ? tabbable[0] : tabbable.at(-1);
    if (document.activeElement !== boundary) return;
    event.preventDefault();
    (event.shiftKey ? tabbable.at(-1) : tabbable[0]).focus();
  });

  form.addEventListener('submit', async event => {
    if (event.defaultPrevented) return;
    event.preventDefault();
    if (pending || unavailable) return;
    clearError();
    const body = new URLSearchParams(new FormData(form));
    setPending(true);
    let navigating = false;
    try {
      const response = await fetch(form.action, {
        method: 'POST', body, redirect: 'manual', headers: { 'X-TraceBack-Claim': 'drawer' },
      });
      if (response.type === 'opaqueredirect' || response.status === 401) {
        unavailable = true;
        showError('Your session may have ended. Copy your message before signing in again.');
        return;
      }
      const result = (response.headers.get('content-type') || '').includes('application/json') ? await response.json() : {};
      if (response.ok && result.redirectUrl) {
        const target = new URL(result.redirectUrl, location.href);
        if (target.origin !== location.origin) throw new Error('Unexpected claim redirect');
        location.assign(target.href);
        navigating = true;
        return;
      }
      unavailable = [400, 403, 404].includes(response.status);
      showError(result.error || (unavailable
        ? 'This claim form is no longer valid. Copy your message before reloading the report.'
        : 'We could not confirm your claim. Check My claims on your Dashboard before trying again.'), result.messageError);
    } catch {
      showError('We could not confirm your claim. Check My claims on your Dashboard before trying again.');
    } finally {
      if (!navigating) setPending(false);
    }
  });
  window.addEventListener('pageshow', event => {
    // Back/forward after a successful write must not offer another POST from the old drawer.
    if (event.persisted && pending) {
      unavailable = true;
      setPending(false);
      showError('Check My claims on your Dashboard before submitting another claim.');
    }
  });
})();
