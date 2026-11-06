# PAT-90 report calendar-date verification

Verified on 2026-11-06 in the supplied primary checkout `D:\SDE Projects\TraceBack`,
branch `PAT-87-modern-graphic-ui`, starting from
`e5275ae44b39fa395e9cfccd88f873c5dbfa7c37`. `git rev-parse --show-toplevel`
resolved to that path. `git worktree list` identified it as the active checkout;
the old PAT-15 entry was prunable and unused. The checkout was initially clean.
Patrick granted Kimball exclusive edits/build/browser use for PAT-90; Bret's
PAT-80 audit was notified to remain read-only. This correction extends PR #22.

## Scope and behavior

The report form previously defaulted to `DateTime.Today` while MVC and API
validation rejected days after `DateTime.UtcNow.Date`. With UTC at 20:00 on
November 5, India already has November 6, so the untouched local default failed.
Defaults and both validation paths now use the UTC calendar. A `TimeProvider`
allows deterministic boundary verification without changing the system clock.
Submitted dates retain their selected calendar components and are explicitly
stored as UTC midnight, avoiding MongoDB converting unspecified local midnight
into the previous UTC date. Existing stored records are unchanged; no migration,
timezone feature, UI asset, markup or styling change is included.

## Local checks before commit

All commands ran with the primary checkout as working directory, against the
uncommitted correction based on the starting SHA above:

- `dotnet build TraceBack.slnx -c Release --no-restore`: exit 0, no warnings/errors.
- `dotnet test TraceBack.slnx -c Release --no-restore`: exit 0, 56 passed, 0 failed.
  `TRACEBACK_TEST_MONGO_URI` was supplied privately from local configuration, so
  the database integration tests actually executed, including the new cases.
- `dotnet format TraceBack.slnx --verify-no-changes --no-restore`: exit 0.
- `git diff --check`: exit 0.

The five added test cases cover the parameterless UTC default, fixed-clock
defaults/validation with local calendars ahead and behind UTC, and two real
MongoDB MVC/API round trips. Boundary instants are `2026-11-05T20:00:00Z` at
UTC+05:30 and `2026-11-06T01:00:00Z` at UTC-08:00. Integration checks exercise
rendered defaults, MVC/API create, future rejection on create and edit, persisted
UTC midnight, MVC edit/reload and date-only JSON update/reload. Each case uses a
unique `tb_pat90_` database and removes it in `finally`.

## Direct browser behavior

Command from the same primary checkout:

```powershell
node 'C:/Users/S P Work_Station/.paperclip/instances/default/tmp/pat90/check.cjs'
```

Exit 0. Chrome ran the actual Release app using local `.env` dependencies with
an explicit command-line `--Mongo:Database=tb_pat90_browser_1793934504249` override.
The helper verified an isolated bootstrap account existed in that exact database
before browser writes. In Asia/Kolkata and America/Los_Angeles browser contexts,
each untouched UTC date published successfully, persisted at UTC midnight and
reappeared unchanged in the editor. Changing the selected date to 2020-01-02
survived save/reload; a future edit displayed the validation error and left the
stored date unchanged. These browser checks use real current time; fixed-clock
integration tests above establish the exact midnight boundary behavior.

Two fixture reports existed only in the isolated database. No image uploads
were made; pending image deletes were zero. The app stopped and that database
was dropped, with absence verified. An earlier helper run completed the same
flows but hit a Windows process-cleanup command lookup error; its two owned
fixtures were verified and its database removed before the successful rerun.
Logs, screenshots, helper and `results.json` remain in the Paperclip internal
`tmp/pat90` directory, outside product files. No real-user data was removed.

Fresh Grace clearance and Patrick acceptance must cover the resulting pushed
SHA; earlier PAT-87 clearance applies only to its historical revision. Patrick
owns audit release and parent closeout. The user retains manual merge.
