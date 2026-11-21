# Report an Item

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `Authorized GET and POST /Items/Create`

**Current view** — [Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml)

Render one continuous form. Use the desktop vertical tracker and the mobile horizontal tracker with one centered section label that updates in place. Use tracker nodes `36519` (218 × 236 desktop) and `36703` (350 × 48 mobile) as layout references. Keep accessible section headings/anchors but remove redundant visible headings. Preserve subtle dividers. Give each type tile icon the same foreground as its label, including the active state. For Lost use Lucide search-x. For Found use package-check. Use the shaded light input surface across the editor.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `29165` | Report / motive background / Laptop (`34906`, 1440 × 1040). Scrollable report form (`29166`, 1440 × 1040). Lost (`29171`, 354 × 48). Found (`29177`, 354 × 48) |
| `30468` | Report / motive background / Phone (`34994`, 390 × 844). Scrollable report form (`30469`, 390 × 708). Lost (`30474`, 169 × 48). Found (`30480`, 169 × 48) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Default | Authorized member opens Create | Default to Lost and current date. Keep Cancel to Dashboard. Place map instructions next to the location input. | Designed · `29165`, `30468`, `31723`, `33090` |
| Filled | User chooses type and fills the form | Retain inputs when switching sections. Require the selected coordinates for a new report and allow typed coordinates when the interactive map is unavailable. | Designed · `29328`, `30625`, `31894`, `33255` |
| Validation | Required data, date, lengths or pin is invalid | Apply server ModelState errors at the summary and field. Keep prior values. Tell user when a file must be chosen again. | Designed · `29491`, `30782`, `32065`, `33420` |
| Publishing | Valid form posts | Send multipart form with antiforgery. Guard duplicates. Successful POST redirects to owner Item Details with the existing notice. | Designed · `30132`, `31399`, `32738`, `34069` |
| Map error | The interactive map fails | Keep manual coordinate controls and retry in place. Do not drop a selected pin or typed values. | Designed · `29663`, `30948`, `32245`, `33594` |
| Photo selected | Supported file selected | Replace picker placeholder with preview and remove/replace controls. Keep the file client-side until submit. | Designed · `29965`, `31238`, `32563`, `33900` |
| Image error | File unsupported, too large, or storage fails | Explain error and let the member retry. Create currently attempts to remove the newly created report if storing the supplied image fails. | Designed · `29801`, `31080`, `32391`, `33734` |
| Camera | User requests digital capture | Open the camera overlay or platform fallback. Allow cancel, denial recovery, and capture review before attaching the final file. | Designed · `30295`, `31556`, `32909`, `34234` |
| Unavailable camera or scripts | Permission denied, camera unavailable or JavaScript disabled | Retain file input and manual coordinates. Resume without losing existing form values. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | The tracker jumps and camera overlay demonstrate behavior. Derive the active section from real scrolling and use actual camera/file input instead of the mock photograph. | Prototype simulation |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Buttons and submit actions](../components/button.md)
- [Camera capture overlay](../components/camera-overlay.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Footer](../components/footer.md)
- [Form fields](../components/form-field.md)
- [Lucide icon primitives](../components/icons.md)
- [Report image picker and editor photo](../components/image-picker.md)
- [Glass navigation and menus](../components/navigation.md)
- [Shared Create/Edit report editor](../components/report-form.md)
- [Map display and pin picker](../components/report-map.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)
- [Responsive report section tracker](../components/section-tracker.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Reuse the editor’s binding for title ≤120, description ≤2000, category ≤80, location ≤120, contact preference ≤120 and a non-future date. All those fields are required. Type is Lost or Found. Map follows server coordinate normalization.
2. Wire click and scroll tracker states to one set of section anchors. Prevent fixed navigation from covering focused controls or validation targets.
3. Verify multipart image input, manual coordinates with scripts or tiles unavailable, retry, pre-submit preview removal, and camera denial. Run MapLocationIntegrationTests and ItemImageEndpointTests.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **06 · Report an item**, node `36437`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs. Camera uses a separate [overlay component reference](../components/camera-overlay.md).

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `29165` | Report an item / 1440 / Light / Default | 1440 × 1040 |
| `30468` | Report an item / 390 / Light / Default | 390 × 844 |
| `31723` | Report an item / 1440 / Dark / Default | 1440 × 1040 |
| `33090` | Report an item / 390 / Dark / Default | 390 × 844 |
| `29328` | Report an item / 1440 / Light / Filled | 1440 × 1040 |
| `30625` | Report an item / 390 / Light / Filled | 390 × 844 |
| `31894` | Report an item / 1440 / Dark / Filled | 1440 × 1040 |
| `33255` | Report an item / 390 / Dark / Filled | 390 × 844 |
| `29491` | Report an item / 1440 / Light / Validation | 1440 × 1040 |
| `30782` | Report an item / 390 / Light / Validation | 390 × 844 |
| `32065` | Report an item / 1440 / Dark / Validation | 1440 × 1040 |
| `33420` | Report an item / 390 / Dark / Validation | 390 × 844 |
| `29663` | Report an item / 1440 / Light / Map error | 1440 × 1040 |
| `30948` | Report an item / 390 / Light / Map error | 390 × 844 |
| `32245` | Report an item / 1440 / Dark / Map error | 1440 × 1040 |
| `33594` | Report an item / 390 / Dark / Map error | 390 × 844 |
| `29801` | Report an item / 1440 / Light / Image error | 1440 × 1040 |
| `31080` | Report an item / 390 / Light / Image error | 390 × 844 |
| `32391` | Report an item / 1440 / Dark / Image error | 1440 × 1040 |
| `33734` | Report an item / 390 / Dark / Image error | 390 × 844 |
| `29965` | Report an item / 1440 / Light / Photo selected | 1440 × 1040 |
| `31238` | Report an item / 390 / Light / Photo selected | 390 × 844 |
| `32563` | Report an item / 1440 / Dark / Photo selected | 1440 × 1040 |
| `33900` | Report an item / 390 / Dark / Photo selected | 390 × 844 |
| `30132` | Report an item / 1440 / Light / Publishing | 1440 × 1040 |
| `31399` | Report an item / 390 / Light / Publishing | 390 × 844 |
| `32738` | Report an item / 1440 / Dark / Publishing | 1440 × 1040 |
| `34069` | Report an item / 390 / Dark / Publishing | 390 × 844 |
| `30295` | Report an item / 1440 / Light / Camera | 1440 × 1040 |
| `31556` | Report an item / 390 / Light / Camera | 390 × 844 |
| `32909` | Report an item / 1440 / Dark / Camera | 1440 × 1040 |
| `34234` | Report an item / 390 / Dark / Camera | 390 × 844 |
