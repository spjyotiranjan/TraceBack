# Authentication form and shell

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Make one responsive card/shell with title, fields, summary, submit and account-route link slots. Support sign-in and sign-up fields, credentials and duplicate states, loading and mobile keyboard overflow. Keep pending status and field validation in the same DOM. Balance contrast over the animated background. Bind real autocomplete and local return context.

**Current integration** — [Views/Account/Login.cshtml](../../src/TraceBack/Views/Account/Login.cshtml) · [Views/Account/Register.cshtml](../../src/TraceBack/Views/Account/Register.cshtml)

**Page plans** — [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md)

## FigCraft references

Sign-in library `3771` and registration library `5458`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `3837` | Auth form / Sign in / Default / 440 / Light | Component |
| `3894` | Auth form / Sign in / Focused / 440 / Light | Component |
| `3951` | Auth form / Sign in / Validation / 440 / Light | Component |
| `4018` | Auth form / Sign in / Credentials / 440 / Light | Component |
| `4081` | Auth form / Sign in / Submitting / 440 / Light | Component |
| `4202` | Auth form / Sign in / Default / 350 / Light | Component |
| `4257` | Auth form / Sign in / Focused / 350 / Light | Component |
| `4312` | Auth form / Sign in / Validation / 350 / Light | Component |
| `4377` | Auth form / Sign in / Credentials / 350 / Light | Component |
| `4438` | Auth form / Sign in / Submitting / 350 / Light | Component |
| `4557` | Auth form / Sign in / Default / 440 / Dark | Component |
| `4622` | Auth form / Sign in / Focused / 440 / Dark | Component |
| `4687` | Auth form / Sign in / Validation / 440 / Dark | Component |
| `4762` | Auth form / Sign in / Credentials / 440 / Dark | Component |
| `4833` | Auth form / Sign in / Submitting / 440 / Dark | Component |
| `4962` | Auth form / Sign in / Default / 350 / Dark | Component |
| `5025` | Auth form / Sign in / Focused / 350 / Dark | Component |
| `5088` | Auth form / Sign in / Validation / 350 / Dark | Component |
| `5161` | Auth form / Sign in / Credentials / 350 / Dark | Component |
| `5230` | Auth form / Sign in / Submitting / 350 / Dark | Component |
| `5460` | Auth form / Register / Default / 440 / Light | Component |
| `5529` | Auth form / Register / Focused / 440 / Light | Component |
| `5598` | Auth form / Register / Validation / 440 / Light | Component |
| `5679` | Auth form / Register / Duplicate / 440 / Light | Component |
| `5756` | Auth form / Register / Submitting / 440 / Light | Component |
| `5825` | Auth form / Register / Default / 350 / Light | Component |
| `5895` | Auth form / Register / Focused / 350 / Light | Component |
| `5965` | Auth form / Register / Validation / 350 / Light | Component |
| `6047` | Auth form / Register / Duplicate / 350 / Light | Component |
| `6125` | Auth form / Register / Submitting / 350 / Light | Component |
| `6195` | Auth form / Register / Default / 440 / Dark | Component |
| `6272` | Auth form / Register / Focused / 440 / Dark | Component |
| `6349` | Auth form / Register / Validation / 440 / Dark | Component |
| `6438` | Auth form / Register / Duplicate / 440 / Dark | Component |
| `6523` | Auth form / Register / Submitting / 440 / Dark | Component |
| `6600` | Auth form / Register / Default / 350 / Dark | Component |
| `6678` | Auth form / Register / Focused / 350 / Dark | Component |
| `6756` | Auth form / Register / Validation / 350 / Dark | Component |
| `6846` | Auth form / Register / Duplicate / 350 / Dark | Component |
| `6932` | Auth form / Register / Submitting / 350 / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
