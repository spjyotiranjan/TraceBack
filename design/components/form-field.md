# Form fields

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Represent label, field ID/name, value, hint, validation, requirement, and optional icon or trailing action. Cover text, email, password, multiline, date, select, readonly/locked, focus, invalid and disabled. Use the shared shaded light field surface, and keep filled labels and typed content legible in dark mode. Link errors and hints through aria-describedby and keep label associations on all variants.

**Current integration** — [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [Views/Account/Login.cshtml](../../src/TraceBack/Views/Account/Login.cshtml) · [Views/Account/Register.cshtml](../../src/TraceBack/Views/Account/Register.cshtml)

**Page plans** — [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Report an Item](../pages/06-report-an-item.md) · [Submit a Claim](../pages/08-submit-a-claim.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Auth libraries `3771` / `5458` and editor library `29032`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `3773` | Field / Email / Default / 440 / Light | Component |
| `3777` | Field / Email / Filled / 440 / Light | Component |
| `3781` | Field / Email / Focus / 440 / Light | Component |
| `3785` | Field / Email / Invalid / 440 / Light | Component |
| `3790` | Field / Password / Default / 440 / Light | Component |
| `3799` | Field / Password / Filled / 440 / Light | Component |
| `3808` | Field / Password / Focus / 440 / Light | Component |
| `3817` | Field / Password / Invalid / 440 / Light | Component |
| `4138` | Field / Email / Default / 350 / Light | Component |
| `4142` | Field / Email / Filled / 350 / Light | Component |
| `4146` | Field / Email / Focus / 350 / Light | Component |
| `4150` | Field / Email / Invalid / 350 / Light | Component |
| `4155` | Field / Password / Default / 350 / Light | Component |
| `4164` | Field / Password / Filled / 350 / Light | Component |
| `4173` | Field / Password / Focus / 350 / Light | Component |
| `4182` | Field / Password / Invalid / 350 / Light | Component |
| `4493` | Field / Email / Default / 440 / Dark | Component |
| `4497` | Field / Email / Filled / 440 / Dark | Component |
| `4501` | Field / Email / Focus / 440 / Dark | Component |
| `4505` | Field / Email / Invalid / 440 / Dark | Component |
| `4510` | Field / Password / Default / 440 / Dark | Component |
| `4519` | Field / Password / Filled / 440 / Dark | Component |
| `4528` | Field / Password / Focus / 440 / Dark | Component |
| `4537` | Field / Password / Invalid / 440 / Dark | Component |
| `4898` | Field / Email / Default / 350 / Dark | Component |
| `4902` | Field / Email / Filled / 350 / Dark | Component |
| `4906` | Field / Email / Focus / 350 / Dark | Component |
| `4910` | Field / Email / Invalid / 350 / Dark | Component |
| `4915` | Field / Password / Default / 350 / Dark | Component |
| `4924` | Field / Password / Filled / 350 / Dark | Component |
| `4933` | Field / Password / Focus / 350 / Dark | Component |
| `4942` | Field / Password / Invalid / 350 / Dark | Component |
| `29033` | Report field / Title / Laptop / Light | Component |
| `29036` | Report field / Category / Laptop / Light | Component |
| `29040` | Report field / Description / Laptop / Light | Component |
| `29043` | Report field / Location description / Laptop / Light | Component |
| `29047` | Report field / Date reported / Laptop / Light | Component |
| `29056` | Report field / Contact preference / Laptop / Light | Component |
| `29060` | Report field / Latitude / Laptop / Light | Component |
| `29063` | Report field / Longitude / Laptop / Light | Component |
| `29066` | Report field / Title / Phone / Light | Component |
| `29069` | Report field / Category / Phone / Light | Component |
| `29073` | Report field / Description / Phone / Light | Component |
| `29076` | Report field / Location description / Phone / Light | Component |
| `29080` | Report field / Date reported / Phone / Light | Component |
| `29089` | Report field / Contact preference / Phone / Light | Component |
| `29093` | Report field / Latitude / Phone / Light | Component |
| `29096` | Report field / Longitude / Phone / Light | Component |
| `29099` | Report field / Title / Laptop / Dark | Component |
| `29102` | Report field / Category / Laptop / Dark | Component |
| `29106` | Report field / Description / Laptop / Dark | Component |
| `29109` | Report field / Location description / Laptop / Dark | Component |
| `29113` | Report field / Date reported / Laptop / Dark | Component |
| `29122` | Report field / Contact preference / Laptop / Dark | Component |
| `29126` | Report field / Latitude / Laptop / Dark | Component |
| `29129` | Report field / Longitude / Laptop / Dark | Component |
| `29132` | Report field / Title / Phone / Dark | Component |
| `29135` | Report field / Category / Phone / Dark | Component |
| `29139` | Report field / Description / Phone / Dark | Component |
| `29142` | Report field / Location description / Phone / Dark | Component |
| `29146` | Report field / Date reported / Phone / Dark | Component |
| `29155` | Report field / Contact preference / Phone / Dark | Component |
| `29159` | Report field / Latitude / Phone / Dark | Component |
| `29162` | Report field / Longitude / Phone / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
