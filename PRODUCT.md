# TraceBack product context

TraceBack helps campus users report lost or found belongings, browse reports,
share useful public discussion, and submit private ownership claims. Signed-in
users manage their reports and claims; admins decide ownership requests.

The primary product checkout is `D:\SDE Projects\TraceBack`. The application is
ASP.NET Core MVC with Razor views, self-hosted assets and MongoDB. Existing
Paperclip tasks own project delivery; TraceBack does not use Jira. GitHub owns
the code and PRs, Grace reviews the current revision, Patrick accepts delivery,
and the user owns merge. Tests that write data use an explicit, isolated database.

Report photos and reporter-supplied facts are evidence, never decoration to be
invented. Preserve authentication, validation, claim privacy and accessible
keyboard paths. A map pin is a fixed validated coordinate pair. Reporter-written
location is distinct from a verified address; reverse geocoding is disabled and
the honest fallback is address unavailable. Keep Leaflet/OSM attribution visible.

The established visual world is Campus index: self-hosted Source Sans 3,
warm-day light and deep-blue-green dark palettes, semantic `--tb-*` tokens,
compact scannable report rows, opaque surfaces and relevant record geometry.
Task completion leads on operational screens. Real photos stay unfiltered and
prominent. Use reduced motion, visible keyboard focus, mobile reflow and clear
loading/error/empty/validation states. Graphics cannot imply automated matching,
an address, a saved draft, a successful write or a completed claim.

The current accepted PAT-86 brief extends this world with a compact sun/moon
switch, current-result browse board and local draft canvas. See
[graphic UI](docs/graphic-ui.md) and [theme foundation](docs/theme-foundation.md).
Product behavior and the accepted brief take precedence over generic styling
preferences. No new theme, raster asset service, animation library or font is
needed for this extension.
