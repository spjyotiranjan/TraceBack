# Shared authentication layout

`Views/Account/_AuthLayout.cshtml` is the nested layout for `Login.cshtml` and `Register.cshtml`. It owns the form heading, description, server/client summary, pending submit and alternate-account link. Each view supplies the same strongly typed Razor fields and uses the shared field and button helpers. The account controller still owns authentication, validation, antiforgery and local return handling. It sets a distinct summary heading for incorrect credentials or an existing email; client validation on retry replaces the server error items and restores the generic summary heading.

At the shared 56rem breakpoint, the unframed desktop campus message sits beside the 27.5rem form. Below it, the message is omitted and the centered 21.875rem form shrinks within safe-area gutters. The rounded mobile backing is decorative. No panel has its own scrollbar or fixed submit action: the whole document reflows and scrolls when a validation summary appears or the viewport is shortened.

## PDF comparison

Compared the real guest routes before and after with disposable MongoDB fixtures, Light/Dark, 1440 and 390 CSS px, and the route art at its reduced-motion pose. Each PDF has one page with a grid of states. Layout and measurements below refer to the frames within that page (see the [sign-in](../design/pages/02-sign-in.md) and [registration](../design/pages/03-create-account.md) checklists for all frame IDs).

| PDF, p. 1 | Observed difference and owner | Resolution |
| --- | --- | --- |
| 02/03 default and focused; `3856`, `4221`, `5483`, `5848` | The auth heading sat in a separate solid card; the inputs started at x801 on desktop and x41 on mobile, were 44px tall, and had no hints or lock. | `_AuthLayout` now puts the heading in the form and the campus copy at desktop only. Inputs start at x840/x20, match the 440/350px reference widths and 52px height, and use the shared focus ring. The account link is centered without the old divider. The TraceBack mark remains in the shared shell. |
| 02 validation/credentials `3975`, `4336`, `4040`, `4399`; 03 validation/duplicate `5627`, `5994`, `5706`, `6074` | Same generic summary for all failures; dense and narrow fields. | Dedicated credential/duplicate headings, with the original messages and any linked and inline errors retained. Errors expand the document and focus the summary. |
| 02/03 submitting `4100`, `4457`, `5779`, `6148` | Status text appeared below an unchanged button. | The existing live status is positioned over the button label with no added tab stop or second submit. The button's accessible name stays stable. |
| 02 return `46194`, `46236`, `46278`, `46328` | Return-context form used the old split-card layout. | The same revised login form renders on a real protected-route challenge. Visual samples used Browse's challenge; the Mongo-backed integration verifies return to a seeded item and local-only destination handling. |

This is not a pixel-identical recreation. The revised shared navigation and fixed artwork still apply; the shared document-flow Privacy footer is unchanged (e.g. `3856` and `5848` show a differently placed, bare link). The default form controls are within a few vertical CSS pixels of the PDF. Error frames retain the stronger focus outline, linked summary and production message text (`3975`, `4336`, `5627`, `5994`). The full-name label keeps its existing 2–80 guidance (`5483`, `5848`); server responses retain non-secret fields and never return a previously submitted password, even when the mockup shows masking dots (`4040`, `4399`). Frame `5994` clips the lower form in the export; the real form intentionally scrolls to keep the button and sign-in link accessible.

## Verification

With Node resolving Playwright and its matching Chromium installed, run from the repository root in Git Bash (`pwd` instead of `pwd -W` on other platforms). Point `TRACEBACK_TEST_MONGO_URI` only at a disposable local MongoDB instance.

```bash
AUTH_LAYOUT_BROWSER_SCRIPT="$(pwd -W)/tests/ui/auth-layout-check.cjs" \
SHARED_CONTROLS_BROWSER_SCRIPT="$(pwd -W)/tests/ui/shared-controls-check.cjs" \
TRACEBACK_BROWSER=1 TRACEBACK_BROWSER_TESTS=1 TRACEBACK_EDITOR_BROWSER=1 \
dotnet test -c Release --nologo
```

The auth Chromium suite covers both themes at 1440/896/895/390/320 CSS px, keyboard order and focus, touch links, validation anchors, pending/duplicate-submit prevention, native 200% zoom and a 360px-tall viewport as a proxy for space lost to a software keyboard. The separate background suite checks art anchoring, animation phases and reduced motion. Mongo-backed MVC tests cover antiforgery, blank and invalid form submissions, credential/duplicate errors, safe redirects, and successful signup with automatic sign-in and the Dashboard notice.
