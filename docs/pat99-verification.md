# PAT-99 map controls and bounded report-state verification

Verified in the user-supplied primary checkout `D:\SDE Projects\TraceBack`,
branch `PAT-87-modern-graphic-ui`, base `1300ed8e38693f8a79a64d4cc8ada75cd9e194d3`
plus the scoped `report-map.css` correction. `git rev-parse --show-toplevel`
and `git worktree list` confirmed the primary physical root. The listed obsolete
PAT-15 worktree was not used. Patrick granted exclusive primary checkout access
on PAT-99; it remains reserved until explicit release. Existing unrelated work
was absent. No vendored Leaflet file changed.

The app-owned zoom controls now have 44 by 44 CSS pixel bounds, 8px separation,
individual borders and an inset focus ring on their opaque surface. Target size
is a usability correction, not an assertion that the previous size failed WCAG.

## Local checks

- `dotnet build TraceBack.slnx -c Release --no-restore --nologo`: exit 0,
  zero warnings/errors.
- `dotnet test TraceBack.slnx -c Release --no-build --no-restore --nologo`:
  exit 0, 60 passed, zero failed/skipped.
- `dotnet format TraceBack.slnx --verify-no-changes --no-restore --verbosity quiet`
  and `git diff --check`: exit 0. No CSS formatter is configured; the correction
  follows the existing compact stylesheet convention.
- Direct behavior: headless Chrome, Release app launched from the primary root,
  Development profile, port 5217, explicit command-line
  `--Mongo:Database=TraceBack_PAT99_1794006275830`. Isolation was checked before
  fixture writes, including absence of the test account in the configured DB.

## Affected behavior

Create, edit and detail were captured in light/dark at 320, 768 and 1280 CSS px
(18 captures). Create/edit plus and minus targets measured 44 by 44, with the
nearest control 8px away. Synthesized Chromium touch activated plus by +1 zoom
and minus by -1, using tile URL zoom levels as observable evidence. Coordinate
inputs remained unchanged. Edit retained its fixed pin. Detail retains its
noninteractive preview with a pin and no zoom controls. Attribution remained
visible within each map, and page horizontal overflow was zero.

In both themes, keyboard Tab/Enter activated plus/minus, showed visible focus,
and allowed Tab exit. Map-required errors, server report summary/inline errors,
tile failure and successful Retry recovery were exercised. Prior audit evidence
for other flows remains applicable; no full audit was repeated. Representative
mobile/tablet/desktop captures, focus and summary captures were visually checked.
Tile responses used a deterministic transparent fixture; live provider imagery
availability was not tested.

## Rendered contrast

Ratios below use solid, directly adjacent screenshot pixels (1px apart), with
coordinates and RGB samples recorded in `results.json`. Outside Retry focus
includes an expanded crop; zoom focus is inset on the control surface.

| State | Light | Dark |
| --- | ---: | ---: |
| Zoom plus/minus focus | 6.58:1 | 8.99:1 |
| Map inline required error | 6.69:1 | 9.97:1 |
| Report summary error | 6.33:1 | 7.04:1 |
| Report summary link | 6.80:1 | 7.59:1 |
| Report inline error | 6.69:1 | 9.97:1 |
| Map tile error | 6.69:1 | 9.97:1 |
| Map Retry focus | 5.94:1 | 12.10:1 |

## Delayed/failed submit and cleanup

For each theme, a fully populated create form issued exactly one intercepted
POST. The probe observed disabled `Publishing…` button and matching live status
through a browser binding after submit handlers ran. It held the response for
at least 600ms and supplied HTTP 503 without reaching the server. Each probe
has an 8s response timeout, 4s POST timeout, 1.5s state callback timeout and an
automatic response release at 3s to prevent another hanging navigation.

The browser displayed the synthetic service-unavailable response. Back restored
the populated title and enabled Publish button in this Chrome run; fresh report
navigation also restored an enabled Publish button. No automatic retry occurred,
and item counts stayed unchanged. This demonstrates native form submission
loading and recovery for a synthetic HTTP failure, not a backend outage,
network-disconnect recovery, durable draft, audible announcement or idempotency
guarantee. No failed-submit retry write was attempted.

The final isolated database had zero pending image deletes and was dropped;
absence was independently checked from a separate process. Two abandoned PAT-99
databases from interrupted helpers were checked for the exact test account and
fixture, no image keys/URLs and zero pending image deletes, then removed and
their absence verified. No PAT-99 databases remain. App and browser stopped;
no product or live-data cleanup was performed.

Disposable helpers, startup log, 18 layout screenshots, state screenshots,
`results.json` and `interrupted-cleanup.json` are under the Paperclip internal
default `tmp/pat99-map` directory. Initial helper failures involved the BSON
fixture field spelling, focus crop, pending-navigation observation, and shell
encoding of an ellipsis; the final helper completed with no page errors.

The Impeccable launcher could not verify/download its engine; written guidance,
PRODUCT.md and rendered inspection were used. DESIGN.md and a canonical decision
record/pointer remain absent. Current task manual-merge direction governs over
older conflicting project-description merge text. No detector or whole-product
WCAG conformance claim is made. Physical touch, speech output and Narrator were
not tested; no desktop session was used. Grace must independently clear the new
SHA and Patrick must accept before this coding task is Done.
