# Validation, status and retry feedback

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use form-level validation summary plus associated field errors, pending status, one-time success banners and recoverable service errors. Reuse TempData success/error and server validation where wired. Mark invalid fields, move focus for submitted invalid forms, announce status changes and keep retry in the affected region. Errors stay visible until remedied or dismissed.

**Current integration** — [Views/Shared/_ValidationSummary.cshtml](../../src/TraceBack/Views/Shared/_ValidationSummary.cshtml) · [Views/Shared/_Layout.cshtml](../../src/TraceBack/Views/Shared/_Layout.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Submit a Claim](../pages/08-submit-a-claim.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Inspect the relevant embedded component in the linked page frames.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `3831` | Auth feedback / Validation / 440 / Light | Component |
| `3834` | Auth feedback / Credentials / 440 / Light | Component |
| `4196` | Auth feedback / Validation / 350 / Light | Component |
| `4199` | Auth feedback / Credentials / 350 / Light | Component |
| `4551` | Auth feedback / Validation / 440 / Dark | Component |
| `4554` | Auth feedback / Credentials / 440 / Dark | Component |
| `4956` | Auth feedback / Validation / 350 / Dark | Component |
| `4959` | Auth feedback / Credentials / 350 / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
