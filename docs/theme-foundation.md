# Campus Signal theme foundation

The shared Razor layout resolves `light`, `dark`, or `system` synchronously in the document head, before the stylesheets. `traceback.theme.v1` remains the storage key. System is the default and is the only choice that follows OS changes. Blocked storage preserves the current page’s choice in memory, and storage events synchronize other tabs. Without JavaScript, CSS follows the OS directly, the inactive appearance control stays hidden, and the mobile navigation links remain available.

## Consumption

- `theme.css` owns the public `--tb-*` palette. `--tb-divider` is for decorative separation; use `--tb-control-border` for meaningful input and control boundaries. Text inputs use `--tb-field` in both resting and focused states. Coral is `--tb-accent`; use `--tb-accent-ink` for small text on light tints and `--tb-on-accent` directly on coral. Statuses always retain text.
- `fonts.css` owns the eight `--tb-type-*` roles. It self-hosts the unmodified `web/InterVariable.woff2` from the upstream Inter 4.1 release (`rsms/inter`), with `wwwroot/fonts/Inter-OFL.txt` and a system fallback.
- Render a named decorative Lucide icon with `<partial name="_Icon" model='"map-pin"' />`. The sprite contains the 47 names from the design inventory, preserving `lucide-static@1.72.0` geometry. The upstream ISC and Feather MIT notices are in `wwwroot/icons/Lucide-LICENSE.txt`. Name icon-only links and buttons on the control, not on the SVG.
- `wwwroot/icons/traceback-mark.svg` reproduces the Reunion symbol (node `1037` in master `1036`). The shared Home link uses the mark in `currentColor` and is named “TraceBack Home.”

## Verification

`dotnet test` runs the existing server suite. `tests/ui/theme-check.cjs` checks the live Sign in page in Chromium at 1440 and 390 px, including the computed palette, field/focus and status contrast, native keyboard theme selection, stored choice, blocked storage, CSS-only System mode, startup without the deferred script, and local SVG/font rendering. Run it against a local instance with Playwright installed and available to Node (for example through `NODE_PATH`). Set `TRACEBACK_URL` if the instance is not on `http://127.0.0.1:5187`, `CHROME_PATH` to use an installed browser, and `THEME_SCREENSHOTS` to save the visual comparison captures.

The theme choices use native radios in the existing navigation. The separate glass-navigation work owns the final flyout/dock placement and layout; the existing page artwork is unchanged.
