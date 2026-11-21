# Create account

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET and POST /Account/Register`

**Current view** — [Account/Register.cshtml](../../src/TraceBack/Views/Account/Register.cshtml)

Use the same desktop split composition, mobile auth shell and decoration as sign-in. Place all validation beneath the relevant controls and allow the panel to expand vertically. At mobile widths preserve access to the submit and Sign in link after keyboard dismissal.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `5483` | Animated background / Laptop / Light (`5484`, 1440 × 1028). Auth form / Register / Default / 440 / Light (`5489`, 440 × 520). Navbar / Guest / Laptop / Light (`5516`, 1248 × 72) |
| `5848` | Animated background / Mobile / Light (`5849`, 390 × 844). Scrollable account form (`5854`, 390 × 760). Auth form / Register / Default / 350 / Light (`5855`, 350 × 544). Navbar / Guest / Mobile / Light (`5880`, 366 × 60) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Default | Guest opens registration | Show Name, Email and Password, the 8-character requirement and the Sign in link. | Designed · `5483`, `5848`, `6218`, `6623` |
| Focused | Field receives focus | Keep visible focus, label association and selected theme. | Designed · `5552`, `5918`, `6295`, `6701` |
| Validation | Name is outside 2–80 characters, email invalid, or password under 8 | Show model errors. Preserve non-secret values and let the user correct the form. | Designed · `5627`, `5994`, `6378`, `6785` |
| Duplicate | Server identifies an existing account | Show existing duplicate feedback and keep Sign in reachable. | Designed · `5706`, `6074`, `6465`, `6873` |
| Submitting | Valid POST starts | Prevent double submit. On success retain registration’s automatic sign-in and redirect to Dashboard. | Designed · `5779`, `6148`, `6546`, `6955` |
| Prototype interaction | FigCraft demonstrates the flow | Canvas outcome switches are demonstrations. Use the registration POST and server validation. | Prototype simulation |

## Reusable components and assets

- [Authentication form and shell](../components/auth-form.md)
- [TraceBack brand mark](../components/brand.md)
- [Buttons and submit actions](../components/button.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Footer](../components/footer.md)
- [Form fields](../components/form-field.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants.

## Build steps and acceptance checks

1. Compose Register.cshtml with the same auth form and validation components as Login.
2. Use name, email and new-password autocomplete and preserve antiforgery and server validation.
3. Test duplicate and successful registration and verify layout with several simultaneous field errors.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **03 · Create account**, node `36422`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `5483` | Account / Register / 1440 / Light / Default | 1440 × 1000 |
| `5848` | Account / Register / 390 / Light / Default | 390 × 844 |
| `6218` | Account / Register / 1440 / Dark / Default | 1440 × 1000 |
| `6623` | Account / Register / 390 / Dark / Default | 390 × 844 |
| `5552` | Account / Register / 1440 / Light / Focused | 1440 × 1000 |
| `5918` | Account / Register / 390 / Light / Focused | 390 × 844 |
| `6295` | Account / Register / 1440 / Dark / Focused | 1440 × 1000 |
| `6701` | Account / Register / 390 / Dark / Focused | 390 × 844 |
| `5627` | Account / Register / 1440 / Light / Validation | 1440 × 1028 |
| `5994` | Account / Register / 390 / Light / Validation | 390 × 844 |
| `6378` | Account / Register / 1440 / Dark / Validation | 1440 × 1028 |
| `6785` | Account / Register / 390 / Dark / Validation | 390 × 844 |
| `5706` | Account / Register / 1440 / Light / Duplicate | 1440 × 1000 |
| `6074` | Account / Register / 390 / Light / Duplicate | 390 × 844 |
| `6465` | Account / Register / 1440 / Dark / Duplicate | 1440 × 1000 |
| `6873` | Account / Register / 390 / Dark / Duplicate | 390 × 844 |
| `5779` | Account / Register / 1440 / Light / Submitting | 1440 × 1000 |
| `6148` | Account / Register / 390 / Light / Submitting | 390 × 844 |
| `6546` | Account / Register / 1440 / Dark / Submitting | 1440 × 1000 |
| `6955` | Account / Register / 390 / Dark / Submitting | 390 × 844 |
