(() => {
  const key = 'traceback.theme.v1';
  const root = document.documentElement;
  const choices = [...document.querySelectorAll('input[name="theme-choice"]')];
  const media = window.matchMedia('(prefers-color-scheme: dark)');
  let choice = root.dataset.themeChoice || 'system';

  const apply = () => {
    root.dataset.themeChoice = choice;
    root.dataset.theme = choice === 'system' ? (media.matches ? 'dark' : 'light') : choice;
    choices.forEach(input => { input.checked = input.value === choice; });
  };

  choices.forEach(input => input.addEventListener('change', () => {
    if (!input.checked) return;
    choice = input.value;
    try { window.localStorage.setItem(key, choice); }
    catch { choice = 'system'; }
    apply();
  }));

  media.addEventListener('change', () => { if (choice === 'system') apply(); });
  apply();
})();
