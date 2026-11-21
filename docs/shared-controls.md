# Shared controls and recovery

Components use Campus Signal tokens. Use the Razor partials in `Views/Shared`; keep route wording, auth and HTTP status decisions with the consuming page/controller. `shared-controls.css` and `shared-controls.js` are loaded by `_Layout`.

## Actions and pending forms

```cshtml
<partial name="_Button" model='@(new UiButton("Browse reports", Href: Url.Action("Index", "Items"), Icon: "search"))' />
<partial name="_Button" model='@(new UiButton("Save", Submit: true, PendingText: "Saving…"))' />
<partial name="_PendingStatus" />
```

- `ButtonTone` is `Primary`, `Secondary` (outline), `Quiet` or `Destructive`. Use local `Href` for navigation and `Submit: true` for native form submission; an action without a destination defaults to `type="button"`. For page-owned JS actions that need `data-*` controls, keep a native button and use `.btn.tb-button.tb-button--quiet` (or the appropriate variant). For icon-only buttons pass an explicit label and named icon: the label is accessible and also used as the title. Prefer visible labels for unfamiliar actions. Disabled links render without a destination and are not in the tab order.
- Use one `_PendingStatus` per form, by its action row. Pending is opt-in for same-window **POST** navigation and starts only after client validation allows submission. It preserves the button dimensions, submitted fields, submitter name/value and antiforgery token. The visible live region announces the change. JavaScript guards repeat clicks/submits and clears state on a restored page; it never sends a request by itself. AJAX forms manage their own status, unlock and error handling. Do not call `form.submit()` or treat the client lock as server idempotency.

## Fields and validation

```cshtml
<partial name="_ValidationSummary" />
<div class="tb-field">
    <label asp-for="Title" class="form-label">Title <span class="required">required</span></label>
    <input asp-for="Title" class="form-control" tb-field tb-hint="report-title-hint" tb-error="report-title-validation" />
    <p id="report-title-hint" class="tb-field__hint">Use a description people can search for.</p>
    <span id="report-title-validation" asp-validation-for="Title" class="tb-field__error"></span>
</div>
```

- `tb-field` on an `asp-for` input/textarea/select adds hints and errors to any existing `aria-describedby` and marks server-invalid fields in Razor. Use `.form-select` for native selects, `.form-control` for text/email/password/date/textarea, and `.tb-field__control` to wrap input with a decorative `_Icon` or separately labelled action. Give validation containers an ID distinct from jQuery Validate’s `<input-id>-error` element.
- Group related radio/checkbox choices with `<fieldset class="tb-field">` and a visible `legend`; add associated hints and errors with `aria-describedby`. Use `.tb-field__choice` on each associated/wrapping label. Mark required/optional visibly where it matters, with model validation as the source of truth. Use native `disabled` when unavailable and `readonly` where supported when the value must still submit. Use a static `.tb-field__locked` value and visible explanation for choices that cannot be edited; enforce immutable values on the server.
- Retain MVC ModelState and `asp-validation-for`. Put one `_ValidationSummary` in each form before its fields and include `_ValidationScriptsPartial` for unobtrusive client validation when appropriate. The summary focuses on submitted errors; selecting an error moves focus to its field. With custom field IDs, pass a `Dictionary<string, string?>` keyed by **full ModelState key** to `_ValidationSummary`; an explicit null renders text rather than a link. Use unique element IDs across forms.

## Status, empty and recovery

- `_TempDataNotices` already consumes `TempData["Success"]` and `TempData["Error"]` in the layout. Use `_Notice` (`UiNotice`) for inline info/success/error and an optional action. Supply text that makes the result or next step explicit. Keep inline errors in their affected region.
- `_RecoveryContent` (`RecoveryContent`) takes the caller’s heading, help, art (`Search`, `Reports`, `Claims`, `Access`, `Service`), actions and optional request reference. `FullPage: true` renders the page’s `h1`; the inline default renders `h2`. Art is decorative; route backgrounds belong to page cards.
- A retry is an ordinary local GET link. For a failed GET, generate it from the server-known original path and query (including filters/page), not a redirect parameter, referrer or `location.reload()`. Do not render a retry for failed writes; offer a safe GET destination instead. The shared error view preserves the middleware response status and only offers retry for a genuine original GET. Rendering a partial never changes the response status.

## Checks

`dotnet test -c Release --nologo` runs the HTTP integration tests. To opt in to the browser checks, have Playwright resolvable by Node (`NODE_PATH` is supported); set `CHROME_PATH` to an existing Chromium executable if not using Playwright's bundled browser. In Git Bash from the repo root:

```sh
SHARED_CONTROLS_BROWSER_SCRIPT="$(pwd -W)/tests/ui/shared-controls-check.cjs" \
  dotnet test -c Release --nologo --filter FullyQualifiedName~SharedControls
```

Use `$(pwd)` instead of `$(pwd -W)` on Linux/macOS. `CONTROL_SCREENSHOTS` can point to an existing local output directory for screenshots. The suite uses a loopback MVC test server with no account database: it exercises the production Razor and static assets; the pending POST is intercepted before reaching the database. It checks keyboard interaction, live region markup, sizing, reflow and contrast at 1440, 390 and 320px in both themes and verifies JavaScript-free GET retry and notices.
