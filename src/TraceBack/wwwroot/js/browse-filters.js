(() => {
    const toggle = document.querySelector('.browse-filter-toggle');
    const options = document.getElementById('browse-filter-options');
    if (!toggle || !options) return;

    const desktop = matchMedia('(min-width: 56rem)');
    const label = toggle.querySelector('.browse-filter-toggle-label');
    const clear = options.parentElement.querySelector('.browse-filter-actions > a');
    let expanded = false;

    function update() {
        const active = document.activeElement;
        toggle.hidden = desktop.matches;
        toggle.setAttribute('aria-expanded', String(expanded));
        label.textContent = expanded ? 'Hide filters' : 'Filters';
        options.hidden = !desktop.matches && !expanded;
        if (options.hidden && (options.contains(active) || active === clear)) toggle.focus();
        if (toggle.hidden && active === toggle) options.querySelector('select')?.focus();
    }

    toggle.addEventListener('click', () => {
        expanded = !expanded;
        update();
    });
    toggle.form.addEventListener('keydown', event => {
        if (desktop.matches || !expanded || event.defaultPrevented || event.key !== 'Escape') return;
        // Let native select menus handle Escape themselves.
        if (event.target !== toggle && (!options.contains(event.target) || event.target.matches('select'))) return;
        event.preventDefault();
        expanded = false;
        update();
    });
    desktop.addEventListener('change', update);
    update();
})();
