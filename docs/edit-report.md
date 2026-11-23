# Edit report

From an active report’s Details page, owners and admins can use **Edit report**. The shared multipart editor starts with the saved report, locked Type, pin (if present), and photo (if present). **Cancel** goes to Dashboard without writing. **Save changes** returns to Details. The manual coordinate and file inputs work without JavaScript; older reports can still be edited without adding a pin.

An Edit form carries the revision it loaded. If another save changes that revision, the POST returns 409 before any text or photo mutation. **Reload report** performs a fresh GET; it does not repeat the stale POST. The service also checks the revision when it reads the record for the conditional MongoDB update. Legacy form callers without the revision retain their existing POST contract. Authorization, antiforgery, and the API request shape are unchanged.

A replacement or removal is only committed on Save. Undo is local. An invalid upload redisplays the persisted photo and prompts the user to reselect the file before retrying. If the text save succeeds but the image write fails, Details shows the partial-success notice and the previously persisted image.

## Verification

Run `dotnet test TraceBack.slnx -c Release --nologo --logger 'console;verbosity=normal' -- xUnit.ParallelizeTestCollections=false` with a disposable `TRACEBACK_TEST_MONGO_URI`, Playwright/Chromium available to Node, and `TRACEBACK_EDITOR_BROWSER=1`. The shared scripts exercise the real multipart form, Map fallback, and camera flow; set `EDITOR_EVIDENCE` to save Chromium captures. The Edit matrix includes 1440×1040 and 390×844 in both themes, keyboard/touch, reduced motion, existing-photo and native file fallbacks, and a no-script two-tab 409/reload flow. Camera frames use controllable in-browser media tracks, not a physical device.
