# Shared navigation shell

The same Razor layout renders for guests, members and admins. Guest Browse uses the existing authenticated `/Items` endpoint and its normal sign-in challenge; it does not make report data public. The authenticated desktop row and mobile dock share one destination partial. `aria-current="page"` marks exact destinations and `location` marks the parent section on detail, claim and edit routes. The responsive account controls share one server-rendered partial with role checks and antiforgery logout forms.

Appearance and the mobile account menu are native disclosures. Tab reaches their contents; Escape closes and restores trigger focus. Outside activation closes them without overriding focus on the control the visitor clicked. Account links and logout remain reachable without script, while the theme follows the OS default.

`shell.css` reserves document and scroll clearance with `--tb-header-clearance` and `--tb-footer-clearance`. Sticky page controls should use these rather than hard-coded offsets. Keep overlays above `--tb-z-header` and `--tb-z-dock`; Bootstrap offcanvas and native modal dialogs already layer above both. Keep any route artwork outside the shell, below its stacking levels and pointer-transparent. The footer and Privacy link stay in document flow.

## Running the checks

`dotnet test -c Release --nologo` runs the normal suite. With Node and Playwright available, opt in to the real Chromium checks:

```bash
TRACEBACK_BROWSER=1 dotnet test -c Release --nologo --logger 'console;verbosity=detailed'
```

Set `NODE_PATH` if Playwright was installed outside this checkout. Install Playwright's full Chromium browser, or set `CHROME_PATH` to a local Chrome executable. Set `SHELL_SCREENSHOTS` to an existing directory to collect screenshots. Tests start a disposable loopback Kestrel instance and use test-only cookies protected by ephemeral keys; they do not require a database or real accounts.

The browser suite exercises light and dark at 1440, 1024, 896, 895 and 390 CSS px. It also checks native 200% page zoom at 1440 and 640 px (720 and 320 CSS px), emulated cutout insets, reduced-transparency fallback, native keyboard and no-script interactions, and a Bootstrap offcanvas probe above the header and dock. The dedicated claim drawer is implemented separately.
