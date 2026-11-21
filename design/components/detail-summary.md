# Report summary and metadata

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Expose full title, description, type/status, dates, contact preference and item media by context. Allow unlimited heading wrap, clamp only the main description to eight lines conditionally, and implement an inline underlined expansion button with aria-expanded. Claim summaries have tighter layout but must wrap and keep image fallback.

**Current integration** — [Views/Items/Details.cshtml](../../src/TraceBack/Views/Items/Details.cshtml) · [Views/Shared/_FactList.cshtml](../../src/TraceBack/Views/Shared/_FactList.cshtml)

**Page plans** — [Item Details](../pages/05-item-details.md) · [Submit a Claim](../pages/08-submit-a-claim.md)

## FigCraft references

Embedded full report summary in Details `12080`. Claim summary panel represented by the native components below.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `39623` | Item summary / Desktop / Light | Component |
| `39629` | Long item summary / Desktop / Light | Component |
| `39913` | Item summary / Mobile / Light | Component |
| `39919` | Long item summary / Mobile / Light | Component |
| `40197` | Item summary / Desktop / Dark | Component |
| `40203` | Long item summary / Desktop / Dark | Component |
| `40487` | Item summary / Mobile / Dark | Component |
| `40493` | Long item summary / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
