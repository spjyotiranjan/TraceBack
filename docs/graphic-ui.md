# Living record UI (PAT-87)

The implementation follows Bret's accepted PAT-86 design brief, revision 4
(`ee18eb14-d035-49cb-a24b-913675c92fdf`). It extends the Campus index identity,
self-hosted Source Sans 3, and paired warm-day / blue-green-evening tokens.
These are task-oriented screens: report facts, photos, controls and recovery
actions lead; decorative geometry never represents a match, address or success.

## Direction and quality bar

THESIS: make each report a legible record, connecting its evidence to the next
human action. OWN-WORLD: extend the already accepted Campus index with external
record frames, asymmetric real-photo windows and linked section ordinals.
STORY: browse the current index, recognize an item, then report or claim through
the existing server-backed flows. FIRST VIEWPORT: browse has a compact result
board, filters and the beginning of real rows; report places the actual form
beside a clearly unpublished draft. FORM: opaque warm/deep surfaces, shared
3px horizontal rules, Source Sans 3 and paired semantic tokens.

This is an exact, accepted extension of an established world, not a new
concept-seed or comp-selection round. Quality bar: current-page photo provenance;
one/two/three truthful windows; unclipped long report facts; form dominance;
unambiguous draft state; a keyboard-operable effective-theme switch with a device
return path; static reduced-motion equivalence; and working controls/attribution
at 320/768/1280px in both themes. Automated dimensions and a tiny image fixture
verify media reservation and lifecycle, not subjective photographic fidelity.

## Shared components

- `_ThemeControl`: native Dark appearance switch reflects the effective theme.
  The existing head resolver prevents a wrong-theme first paint. Explicit choices
  persist under `traceback.theme.v1`; Use device appearance restores OS following.
  Denied storage retains the choice for the visit. Other-tab changes synchronize.
  Only user actions animate the thumb; OS changes and reduced motion are immediate.
- `_RecordAperture`: unfocusable decorative SVG, shared by empty media, account
  introductions and recovery screens. No third-party art, imagery or font is added.
- `_ResultBoard`: up to three reports from the current server-rendered result page,
  with real titles, types, photos or an explicit No photo fallback. The first window
  is eager; others are lazy. Ordinals connect windows to catalog rows on the same
  page. Keyboard focus and hover highlight their counterpart without moving focus.
  Filters, chips, links and pagination remain server-rendered GET navigation.
- `_DraftCanvas`: local type, title and reporter-entered location only. The
  decorative preview is hidden from assistive technology to avoid duplicate and
  per-keystroke announcements. Its section navigation remains accessible. It never
  mirrors contact preference, claims, map coordinates or inferred addresses.
  The canvas consumes the image editor's validated preview and uses its existing
  object URL lifecycle; it creates no additional URLs or persistence.

## Responsive behavior

At desktop widths the browse board is asymmetric and the draft canvas sits beside
the real form. Tablet reduces the board to two windows and moves the draft above
the form. Phones give the first catalog photo priority, compress the draft into a
masthead and retain section links. The image editor remains the full photo preview
on phones. Long source content wraps; captions may clamp with full detail links.
CSS geometry is reserved in the initial layout to avoid layout shifts.

Other screens share opaque record surfaces, image framing and semantic rules.
Dashboard counts come from its existing lists; photo ribbons use only those
reports. Claim/admin thumbnails show their associated report. Admin confirmation
ownership stays in `admin.js`; shared confirmation remains button-only.

## Scope and verification

No route, API, query, authentication rule, report validation, claim visibility,
map provider, image storage or database contract changes. Browser writes must use
a unique database supplied with the command-line `--Mongo:Database` override;
the root `.env` overwrites environment-only overrides.

Build/test/formatter/syntax and direct Chrome evidence are recorded on PAT-87
and its PR. Browser checks cover both themes, narrow/tablet/desktop layout,
keyboard and reduced motion, theme storage/OS/cross-tab states, browse variants,
draft preview, server validation, pin entry, publish/edit, discussion and admin.
The Impeccable context engine could not run: its cached engine failed checksum
verification and its launcher could not download without `curl.exe`. The current
skill references, accepted brief and rendered captures guide the manual fallback;
this is not a claim of an engine detector pass.
