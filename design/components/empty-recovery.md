# Empty, unavailable and recovery layouts

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Compose illustration, specific headline and help, primary safe action and optional secondary action or request reference. Handle no active reports, no matches, empty reports/claims, search failure and full-page recovery with distinct copy. Keep the original response status and a real destination. Never automatically replay a POST.

**Current integration** — [Views/Items/NotFound.cshtml](../../src/TraceBack/Views/Items/NotFound.cshtml) · [Views/Account/Denied.cshtml](../../src/TraceBack/Views/Account/Denied.cshtml) · [Views/Shared/Error.cshtml](../../src/TraceBack/Views/Shared/Error.cshtml) · [Views/Dashboard/Index.cshtml](../../src/TraceBack/Views/Dashboard/Index.cshtml)

**Page plans** — [Browse reports](../pages/04-browse.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Access Denied](../pages/10-access-denied.md) · [Item Not Found](../pages/11-item-not-found.md) · [Something Went Wrong](../pages/12-something-went-wrong.md)

## FigCraft references

Inspect the relevant embedded component in the linked page frames.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `37189` | Empty reports / Desktop / Light | Component |
| `37202` | Empty claims / Desktop / Light | Component |
| `37781` | Empty reports / Mobile / Light | Component |
| `37794` | Empty claims / Mobile / Light | Component |
| `38367` | Empty reports / Desktop / Dark | Component |
| `38380` | Empty claims / Desktop / Dark | Component |
| `39007` | Empty reports / Mobile / Dark | Component |
| `39020` | Empty claims / Mobile / Dark | Component |
| `45153` | Access denied / Recovery content / Desktop / Light | Component |
| `45224` | Access denied / Recovery content / Mobile / Light | Component |
| `45292` | Access denied / Recovery content / Desktop / Dark | Component |
| `45371` | Access denied / Recovery content / Mobile / Dark | Component |
| `45452` | Item not found / Recovery content / Desktop / Light | Component |
| `45521` | Item not found / Recovery content / Mobile / Light | Component |
| `45587` | Item not found / Recovery content / Desktop / Dark | Component |
| `45664` | Item not found / Recovery content / Mobile / Dark | Component |
| `45742` | Something went wrong / Recovery content / Desktop / Light | Component |
| `45849` | Something went wrong / Recovery content / Mobile / Light | Component |
| `45955` | Something went wrong / Recovery content / Desktop / Dark | Component |
| `46078` | Something went wrong / Recovery content / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
