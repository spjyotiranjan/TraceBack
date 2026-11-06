(() => {
  const key = 'traceback.theme.v1';
  const root = document.documentElement;
  const toggle = document.querySelector('[data-theme-switch]');
  const reset = document.querySelector('[data-theme-reset]');
  const state = document.querySelector('[data-theme-state]');
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  const normalize = value => ['light', 'dark', 'system'].includes(value) ? value : 'system';
  let choice = normalize(root.dataset.themeChoice);

  const apply = () => {
    const dark = choice === 'system' ? media.matches : choice === 'dark';
    root.dataset.themeChoice = choice;
    root.dataset.theme = dark ? 'dark' : 'light';
    if (toggle) toggle.checked = dark;
    if (state) state.textContent = `${dark ? 'Dark' : 'Light'}${choice === 'system' ? ' from device' : ''}`;
    if (reset) reset.hidden = choice === 'system';
  };
  let motionTimer;
  const select = value => {
    clearTimeout(motionTimer);
    root.dataset.themeAnimate = 'true';
    motionTimer = setTimeout(() => delete root.dataset.themeAnimate, 200);
    choice = value;
    try { window.localStorage.setItem(key, choice); }
    catch { /* Keep the in-memory choice when persistence is unavailable. */ }
    apply();
  };
  toggle?.addEventListener('change', () => select(toggle.checked ? 'dark' : 'light'));
  reset?.addEventListener('click', () => {
    select('system');
    toggle?.focus();
  });
  media.addEventListener('change', () => {
    delete root.dataset.themeAnimate;
    if (choice === 'system') apply();
  });
  window.addEventListener('storage', event => {
    if (event.key === key || event.key === null) {
      delete root.dataset.themeAnimate;
      choice = normalize(event.newValue);
      apply();
    }
  });
  apply();
})();
