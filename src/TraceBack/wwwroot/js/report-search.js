document.querySelectorAll('[data-search-field]').forEach(field => {
  const query = field.querySelector('input');
  const clear = field.querySelector('button');
  const update = () => { clear.hidden = query.value.length === 0; };
  field.classList.add('is-enhanced');
  query.addEventListener('input', update);
  window.addEventListener('pageshow', update);
  update();
  clear.addEventListener('click', () => {
    query.value = '';
    query.focus();
    query.dispatchEvent(new Event('input', { bubbles: true }));
  });
});