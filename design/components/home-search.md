# Home and Browse search input

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Submit to GET /Items using Query, allow Enter, and distinguish clearing the text input from submitting. On Browse preserve other filter values and reset Page. Home begins a fresh search. Show label, Lucide Search and named clear action. Make the control keyboard-friendly without presenting a custom combobox.

**Current integration** — [Views/Home/Index.cshtml](../../src/TraceBack/Views/Home/Index.cshtml) · [Views/Shared/_SearchFilters.cshtml](../../src/TraceBack/Views/Shared/_SearchFilters.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Browse reports](../pages/04-browse.md)

## FigCraft references

Home library `1678` and Browse library `7012`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `2093` | Home / search button / laptop / Light | Component |
| `2125` | Home / search button / mobile / Light | Component |
| `2168` | Home / search button / laptop / Dark | Component |
| `2200` | Home / search button / mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
