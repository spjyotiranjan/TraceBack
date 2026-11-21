# Theme switcher

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Expose System, Light and Dark with their corresponding Lucide icons and clear selected state. Read the existing traceback.theme.v1 preference, resolve system through prefers-color-scheme, and keep the page and all components in one mode. Follow an OS change only in System mode. Respect keyboard selection, storage failure and no-JS defaults.

**Current integration** — [Views/Shared/_ThemeControl.cshtml](../../src/TraceBack/Views/Shared/_ThemeControl.cshtml) · [wwwroot/js/theme.js](../../src/TraceBack/wwwroot/js/theme.js)

**Page plans** — [Home](../pages/01-home.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Privacy](../pages/13-privacy.md)

## FigCraft references

Theme-option frames `2518` and `2766`.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
