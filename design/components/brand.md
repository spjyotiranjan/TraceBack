# TraceBack brand mark

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use the revised saved vector mark for the shared brand link, retaining original proportions. Export SVG, use matching theme foreground, and pair with accessible TraceBack Home name. Avoid using a raster screenshot as the brand source.

**Current integration** — [Views/Shared/_Layout.cshtml](../../src/TraceBack/Views/Shared/_Layout.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Browse reports](../pages/04-browse.md) · [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Edit Report](../pages/09-edit-report.md) · [Access Denied](../pages/10-access-denied.md) · [Item Not Found](../pages/11-item-not-found.md) · [Something Went Wrong](../pages/12-something-went-wrong.md) · [Privacy](../pages/13-privacy.md)

## FigCraft references

Revised vector master frame `1036`. The component list also includes legacy Home brand instances for locating their current use.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `1680` | Home / brand / Light | Component |
| `1867` | Home / brand / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
