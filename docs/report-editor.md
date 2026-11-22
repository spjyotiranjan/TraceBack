# Shared report editor verification

Create and Edit render one multipart Razor form with the existing antiforgery token, ModelState errors, and named coordinate inputs. Create defaults to Lost. Edit posts the persisted Type through a readonly field, with server-side enforcement unchanged. The tracker uses real anchors; its active step indicates reading position, not completion. Manual coordinates and the labelled file chooser work without JavaScript.

The photo picker only stages changes. Undo restores the current image locally and a file discarded on server redisplay must be reselected. Camera review never binds a file until confirmation; denial leaves the ordinary chooser available. Successful Create redirects to Details. If saving the optional image fails, the editor offers a fresh submission only after the original report is gone; if the report remains, the member is directed to Details and Edit. The pending guard covers repeated submissions of the same enhanced form, not replay of a successful request from a different page or tab.

## Run locally

Use an isolated MongoDB instance and set `TRACEBACK_TEST_MONGO_URI` to its URI. Run:

```sh
dotnet test TraceBack.slnx -c Release --nologo
```

Install Playwright and Chromium (or set `CHROME_PATH` to a local Chrome executable; `NODE_PATH` may point to an isolated Playwright install). To include the browser runs on a disposable database, local Kestrel and test image storage:

```sh
export TRACEBACK_EDITOR_BROWSER=1
dotnet test TraceBack.slnx -c Release --nologo --filter 'FullyQualifiedName~MapLocation|FullyQualifiedName~ItemImage' --logger 'console;verbosity=detailed'
```

The browser runs exercise the real Create/Edit routes without scripts, multipart upload, guarded publish, stored photo and local Undo, permission denial, and camera review with controllable media tracks. Both themes are tested at 1440, 896, 895, 390 and 320 CSS px, plus native 200% Chromium zoom. Set `EDITOR_EVIDENCE` and `TRACEBACK_MAP_EVIDENCE` for screenshots. The fixture uses synthetic media and controlled tile responses; it is not a real R2, physical camera, native phone picker or assistive-technology smoke test. See [map verification](report-map-verification.md) for more about tile and asset recovery.
