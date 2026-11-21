# Submit a Claim drawer and direct form

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- One form accepts item summary, private message, validation, in-flight state and dismissal/submit actions. Size to approximately 30% desktop and full width mobile. Make background inert, focus the drawer, restore opener, and keep internal scrolling/keyboard usable. Direct URL uses the same form with normal navigation. Handle server errors in place and preserve Dashboard redirect.

**Current integration** — [Views/Shared/_ClaimPanel.cshtml](../../src/TraceBack/Views/Shared/_ClaimPanel.cshtml) · [Views/Claims/Create.cshtml](../../src/TraceBack/Views/Claims/Create.cshtml) · [wwwroot/js/claim.js](../../src/TraceBack/wwwroot/js/claim.js)

**Page plans** — [Item Details](../pages/05-item-details.md) · [Submit a Claim](../pages/08-submit-a-claim.md)

## FigCraft references

Dedicated drawer section `39621`. Default panels `39658` (desktop light) and `39948` (mobile light).

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `39639` | Privacy notice / Desktop / Light | Component |
| `39929` | Privacy notice / Mobile / Light | Component |
| `40213` | Privacy notice / Desktop / Dark | Component |
| `40503` | Privacy notice / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
