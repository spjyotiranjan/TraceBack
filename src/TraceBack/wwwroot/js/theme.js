(() => {
  const key = 'traceback.theme.v1';
  const root = document.documentElement;
  const control = document.querySelector('[data-theme-control]');
  const options = control?.querySelectorAll('input[type=radio]') ?? [];
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const normalize = value => ['light', 'dark', 'system'].includes(value) ? value : 'system';
  let choice = normalize(root.dataset.themeChoice);

  const apply = () => {
    const dark = choice === 'system' ? media.matches : choice === 'dark';
    root.dataset.themeChoice = choice;
    root.dataset.theme = dark ? 'dark' : 'light';
    options.forEach(option => { option.checked = option.value === choice; });
  };
  const select = value => {
    choice = value;
    try { window.localStorage.setItem(key, choice); }
    catch { /* Keep the in-memory choice when persistence is unavailable. */ }
    apply();
  };
  options.forEach(option => option.addEventListener('change', () => {
    if (option.checked) select(option.value);
  }));
  media.addEventListener('change', () => {
    if (choice === 'system') apply();
  });
  window.addEventListener('storage', event => {
    if (event.key === key || event.key === null) {
      choice = normalize(event.newValue);
      apply();
    }
  });
  apply();
  if (control) control.hidden = false;
})();
