# Edit Report

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `Authorized GET and POST /Items/Edit/{id}`

**Current view** — [Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml)

Reuse Create layout, progress tracker, background, and controls. Prepopulate from the report, including any saved coordinates, and show Type locked with the current value still bound to the server. Preserve the same mobile section order and light form shading. Link into Edit from authorized Item Details and keep Cancel linked to Dashboard.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `40776` | Report / motive background / Laptop (`40777`, 1440 × 1040). Scrollable report form (`40788`, 1440 × 1040). Report field / Title / Laptop / Light (`40819`, 720 × 92). Report field / Category / Laptop / Light (`40823`, 720 × 92) |
| `41867` | Report / motive background / Phone (`41868`, 390 × 844). Scrollable report form (`41879`, 390 × 708). Report field / Title / Phone / Light (`41915`, 350 × 92). Report field / Category / Phone / Light (`41919`, 350 × 92) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Current photo | Authorized editor opens an active report | Show the existing image and choices to replace or remove. A legacy report can retain an absent pin under the existing rules. | Designed · `40776`, `41867`, `42918`, `44049` |
| Replacement selected | Editor chooses a valid replacement | Preview the pending file. Cancel/undo restores the current photo display. Keep replacement/removal state mutually exclusive. | Designed · `40993`, `42076`, `43143`, `44266` |
| Removal pending | Editor chooses Remove | Show pending removal and Undo. Commit removal only on save. | Designed · `41211`, `42286`, `43369`, `44484` |
| Image error | Replacement validation fails | Keep existing persisted photo as context and let user reselect. If text saved but image write fails, show the existing partial-success error on Details. | Designed · `41431`, `42498`, `43597`, `44704` |
| Saving | Editor posts changes | Guard duplicate save. Success returns to Details. Preserve 409 Conflict recovery and current missing, forbidden and inactive responses. | Designed · `41650`, `42709`, `43824`, `44923` |
| Other editor validation and concurrent update | Form invalid, item inactive or another update wins | Redisplay field feedback with the persisted photo as applicable. Preserve inactive and conflict responses from the controller. | Existing app / no dedicated frame |
| Partial image failure | Details save succeeds but the image write fails | Redirect to Details with the existing partial-success message and show the persisted photo state. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | Photo transitions in the canvas are demonstrations. Use existing media and a local pending file or removal state until edits are committed through the existing POST. | Prototype simulation |

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

1. Use one report editor, branching on edit mode and existing image only where behavior differs.
2. Bind id, locked type, form values, replacement file and removeImage to existing endpoint. Invalid input redisplays editor. Pre-commit Undo sends no mutation.
3. Exercise existing-photo, absent-photo, pending removal and undo, invalid replacement, storage failure, stale updates and legacy pin behavior. Reference ReportAuthorizationTests, MapLocationIntegrationTests and ItemImageEndpointTests.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **09 · Edit Report**, node `40771`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs. Camera uses a separate [overlay component reference](../components/camera-overlay.md).

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `40776` | Edit report / 1440 / Light / Current photo | 1440 × 1040 |
| `40993` | Edit report / 1440 / Light / Replacement selected | 1440 × 1040 |
| `41211` | Edit report / 1440 / Light / Removal pending | 1440 × 1040 |
| `41431` | Edit report / 1440 / Light / Image error | 1440 × 1040 |
| `41650` | Edit report / 1440 / Light / Saving | 1440 × 1040 |
| `41867` | Edit report / 390 / Light / Current photo | 390 × 844 |
| `42076` | Edit report / 390 / Light / Replacement selected | 390 × 844 |
| `42286` | Edit report / 390 / Light / Removal pending | 390 × 844 |
| `42498` | Edit report / 390 / Light / Image error | 390 × 844 |
| `42709` | Edit report / 390 / Light / Saving | 390 × 844 |
| `42918` | Edit report / 1440 / Dark / Current photo | 1440 × 1040 |
| `43143` | Edit report / 1440 / Dark / Replacement selected | 1440 × 1040 |
| `43369` | Edit report / 1440 / Dark / Removal pending | 1440 × 1040 |
| `43597` | Edit report / 1440 / Dark / Image error | 1440 × 1040 |
| `43824` | Edit report / 1440 / Dark / Saving | 1440 × 1040 |
| `44049` | Edit report / 390 / Dark / Current photo | 390 × 844 |
| `44266` | Edit report / 390 / Dark / Replacement selected | 390 × 844 |
| `44484` | Edit report / 390 / Dark / Removal pending | 390 × 844 |
| `44704` | Edit report / 390 / Dark / Image error | 390 × 844 |
| `44923` | Edit report / 390 / Dark / Saving | 390 × 844 |
