# PAT-87 local verification

Date: 2026-11-03. Primary physical checkout: `D:\SDE Projects\TraceBack`.
Branch: `PAT-87-modern-graphic-ui`. Starting reviewed commit:
`02cf1fd33dd5122deafc1b6877b43ef881cc41e0`.
`git rev-parse --show-toplevel`, physical-path inspection and `git worktree list`
confirmed this primary checkout, plus an old prunable PAT-15 entry. Patrick's
2026-11-03 checkout-release handoff grants Wayne exclusive use.

The code under test is this commit's product tree. PR #21 merged while PAT-87
was being implemented; `origin/main` at `7592ea7f457c8568e1cbbc5eebf592df63c41429`
contains the reviewed starting commit and has an identical tree. PAT-87 targets
main. No branch update or integration changes were needed.

## Commands and results

- `dotnet build TraceBack.slnx -c Release --no-restore --nologo`: exit 0,
  zero warnings/errors, after the final Razor/map changes.
- `dotnet test TraceBack.slnx -c Release --no-build --no-restore --nologo`:
  exit 0, 51 passed, zero failed/skipped.
- `dotnet format TraceBack.slnx --verify-no-changes --no-restore --verbosity quiet`:
  exit 0.
- `node --check` for `theme.js` and `record-graphics.js`, and `git diff --check`:
  exit 0.
- Chrome scripts launched the Release app from the primary checkout with
  `--no-build --no-launch-profile`, Development configuration, port 5207 and
  explicit unique `--Mongo:Database=TraceBack_PAT87_<timestamp>` overrides.
  Isolated admin presence and its absence from the configured database were
  verified before browser writes. Every run removed only its isolated database
  after checking `pendingImageDeletes=0`. No live user records were modified.

## Direct behavior evidence

Guest home, login, registration, empty/populated browse, report, detail,
dashboard, admin, privacy and 404 were inspected at 320/375/768/1024/1280px in
both themes with reduced motion: one H1, no horizontal overflow. Claim screens
were checked at 320/768/1280px in both themes. Browse/report also passed 640px
reflow, representing the CSS viewport of a 1280px display at 200% browser zoom;
this is not an automated browser-toolbar zoom test.

Functional checks passed for theme keyboard activation, saved explicit choice,
device reset with focus return, OS changes, cross-tab changes, denied storage,
no-JS fallback, current-page one/two/three browse windows, filters, empty query,
pagination, result-heading focus and corresponding row/window focus highlight.

Draft tests covered approved field mirroring, contact exclusion, validated
selected image/removal, existing image/removal/Undo, camera-to-upload recovery,
server error-summary focus and section error markers. Keyboard pin selection,
tile failure recovery, publish, edit, legacy no-pin save and cookie-authenticated
discussion passed. Private claim validation/submission did not expose the claim
in report content. Admin Approve/Reject acceptance and cancellation used one
confirmation and changed database state only on acceptance.

The final detail recapture checked loaded map tiles, marker containment, complete
attribution containment and Leaflet pane display at all ten width/theme pairs.
Map width was 288–474.53px; the visible map remained usable in each case.

axe-core WCAG 2/2.1 A/AA scans found zero violations in browse and report main
content in both themes, plus claim and final detail main content. These scoped
automated scans supplement keyboard and visual checks; they are not a complete
assistive-technology audit.

Local browser PerformanceObserver samples for browse/report measured maximum
CLS below 0.02. Observed LCP was 60–232ms on the local machine. These are local
fixture measurements, not field Core Web Vitals or a production performance SLA.
Added graphic CSS is 2,714 bytes gzip; interaction JS 1,184 bytes gzip; theme JS
702 bytes gzip; shared aperture 249 bytes gzip. No new runtime dependency.

## Visual review and limits

The independent Impeccable finish reviewer found the inherited descendant
fact-row selector disrupting Leaflet. The fix scopes rows to direct children,
gives the location fact its full available width and exposes map attribution
links within a named region. After recapture, the reviewer scored this and
product-context persistence resolved (`ship` at the two-fix verdict-pass scope).

Impeccable context/detector engine execution was unavailable: cached engine
checksum verification failed and the launcher could not download without
`curl.exe`. Current skill references and independent rendered review were used;
no engine detector pass is claimed. Image fixtures verify dimensions and preview
lifecycle, not subjective photographic-content fidelity. No new raster art ships.

Disposable scripts, captures, axe results and browser logs are under
`C:\Users\S P Work_Station\.paperclip\instances\default\tmp\pat87-ui`:
`check.cjs`, `supplement.cjs`, `map.cjs`, `results.json`,
`supplement-results.json`, `map-results.json`, `axe-*.json`, and `metrics.jsonl`.
Formal current-commit review by Grace and Patrick's acceptance remain separate.
