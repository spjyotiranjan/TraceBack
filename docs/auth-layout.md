# Shared authentication layout

`Views/Account/_AuthLayout.cshtml` is the nested layout for `Login.cshtml` and `Register.cshtml`. It supplies the account route artwork, responsive context and form panels, antiforgery-enabled form, default validation summary, pending submit action and alternate-account slot. The views set `Title`, `AuthDescription`, `AuthAction` and `AuthSubmit`; their body contains strongly typed Razor fields with the shared `tb-field` helpers. `AlternateAccount` is a required Razor section; an optional `Feedback` section can replace the default summary. Login uses the controller’s existing `ReturnUrl` view data. The layout does not add any authentication or validation rules.

At the shared navigation breakpoint (56rem), the context panel sits beside the 27.5rem form. Below it, the panels stack and shrink from 21.875rem to fit the available width and safe-area gutters. Both use solid theme surfaces over the decorative artwork. No panel owns vertical scrolling: validation expands the form in the document. Keep the navigation clearance in the shared shell, not the form.

## Verification

With Playwright resolvable by Node and its Chromium browser installed (or `CHROME_PATH` set to an existing Chromium executable), run from the repository root in Git Bash:

```bash
AUTH_LAYOUT_BROWSER_SCRIPT="$(pwd -W)/tests/ui/auth-layout-check.cjs" \
SHARED_CONTROLS_BROWSER_SCRIPT="$(pwd -W)/tests/ui/shared-controls-check.cjs" \
TRACEBACK_BROWSER=1 dotnet test -c Release --nologo
```

Use `$(pwd)` instead of `$(pwd -W)` on platforms without Git Bash for Windows. Set `AUTH_SCREENSHOTS` to an absolute output directory for screenshots. The Chromium test covers both forms, both themes, 1440/896/895/390/320 CSS px, native 200% zoom and a shortened mobile viewport. The shortened viewport models lost vertical space, not a physical software keyboard. The server validation fixture uses the real form models and Razor views without a live account database. Account success, credential and duplicate-account journeys belong to the consuming pages’ follow-up checks.
