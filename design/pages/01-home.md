# Home

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET / or /Home/Index`

**Current view** — [Home/Index.cshtml](../../src/TraceBack/Views/Home/Index.cshtml)

Guest and signed-in visitors share the public landing route. Render the appropriate navigation and hero actions from the authenticated principal. On desktop keep the designed split hero. At 390 px center the headline, then show the illustration, supporting copy, and actions/search. Keep the top bar and signed-in dock fixed while the page scrolls. Reserve top and bottom clearance so the search and last footer links are reachable.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `1695` | Home / belongings coming together / Guest / Desktop (`47460`, 1440 × 1380). Scrollable Home content (`47816`, 1440 × 936). Home / Sign in / Light (`1722`, 136 × 48). Home / Create account / Light (`1724`, 182 × 48) |
| `1786` | Home / belongings coming together / Guest / Mobile (`47469`, 390 × 1920). Scrollable Home content (`47818`, 390 × 760). Home / Sign in / Light (`1803`, 350 × 48). Home / Create account / Light (`1805`, 350 × 48) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| guest | Anonymous visitor opens Home | Show guest hero and its sign-in and account creation links. Keep those routes reachable from the top menu. | Designed · `1695`, `1786`, `1882`, `1989` |
| member | Signed-in visitor and recent reports load | Render the member hero and recent report fixtures from real data. Open a card at /Items/Details/{id}, submit search to GET /Items with Query, and link to Create from member navigation. | Designed · `2079`, `2113`, `2154`, `2188` |
| recent empty | Recent result set is empty | Replace the list with the designed empty message and keep Browse available. The application shows up to six recent reports when the list is populated. | Designed · `2310`, `2244`, `2446`, `2380` |
| recent error | Recent reports cannot load | Keep hero and navigation usable. Retry the Home GET and preserve independent Browse access. Keep the existing HTTP 503. | Designed · `2345`, `2277`, `2481`, `2413` |
| Search query with no value | Member submits a blank Home search | Navigate to Browse with the Active default and no narrowing filter. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | Prototype report instances and search examples use fixture data. Populate cards from the server and use real GET navigation. | Prototype simulation |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Buttons and submit actions](../components/button.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Footer](../components/footer.md)
- [Home hero and guidance](../components/home-hero.md)
- [Home and Browse search input](../components/home-search.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Report card and image fallback](../components/report-card.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Adapt Index.cshtml plus home-browse.css and reuse the shared ItemCard and glass shell.
2. Implement the belongings-coming-together vector background and its reduced-motion pose. Reserve hero image space before it loads.
3. Test guest/member entry, empty and failure independently, and scroll through the full mobile Home in browser preview.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **01 · Home**, node `36412`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `1695` | Home / guest / 1440 / Light | 1440 × 1040 |
| `1786` | Home / guest / 390 / Light | 390 × 844 |
| `1882` | Home / guest / 1440 / Dark | 1440 × 1040 |
| `1989` | Home / guest / 390 / Dark | 390 × 844 |
| `2079` | Home / member / 1440 / Light | 1440 × 1040 |
| `2113` | Home / member / 390 / Light | 390 × 844 |
| `2154` | Home / member / 1440 / Dark | 1440 × 1040 |
| `2188` | Home / member / 390 / Dark | 390 × 844 |
| `2345` | Home / member / 1440 / Light / recent error | 1440 × 1040 |
| `2277` | Home / member / 390 / Light / recent error | 390 × 844 |
| `2481` | Home / member / 1440 / Dark / recent error | 1440 × 1040 |
| `2413` | Home / member / 390 / Dark / recent error | 390 × 844 |
| `2310` | Home / member / 1440 / Light / recent empty | 1440 × 1040 |
| `2244` | Home / member / 390 / Light / recent empty | 390 × 844 |
| `2446` | Home / member / 1440 / Dark / recent empty | 1440 × 1040 |
| `2380` | Home / member / 390 / Dark / recent empty | 390 × 844 |
