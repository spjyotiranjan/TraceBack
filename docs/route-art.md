# Campus Signal route artwork

## Use in Razor

Set `ViewData["RouteArt"]` in the consuming view. The shared layout places one empty, `aria-hidden` element before the shell content. Its absolute, clipped layer sits behind all controls and does not participate in layout, focus or hit testing. Pages that do not set a variant do not load the artwork styles. Keep responsive app surfaces and text contrast independent of the art.

| Variant | Used by | Poses |
|---|---|---|
| `home-guest`, `home-member` | Home chooses by authentication state | A/B |
| `auth` | Sign in and Create account | A/B |
| `browse` | Browse reports | A/B |
| `details` | Item Details | A/B |
| `editor` | Create and Edit, including validation renders | A/B |
| `dashboard` | My Dashboard | A/B |
| `denied`, `not-found`, `error` | Recovery, including the generic HTML 404 | A only |
| `privacy` | Privacy | A only |

The route layer uses native phone composition below 600 px and desktop composition otherwise. Effective-theme tokens select light or dark, including the system preference with scripts disabled. CSS plays one 4.8-second A → B → A opacity pass when motion is allowed. Reduced motion uses phase A alone, without requesting B. The layer is suppressed in forced colors and print. The Home foreground illustration is a single responsive SVG with intrinsic `480 × 480` dimensions and empty alt text; it is separate from the route background.

## Source and redistribution check

- `design/TraceBack-Campus-Signal.figcraft` is the project-supplied handoff, first added by `eef8438`. `design/foundations.md#asset-register` and `design/components/route-backgrounds.md` explicitly identify the component sets for production SVG export. The Home, Recovery and Privacy PDFs corroborate the compositions. The export allowlist contains only those route components; their visible children are vector paths and ellipses with local fills/strokes. No images, fonts, embedded documents, external URLs, or third-party SVGs are copied from the archive. These are first-party project handoff exports for TraceBack; no independent redistribution license is asserted.
- **Native generated Home hero not shipped.** Nodes `1694` and `2072` both reference `8a36ab98c310e6af14039a37e6a1f1d269440c2a1344df58b4334669a5f2e0ef`. The archive identifies the image but contains no generator attribution, terms or asset-level rights record. The new `wwwroot/art/home-hero.svg` is original path-only artwork, not a crop or trace of that bitmap. No prototype photos (including the unassigned bitmap) are shipped. Confirm the generator/license before substituting an optimized WebP of the native artwork.
- Media fallbacks are a separate contract with UI 05 (`t_4964a8bf`): use the existing licensed Lucide `image` glyph for no upload (no image request) and `image-off` for a failed image URL. Preserve identical media geometry and distinct visible state text; neither is a substitute for a real report photo. License notice: `src/TraceBack/wwwroot/icons/Lucide-LICENSE.txt`.

## Regenerate and verify

Run `python scripts/export-route-art.py` (standard library only); `python scripts/export-route-art.py --check` verifies the SVGs and generated asset stylesheet. No export/build tool is needed by the app. Each page requests only its active device/theme artwork, and the generated configuration can reuse byte-identical poses. `tests/ui/route-art-check.cjs` runs against a local app (set `TRACEBACK_URL`, and `NODE_PATH`/`CHROME_PATH` as needed). It covers actual public routes and renders the private asset choices as CSS fixtures; MVC tests exercise signed-in Home, Browse, Details, Create, Edit and item-not-found with an isolated service fixture.

## Known differences and parity follow-up (UI 27)

- Compared against saved native data and FigCraft PDFs, not a live editor export. The transparent vector layers leave the page ground to the theme. Node transforms and gradient stops are rounded, inside ellipse strokes are inset, and the extra auth gradient blur is omitted. Only saved visible shapes are exported; hidden Home ribbon layers stay hidden. Check alignment, clipping, and blur against the live editor at reference and intermediate widths.
- The web moves between exported poses with a single crossfade rather than replaying FigCraft Smart Animate’s per-node alignment, ~6–7.2-second legs, and infinite looping. Backgrounds adapt to variable document height and cap width at 1800 px instead of stretching the compositions. Confirm the fit after navigation/mobile dock and final route layouts land.
- Exact Home foreground crop is intentionally not claimed. The native hero remains an export/licensing gap. Full protected-route browser screenshots (including Dashboard) belong to the integrated visual QA pass.
