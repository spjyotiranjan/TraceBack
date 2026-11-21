# Browse filters and removable chips

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Bind Query, Type, Status, Category and Location as GET values. Keep desktop filters visible and provide the mobile expandable group with aria-expanded and a persistent clear/apply path. Present named chip removal, preserve unrelated parameters, and reset Page on changes. Use only the allowed type/status values and let the server normalize them.

**Current integration** — [Views/Shared/_SearchFilters.cshtml](../../src/TraceBack/Views/Shared/_SearchFilters.cshtml) · [Views/Shared/_FilterChip.cshtml](../../src/TraceBack/Views/Shared/_FilterChip.cshtml)

**Page plans** — [Browse reports](../pages/04-browse.md)

## FigCraft references

Inspect the relevant embedded component in the linked page frames.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `7122` | Filters / Laptop / Light / Default | Component |
| `7152` | Filters / Laptop / Light / Applied | Component |
| `7182` | Filters / Laptop / Light / Empty match | Component |
| `7321` | Filters / Phone / Light / Default | Component |
| `7338` | Filters / Phone / Light / Expanded | Component |
| `7368` | Filters / Phone / Light / Applied | Component |
| `7385` | Filters / Phone / Light / Empty match | Component |
| `7511` | Filters / Laptop / Dark / Default | Component |
| `7541` | Filters / Laptop / Dark / Applied | Component |
| `7571` | Filters / Laptop / Dark / Empty match | Component |
| `7710` | Filters / Phone / Dark / Default | Component |
| `7727` | Filters / Phone / Dark / Expanded | Component |
| `7757` | Filters / Phone / Dark / Applied | Component |
| `7774` | Filters / Phone / Dark / Empty match | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
