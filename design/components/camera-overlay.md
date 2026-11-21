# Camera capture overlay

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use the four saved device/theme overlays as visual references. Use feature detection for browser camera, existing file/capture fallback, and allow denial, cancel, capture, review and retake. Stop media tracks on exit and restore focus. Attach the resulting supported file to the current form only when confirmed.

**Current integration** — [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [wwwroot/js/report-image.js](../../src/TraceBack/wwwroot/js/report-image.js)

**Page plans** — [Report an Item](../pages/06-report-an-item.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Dedicated prototype overlays section `47278`. Four frames are listed below. The camera hardware flow is implemented in the product.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `47279` | Camera capture / 1440 / Light | 420 × 464 |
| `47288` | Camera capture / 390 / Light | 350 × 464 |
| `47297` | Camera capture / 1440 / Dark | 420 × 464 |
| `47306` | Camera capture / 390 / Dark | 350 × 464 |

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
