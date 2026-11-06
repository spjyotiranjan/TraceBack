# PAT-92 discussion load guidance verification

Primary physical checkout: `D:\SDE Projects\TraceBack`. Branch:
`PAT-87-modern-graphic-ui`. Base: `d8ff3d33d7c34c1bd21b5f360fabbffa46dd5806`.
`git rev-parse --show-toplevel`, `git worktree list`, branch, HEAD and clean
status verified before editing. Only the primary checkout was used; the old
prunable PAT-15 entry was unused. Bret explicitly released checkout/app/browser
use on PAT-92 comment `7156f431-4b78-4885-ab42-c0253acba9b7`.

The tested product revision is the base plus this commit's discussion.js delta.
Load failures use fixed comments-load/connection guidance instead of exception
or server text. 401, 403 and redirects retain session-expiry guidance. Retry
retains the original list, parent and cursor. Posting/deletion are unchanged.

Local checks, 2026-11-06, all exit 0:

- `dotnet build TraceBack.slnx -c Release --no-restore --nologo`: 0 warnings/errors.
- `dotnet test TraceBack.slnx -c Release --no-build --no-restore --nologo`: 56
  passed, zero failed/skipped. Optional external-service test modes not enabled.
- `dotnet format TraceBack.slnx --verify-no-changes --no-restore --verbosity quiet`.
- `node --check src/TraceBack/wwwroot/js/discussion.js` and `git diff --check`.
- Chrome direct behavior script: aborted real signed-in Comments GET, readable
  fixed error and keyboard Enter on Retry recovering the actual empty response
  at 320/768/1280px in both themes; no horizontal overflow or page exceptions.
  HTTP 401/403 preserve session guidance; HTTP 500 suppresses raw server markup;
  all recover with Retry. Nine behavior cases plus database isolation passed.

The Release app launched from the primary checkout using actual root .env
configuration, Development profile, port 5207, and an explicit unique command-line
`--Mongo:Database=TraceBack_PAT92_<timestamp>` override. The isolated admin's
presence and absence from the configured database were checked before fixture
writes. The isolated database was removed after pendingImageDeletes=0; browser
and app closed. No configured-database fixture writes were made.

Disposable helper, startup log, results and twelve failure/recovery screenshots:
`C:\Users\S P Work_Station\.paperclip\instances\default\tmp\pat92`.
Mobile dark and desktop light renders were visually inspected: guidance and
Retry remain visible and wrap within the existing discussion layout. Existing
styles, motion and assets are unchanged; no runtime dependency or bundle added.
The first helper run selected both the hidden composer error and load error;
its selector was narrowed to li.discussion-error, then the full run passed.

Impeccable launcher failed cached-engine verification/download (curl unavailable).
Current written clarify/craft guidance, PRODUCT.md, docs/graphic-ui.md and
rendered inspection were used; no engine detector pass claimed. Project
metadata has no canonical Decision record pointer and no decision file was found.
No durable decision changed. Current assignment preserves user manual merge.
Grace current-SHA review and Patrick acceptance remain required after push.