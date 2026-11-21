# Glass navigation and menus

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use the revised brand as a Home link. Render a shared floating glass top bar in desktop and mobile. Guest controls expose Browse and account actions. Authenticated desktop exposes Browse, Report and Dashboard. The mobile member dock uses only the three Lucide icons with accessible names and a selected state. Keep about 350×64 at the 390 reference, rounded and detached with no full-width backing.
- Inputs are auth state, role, current route, theme and menu-open state. Menus close on Escape/outside activation and restore focus. Use 44 px targets and aria-current for destinations. Sign out is an antiforgery POST. Admin-only menu entries remain role gated. Preserve safe-area padding, layer below drawers and reserve document clearance.

**Current integration** — [Views/Shared/_Layout.cshtml](../../src/TraceBack/Views/Shared/_Layout.cshtml) · [wwwroot/js/site.js](../../src/TraceBack/wwwroot/js/site.js)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Browse reports](../pages/04-browse.md) · [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Edit Report](../pages/09-edit-report.md) · [Access Denied](../pages/10-access-denied.md) · [Item Not Found](../pages/11-item-not-found.md) · [Something Went Wrong](../pages/12-something-went-wrong.md) · [Privacy](../pages/13-privacy.md)

Use the [theme switcher](theme-control.md) and [brand mark](brand.md) as shared dependencies.

## FigCraft references

Library `2516`. Guest menus `2575` / `2839`. Member menus `2646` / `2926`. Role-gated Admin menus `2728` / `3024` are existing shell references only.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `1691` | Home / Menu / Light | Component |
| `1878` | Home / Menu / Dark | Component |
| `2545` | Navbar / Guest / Laptop / Light | Component |
| `2559` | Navbar / Guest / Mobile / Light | Component |
| `2595` | Navbar / Member / Laptop / Light | Component |
| `2630` | Navbar / Member / Mobile / Light | Component |
| `2678` | Navbar / Admin / Laptop / Light | Component |
| `2712` | Navbar / Admin / Mobile / Light | Component |
| `2793` | Navbar / Guest / Laptop / Dark | Component |
| `2815` | Navbar / Guest / Mobile / Dark | Component |
| `2859` | Navbar / Member / Laptop / Dark | Component |
| `2902` | Navbar / Member / Mobile / Dark | Component |
| `2958` | Navbar / Admin / Laptop / Dark | Component |
| `3000` | Navbar / Admin / Mobile / Dark | Component |
| `3495` | Bottom navbar / Light / None | Component |
| `3513` | Bottom navbar / Light / Browse | Component |
| `3533` | Bottom navbar / Light / Report | Component |
| `3553` | Bottom navbar / Light / Dashboard | Component |
| `3573` | Bottom navbar / Dark / None | Component |
| `3591` | Bottom navbar / Dark / Browse | Component |
| `3611` | Bottom navbar / Dark / Report | Component |
| `3631` | Bottom navbar / Dark / Dashboard | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
