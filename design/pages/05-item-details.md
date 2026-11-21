# Item Details

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Items/Details/{id}` · `POST /Items/Resolve/{id}` · `POST /Items/Delete/{id}`

**Current view** — [Items/Details.cshtml](../../src/TraceBack/Views/Items/Details.cshtml)

At desktop keep the photo/caption and map pinned on the left during the scrollable details column. Put public discussion after the main details. Avoid a scroll trap in nested regions, especially at zoom and short heights. On mobile use one document flow with the map immediately after image/caption and before report content. Wrap the full title, including long unbroken words. Clamp description to eight lines when needed and offer underlined bold inline Read more/Show less. Keep map coordinates collapsed by default. Render report data from the server, including the public contact preference. The light desktop media reference uses a 560 × 368 photo region (`12093`) and 560 × 184 map preview (`12119`). On the phone use the 350 × 270 photo region (`14585`) and inset 310 × 184 map preview (`14606`) as references.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `12080` | Details / motive background / Laptop (`35326`, 1440 × 1040). Scroll / Item details (`12081`, 1440 × 1040). Back to results (`12082`, 190 × 48). Report photo (`12093`, 560 × 368) |
| `14572` | Details / motive background / Phone (`35478`, 390 × 844). Scroll / Item details (`14573`, 390 × 844). Back to results (`14574`, 190 × 48). Type badge (`14580`, 68 × 29) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Found | View an active Found report | Show the eligible signed-in non-owner Claim entry. A guest can sign in and return to the item. Enforce the same server check at submit. | Designed · `12080`, `14572`, `17044`, `19696` |
| Lost | View a Lost report | Render the report details and discussion without the Claim action. | Designed · `12476`, `14965`, `17464`, `20113` |
| Owner | View own report | Render authorized management actions. Follow Edit to the report editor and resolve/delete via protected POST. | Designed · `12210`, `14701`, `17182`, `19833` |
| Claimed | Report status is Claimed | Expose status, read-only report controls as applicable and no new claim. | Designed · `12710`, `15197`, `17714`, `20361` |
| Resolved | Report status is Resolved | Expose status and remove actions reserved for active reports. | Designed · `12601`, `15089`, `17597`, `20245` |
| Confirm delete | Authorized user activates Delete | Show confirmation. Cancel restores focus. Confirming sends one protected POST, then dashboard for owner with status notice. Preserve existing role behavior. | Designed · `14431`, `16904`, `19547`, `22180` |
| Confirm resolve | Authorized user activates Resolve | Confirm, post, and return to Details showing the updated status. | Designed · `14290`, `16764`, `19398`, `22032` |
| Long title and collapsed description | Description exceeds eight lines | Show fully wrapped title. Expand or collapse body without navigating. Maintain toggle and state on the report. | Designed · `22851`, `23505`, `24194`, `24888` |
| No photo | Report has no image | Show designed placeholder with stable media bounds. | Designed · `12819`, `15305`, `17831`, `20477` |
| Photo error | Existing image fails | Show media recovery/fallback and leave the map and report available. | Designed · `12954`, `15439`, `17974`, `20619` |
| Coordinates | Activate the map’s coordinate disclosure | Expose saved coordinates with a semantic disclosure, leaving the rest of the page intact. | Designed · `13092`, `15576`, `18120`, `20764` |
| Legacy | Report has no saved coordinates | Keep the location text and the designed no-pin explanation. | Designed · `13434`, `15915`, `18486`, `21127` |
| Map loading | Map is initializing | Preserve reserved map space and announce loading. | Designed · `13223`, `15706`, `18259`, `20902` |
| Map error | Map or tiles fail | Offer map retry and preserve the saved text/coordinate information. | Designed · `13328`, `15810`, `18372`, `21014` |
| Discussion loading | First request for comments is pending | Show loading feedback in the bounded thread while leaving the report readable. | Designed · `13941`, `16418`, `19025`, `21662` |
| Discussion error | List or subsequent cursor request fails | Keep already loaded content. Retry the failed cursor in place without losing scroll position. | Designed · `14049`, `16525`, `19141`, `21777` |
| Reply | User selects Reply | Bind parent comment and retain a clear cancel action. Submit to the same item’s discussion endpoint. | Designed · `13538`, `16018`, `18598`, `21238` |
| Image attached | User selects a valid photo | Place preview and labeled alt text below the composer and expose an accessible Lucide trash button inside the preview. Reveal on photo hover/focus, show on touch, and show dark red fill plus shadow only on button hover/focus. | Designed · `13672`, `16151`, `18740`, `21379` |
| Posting | Post or reply request is pending | Guard duplicate submits. On success append the canonical response and clear the matching draft only. | Designed · `14159`, `16634`, `19259`, `21894` |
| Post error | Comment or attachment post fails | Retain draft, reply target and valid preview. Surface a recoverable accessible error. | Designed · `13809`, `16287`, `18885`, `21523` |
| Guest/read-only and empty discussion | Guest views the public thread or no comments are returned | Read existing comments without a posting composer. Show sign-in with a local return path and the empty hint when appropriate. | Existing app / no dedicated frame |
| Deleted parent and attachment errors | A parent was deleted with surviving replies or an attachment fails validation | Keep the tombstone and surviving children. Keep valid text and reply target and surface server-provided attachment errors. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | The canvas represents thread and description states with saved examples. Drive expansion, comment loading/posting and media from the actual DOM and endpoints. | Prototype simulation |

## Reusable components and assets

- [Comment image preview and remove control](../components/attachment-preview.md)
- [TraceBack brand mark](../components/brand.md)
- [Submit a Claim drawer and direct form](../components/claim-drawer.md)
- [Comment and reply composer](../components/comment-composer.md)
- [Resolve and delete confirmation](../components/confirmation-dialog.md)
- [Report summary and metadata](../components/detail-summary.md)
- [Discussion and replies](../components/discussion-thread.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Map display and pin picker](../components/report-map.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)
- [Report and claim status](../components/status-badge.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Adapt the existing Razor detail view and detail-report.css, keeping capabilities and status from the server.
2. Reuse Leaflet map scripts and existing comment list/mutation endpoints. Replace the current Load more buttons with cursor fetch at the end of the bounded thread. Guard repeat requests, stop at a null next cursor, and keep explicit retry for failed fetches.
3. Test guest read-only, no results, invalid attachment/alt, deleted parent with remaining replies, cursor failure, long headers, clamp overflow, and owner/visitor permissions. These additional cases follow the application and do not require duplicate mock frames.
4. Reuse ReportAuthorizationTests, MapLocationTests, MapLocationIntegrationTests, ItemImageEndpointTests, CommentServiceTests and CommentImageIntegrationTests while implementing.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **05 · Item details**, node `36432`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `12080` | Item details / 1440 / Light / Found | 1440 × 1040 |
| `14572` | Item details / 390 / Light / Found | 390 × 844 |
| `17044` | Item details / 1440 / Dark / Found | 1440 × 1040 |
| `19696` | Item details / 390 / Dark / Found | 390 × 844 |
| `12210` | Item details / 1440 / Light / Owner | 1440 × 1040 |
| `14701` | Item details / 390 / Light / Owner | 390 × 844 |
| `17182` | Item details / 1440 / Dark / Owner | 1440 × 1040 |
| `19833` | Item details / 390 / Dark / Owner | 390 × 844 |
| `12476` | Item details / 1440 / Light / Lost | 1440 × 1040 |
| `14965` | Item details / 390 / Light / Lost | 390 × 844 |
| `17464` | Item details / 1440 / Dark / Lost | 1440 × 1040 |
| `20113` | Item details / 390 / Dark / Lost | 390 × 844 |
| `12601` | Item details / 1440 / Light / Resolved | 1440 × 1040 |
| `15089` | Item details / 390 / Light / Resolved | 390 × 844 |
| `17597` | Item details / 1440 / Dark / Resolved | 1440 × 1040 |
| `20245` | Item details / 390 / Dark / Resolved | 390 × 844 |
| `12710` | Item details / 1440 / Light / Claimed | 1440 × 1040 |
| `15197` | Item details / 390 / Light / Claimed | 390 × 844 |
| `17714` | Item details / 1440 / Dark / Claimed | 1440 × 1040 |
| `20361` | Item details / 390 / Dark / Claimed | 390 × 844 |
| `12819` | Item details / 1440 / Light / No photo | 1440 × 1040 |
| `15305` | Item details / 390 / Light / No photo | 390 × 844 |
| `17831` | Item details / 1440 / Dark / No photo | 1440 × 1040 |
| `20477` | Item details / 390 / Dark / No photo | 390 × 844 |
| `12954` | Item details / 1440 / Light / Photo error | 1440 × 1040 |
| `15439` | Item details / 390 / Light / Photo error | 390 × 844 |
| `17974` | Item details / 1440 / Dark / Photo error | 1440 × 1040 |
| `20619` | Item details / 390 / Dark / Photo error | 390 × 844 |
| `13092` | Item details / 1440 / Light / Coordinates | 1440 × 1040 |
| `15576` | Item details / 390 / Light / Coordinates | 390 × 844 |
| `18120` | Item details / 1440 / Dark / Coordinates | 1440 × 1040 |
| `20764` | Item details / 390 / Dark / Coordinates | 390 × 844 |
| `13223` | Item details / 1440 / Light / Map loading | 1440 × 1040 |
| `15706` | Item details / 390 / Light / Map loading | 390 × 844 |
| `18259` | Item details / 1440 / Dark / Map loading | 1440 × 1040 |
| `20902` | Item details / 390 / Dark / Map loading | 390 × 844 |
| `13328` | Item details / 1440 / Light / Map error | 1440 × 1040 |
| `15810` | Item details / 390 / Light / Map error | 390 × 844 |
| `18372` | Item details / 1440 / Dark / Map error | 1440 × 1040 |
| `21014` | Item details / 390 / Dark / Map error | 390 × 844 |
| `13434` | Item details / 1440 / Light / Legacy | 1440 × 1040 |
| `15915` | Item details / 390 / Light / Legacy | 390 × 844 |
| `18486` | Item details / 1440 / Dark / Legacy | 1440 × 1040 |
| `21127` | Item details / 390 / Dark / Legacy | 390 × 844 |
| `13538` | Item details / 1440 / Light / Reply | 1440 × 1040 |
| `16018` | Item details / 390 / Light / Reply | 390 × 844 |
| `18598` | Item details / 1440 / Dark / Reply | 1440 × 1040 |
| `21238` | Item details / 390 / Dark / Reply | 390 × 844 |
| `13672` | Item details / 1440 / Light / Image attached | 1440 × 1040 |
| `16151` | Item details / 390 / Light / Image attached | 390 × 844 |
| `18740` | Item details / 1440 / Dark / Image attached | 1440 × 1040 |
| `21379` | Item details / 390 / Dark / Image attached | 390 × 844 |
| `13809` | Item details / 1440 / Light / Post error | 1440 × 1040 |
| `16287` | Item details / 390 / Light / Post error | 390 × 844 |
| `18885` | Item details / 1440 / Dark / Post error | 1440 × 1040 |
| `21523` | Item details / 390 / Dark / Post error | 390 × 844 |
| `13941` | Item details / 1440 / Light / Discussion loading | 1440 × 1040 |
| `16418` | Item details / 390 / Light / Discussion loading | 390 × 844 |
| `19025` | Item details / 1440 / Dark / Discussion loading | 1440 × 1040 |
| `21662` | Item details / 390 / Dark / Discussion loading | 390 × 844 |
| `14049` | Item details / 1440 / Light / Discussion error | 1440 × 1040 |
| `16525` | Item details / 390 / Light / Discussion error | 390 × 844 |
| `19141` | Item details / 1440 / Dark / Discussion error | 1440 × 1040 |
| `21777` | Item details / 390 / Dark / Discussion error | 390 × 844 |
| `14159` | Item details / 1440 / Light / Posting | 1440 × 1040 |
| `16634` | Item details / 390 / Light / Posting | 390 × 844 |
| `19259` | Item details / 1440 / Dark / Posting | 1440 × 1040 |
| `21894` | Item details / 390 / Dark / Posting | 390 × 844 |
| `14290` | Item details / 1440 / Light / Confirm resolve | 1440 × 1040 |
| `16764` | Item details / 390 / Light / Confirm resolve | 390 × 844 |
| `19398` | Item details / 1440 / Dark / Confirm resolve | 1440 × 1040 |
| `22032` | Item details / 390 / Dark / Confirm resolve | 390 × 844 |
| `14431` | Item details / 1440 / Light / Confirm delete | 1440 × 1040 |
| `16904` | Item details / 390 / Light / Confirm delete | 390 × 844 |
| `19547` | Item details / 1440 / Dark / Confirm delete | 1440 × 1040 |
| `22180` | Item details / 390 / Dark / Confirm delete | 390 × 844 |
| `22851` | Item details / 1440 / Light / Long title and collapsed description | 1440 × 1040 |
| `23505` | Item details / 390 / Light / Long title and collapsed description | 390 × 844 |
| `24194` | Item details / 1440 / Dark / Long title and collapsed description | 1440 × 1040 |
| `24888` | Item details / 390 / Dark / Long title and collapsed description | 390 × 844 |
