# Sign in

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET and POST /Account/Login`

**Current view** — [Account/Login.cshtml](../../src/TraceBack/Views/Account/Login.cshtml)

Keep the auth form readable over the blended account background. Use the saved desktop split composition with context on the left and the 440 px form on the right. On mobile, use the 350 px form and allow document scrolling with the software keyboard open. Retain the Email and Password controls and the current password-masking behavior. Use the same panel size across errors where the available space allows.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `3856` | Animated background / Laptop / Light (`5338`, 1440 × 1028). Navbar / Guest / Laptop / Light (`3857`, 1248 × 72). Auth form / Sign in / Default / 440 / Light (`3874`, 440 × 412) |
| `4221` | Animated background / Mobile / Light (`5368`, 390 × 844). Navbar / Guest / Mobile / Light (`4222`, 366 × 60). Auth form / Sign in / Default / 350 / Light (`4237`, 350 × 412) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Default | Guest opens the form | Show email, password, primary submit, and link to Create account. | Designed · `3856`, `4221`, `4576`, `4981` |
| Focused | Field receives focus | Show an obvious focus ring. Keep labels and help visible and keyboard order logical. | Designed · `3913`, `4276`, `4641`, `5044` |
| Validation | Missing required value or malformed email | Keep the submitted email and safe context. Display the relevant field messages, mark invalid fields and focus the summary or first invalid field. | Designed · `3975`, `4336`, `4711`, `5112` |
| Credentials | Authentication fails | Show the existing generic credential error. Offer another attempt and keep the password field ready for input. | Designed · `4040`, `4399`, `4784`, `5183` |
| Submitting | Valid form begins posting | Show pending feedback, guard duplicate posts and announce the change. | Designed · `4100`, `4457`, `4852`, `5249` |
| Return to item | A protected item action sends a local returnUrl | Preserve local returnUrl through validation and POST. Navigate to it on success, otherwise use My Dashboard. Reject external redirect targets via existing server check. | Designed · `46194`, `46236`, `46278`, `46328` |
| Safe redirect fallback | The return URL is missing or external | Retain the existing local URL check and go to Dashboard. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | The canvas switches between preset sign-in outcomes and return paths. Resolve them with the real authentication POST and its local redirect. | Prototype simulation |

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

1. Apply the auth form, shaded light fields and blended background to Login.cshtml.
2. Preserve model binding, antiforgery and server messages. Use email and current-password autocomplete.
3. Test blank fields, incorrect credentials, successful sign-in, local return and external return URL.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **02 · Sign in**, node `36417`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `3856` | Account / Login / 1440 / Light / Default | 1440 × 1000 |
| `4221` | Account / Login / 390 / Light / Default | 390 × 844 |
| `4576` | Account / Login / 1440 / Dark / Default | 1440 × 1000 |
| `4981` | Account / Login / 390 / Dark / Default | 390 × 844 |
| `3913` | Account / Login / 1440 / Light / Focused | 1440 × 1000 |
| `46194` | Account / Login / 1440 / Light / Return to item | 1440 × 1000 |
| `4276` | Account / Login / 390 / Light / Focused | 390 × 844 |
| `46236` | Account / Login / 390 / Light / Return to item | 390 × 844 |
| `4641` | Account / Login / 1440 / Dark / Focused | 1440 × 1000 |
| `46278` | Account / Login / 1440 / Dark / Return to item | 1440 × 1000 |
| `5044` | Account / Login / 390 / Dark / Focused | 390 × 844 |
| `46328` | Account / Login / 390 / Dark / Return to item | 390 × 844 |
| `3975` | Account / Login / 1440 / Light / Validation | 1440 × 1000 |
| `4336` | Account / Login / 390 / Light / Validation | 390 × 844 |
| `4711` | Account / Login / 1440 / Dark / Validation | 1440 × 1000 |
| `5112` | Account / Login / 390 / Dark / Validation | 390 × 844 |
| `4040` | Account / Login / 1440 / Light / Credentials | 1440 × 1000 |
| `4399` | Account / Login / 390 / Light / Credentials | 390 × 844 |
| `4784` | Account / Login / 1440 / Dark / Credentials | 1440 × 1000 |
| `5183` | Account / Login / 390 / Dark / Credentials | 390 × 844 |
| `4100` | Account / Login / 1440 / Light / Submitting | 1440 × 1000 |
| `4457` | Account / Login / 390 / Light / Submitting | 390 × 844 |
| `4852` | Account / Login / 1440 / Dark / Submitting | 1440 × 1000 |
| `5249` | Account / Login / 390 / Dark / Submitting | 390 × 844 |
