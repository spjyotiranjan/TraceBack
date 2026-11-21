# Shared report search, cards, and badges (UI 05)

- `_ReportSearchField` renders the named `Query` input. The surrounding forms use native GET to `/Items`; Home supplies only `Query`. Browse submits the currently displayed Type, Status, Category and Location and deliberately omits `Page` so the server starts at 1. The named clear button edits only the input and returns focus to it. JavaScript is not needed to submit either form.
- `_ItemCard` takes a report and `ReportCardVariant`. Browse and Dashboard use an image-led grid; Home uses a compact horizontal card. The title is the single Details link. Browse reserves exactly two lines each for title and location; Details shows the full values. Status and type are visible text, with theme tokens providing supplementary color.
- Cards use `Item.DisplayImageUrl`, including the authenticated `/Items/Image/{id}` route for stored images and the existing legacy URL behavior. Both placeholders keep the 4:3 slot. A missing URL does not produce an image request. With JavaScript enabled, failed requests and decode errors show “Photo unavailable”; without JavaScript, supplied URLs use the browser’s native image/alt fallback. No handoff photos or sample records are included in the app.

## Verification

From the worktree root with .NET 10:

```sh
dotnet test TraceBack.slnx -c Release --nologo
```

The Chromium test is opt-in. With Node, Playwright available via `NODE_PATH`, and a Playwright Chromium binary (or `TRACEBACK_CHROMIUM_PATH` pointing at installed Chrome/Chromium):

```sh
TRACEBACK_BROWSER_TESTS=1 dotnet test TraceBack.slnx -c Release --nologo --logger 'console;verbosity=normal'
```

Optionally set `TRACEBACK_REPORT_EVIDENCE` to an ignored directory to save screenshots. The browser test starts the real application on loopback with test-only records and a Dashboard view model; it does not require a MongoDB or R2 account. It checks all three real Razor views in Chromium at 1440 and 390 px in both themes, opens reports by keyboard, verifies the full Details values, and checks photo success, absence, a 404, and a decode failure. It also checks badge labels and contrast, two-line geometry, filter retention/reset, clear without submission, and keyboard GET without JavaScript. The database-backed Dashboard workflow is not part of this component check.

UI 05 verification: the four new server cases failed before implementation and passed afterward. The Release suite with the Chromium matrix enabled reported 73 passed; live MongoDB/R2 opt-ins were not configured. Visual inspection of the screenshots confirmed the desktop grid, mobile stack, aligned text and distinguishable photo states in both themes. JavaScript syntax checks and Git whitespace checks passed.
