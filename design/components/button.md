# Buttons and submit actions

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use primary, secondary/quiet and destructive variants, with icon-only equivalents only where labels and tooltips make sense. Inputs include label, icon, pending, disabled and destination/action. Use anchors for navigation and buttons for actions. Preserve size across loading, announce pending state and prevent repeat writes. Use the selected text token for icon color.

**Current integration** — [wwwroot/css/site.css](../../src/TraceBack/wwwroot/css/site.css) · [Views/Shared/_Layout.cshtml](../../src/TraceBack/Views/Shared/_Layout.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Report an Item](../pages/06-report-an-item.md) · [Submit a Claim](../pages/08-submit-a-claim.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Shared auth and editor actions appear inside the account library `3771` and editor library `29032`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `1687` | Home / Sign in / Light | Component |
| `1689` | Home / Create account / Light | Component |
| `1874` | Home / Sign in / Dark | Component |
| `1876` | Home / Create account / Dark | Component |
| `3827` | Button / Sign in / Default / 440 / Light | Component |
| `3829` | Button / Sign in / Submitting / 440 / Light | Component |
| `4192` | Button / Sign in / Default / 350 / Light | Component |
| `4194` | Button / Sign in / Submitting / 350 / Light | Component |
| `4547` | Button / Sign in / Default / 440 / Dark | Component |
| `4549` | Button / Sign in / Submitting / 440 / Dark | Component |
| `4952` | Button / Sign in / Default / 350 / Dark | Component |
| `4954` | Button / Sign in / Submitting / 350 / Dark | Component |
| `39648` | Submit actions / Desktop / Light | Component |
| `39653` | Busy actions / Desktop / Light | Component |
| `39938` | Submit actions / Mobile / Light | Component |
| `39943` | Busy actions / Mobile / Light | Component |
| `40222` | Submit actions / Desktop / Dark | Component |
| `40227` | Busy actions / Desktop / Dark | Component |
| `40512` | Submit actions / Mobile / Dark | Component |
| `40517` | Busy actions / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
