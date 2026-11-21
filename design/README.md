# TraceBack Campus Signal implementation handoff

Use the [saved FigCraft file](TraceBack-Campus-Signal.figcraft) for visual behavior and the [MVC route setup](../src/TraceBack/Program.cs) and current controllers, models, Razor and scripts for data and permissions. Start with [foundations](foundations.md), then follow the page checklists. Admin screens are outside this handoff. Keep existing role checks in shared navigation and protected actions.

## Pages and flow ownership

| Page spec | Current route or entry | FigCraft section | Frames |
|---|---|---:|---:|
| [Home](pages/01-home.md) | `GET / or /Home/Index` | `36412` | 16 |
| [Sign in](pages/02-sign-in.md) | `GET and POST /Account/Login` | `36417` | 24 |
| [Create account](pages/03-create-account.md) | `GET and POST /Account/Register` | `36422` | 20 |
| [Browse reports](pages/04-browse.md) | `GET /Items or /Items/Index` | `36427` | 34 |
| [Item Details](pages/05-item-details.md) | `GET /Items/Details/{id} · POST /Items/Resolve/{id} and /Items/Delete/{id}` | `36432` | 80 |
| [Report an Item](pages/06-report-an-item.md) | `Authorized GET and POST /Items/Create` | `36437` | 32 |
| [My Dashboard](pages/07-my-dashboard.md) | `Authorized GET /Dashboard or /Dashboard/Index` | `37163` | 24 |
| [Submit a Claim](pages/08-submit-a-claim.md) | `Item Details enhancement · authorized GET and POST /Claims/Create?itemId={id}` | `39621` | 24 |
| [Edit Report](pages/09-edit-report.md) | `Authorized GET and POST /Items/Edit/{id}` | `40771` | 20 |
| [Access Denied](pages/10-access-denied.md) | `GET /Account/Denied` | `45141` | 4 |
| [Item Not Found](pages/11-item-not-found.md) | `GET /Items/Details/{id} for an unknown or deleted report` | `45440` | 4 |
| [Something Went Wrong](pages/12-something-went-wrong.md) | `GET /Home/Error · production exception handler` | `45731` | 8 |
| [Privacy](pages/13-privacy.md) | `GET /Home/Privacy` | `47836` | 8 |
| [Camera overlay](components/camera-overlay.md) | Inside Create and Edit | `47278` | 4 |

## Reusable component specs

- [Glass navigation and menus](components/navigation.md)
- [Theme switcher](components/theme-control.md)
- [Footer](components/footer.md)
- [Buttons and submit actions](components/button.md)
- [Form fields](components/form-field.md)
- [Validation, status and retry feedback](components/feedback.md)
- [Home hero and guidance](components/home-hero.md)
- [Home and Browse search input](components/home-search.md)
- [Authentication form and shell](components/auth-form.md)
- [Report card and image fallback](components/report-card.md)
- [Report and claim status](components/status-badge.md)
- [Browse filters and removable chips](components/filters.md)
- [Browse pagination](components/pagination.md)
- [Report summary and metadata](components/detail-summary.md)
- [Map display and pin picker](components/report-map.md)
- [Discussion and replies](components/discussion-thread.md)
- [Comment and reply composer](components/comment-composer.md)
- [Comment image preview and remove control](components/attachment-preview.md)
- [Shared Create/Edit report editor](components/report-form.md)
- [Responsive report section tracker](components/section-tracker.md)
- [Report image picker and editor photo](components/image-picker.md)
- [Camera capture overlay](components/camera-overlay.md)
- [Resolve and delete confirmation](components/confirmation-dialog.md)
- [Submit a Claim drawer and direct form](components/claim-drawer.md)
- [Dashboard claim row](components/claim-row.md)
- [Empty, unavailable and recovery layouts](components/empty-recovery.md)
- [Route-specific graphic backgrounds](components/route-backgrounds.md)
- [Lucide icon primitives](components/icons.md)
- [TraceBack brand mark](components/brand.md)
- [Privacy reading content](components/privacy.md)

## Build sequence

1. Apply theme tokens, Inter, logo, Lucide icons, glass navigation, footer and shared form/feedback primitives. Verify guest and member shells at both reference widths and make sure Home scrolls under the fixed navigation.
2. Implement Home, account screens and Browse on the existing route contracts. Exercise recent report states, account redirects, filtered and unfiltered empty results, pagination and retry.
3. Implement Details, map/discussion, permissions, and the claim drawer/direct form. Connect report cards to real IDs, retain report actions and test bounded comment scrolling and safe navigation.
4. Implement Create and Edit on the single report editor, then connect Dashboard and cover claims and reports empty/success states. Wire file selection, manual coordinates, camera availability, photo undo, and safe write retries.
5. Implement Access Denied, Item Not Found, Error and Privacy on the existing routes. Review all scenarios across layouts/themes and follow the browser and contract checks below.

## Source precedence and UI differences

- FigCraft defines visual hierarchy, geometry, artwork and the designed states. The current ASP.NET MVC app defines data, field rules, security, HTTP results and success redirects. Treat sample data, simulated POST outcomes, background phases and prototype jumps as implementation references. Determine whether state needs to persist in the browser, URL, or server from the underlying route behavior.
- Browse already defaults to Active when Status is absent. Dashboard keeps its desktop Report CTA and uses the member dock on mobile. Details applies the inline eight-line description expansion, pins media beside desktop detail and places the map below the mobile photo.
- Discussion currently renders Load more controls. Replace them with cursor loading at the end of the bounded thread, preserve threading and server capabilities, and show retry after failures. Reports and claims continue to use authorization and antiforgery on protected writes.
- The shared shell, extension of route-specific artwork, all field surfaces and responsive trackers are UI work. Existing fallbacks such as manual coordinates, direct claim route and server validation stay usable.

## Coverage and verification

- The native inventory includes **298** page/flow frames plus **4** camera overlays. The handoff records **530** native component or set records and maps every component referenced by page frames (**414** distinct master IDs) to a component spec. Embedded controls without a native master use their containing frame as a reference. Native structure was read from the saved archive. The live FigCraft connection was unavailable, so recheck clipping, blur, text wrapping and motion in the editor before accepting pixel-level parity.
- Shared library sections inspected in the native file
  - Shared foundations and navigation, node `36442`, 15 reference frames.
  - Home and account components, node `36443`, 4 reference frames.
  - Browse, details and report components, node `36444`, 4 reference frames.
- The saved prototype has 3,332 node destination references and 0 missing node targets in the structural check. Live click-through and dynamic layout still need editor/browser checks.
- Run `dotnet test tests/TraceBack.Tests/TraceBack.Tests.csproj` during implementation with the project’s supported test setup. Relevant coverage includes [ReportAuthorizationTests](../tests/TraceBack.Tests/ReportAuthorizationTests.cs), [MapLocationIntegrationTests](../tests/TraceBack.Tests/MapLocationIntegrationTests.cs), [ItemImageEndpointTests](../tests/TraceBack.Tests/ItemImageEndpointTests.cs), [CommentServiceTests](../tests/TraceBack.Tests/CommentServiceTests.cs) and [CommentImageIntegrationTests](../tests/TraceBack.Tests/CommentImageIntegrationTests.cs).
- Check key flows at 1440 and 390 px in Light and Dark, then between reference widths and at narrow or zoomed sizes. Verify keyboard and visible focus, touch and hover alternatives, contrast, reduced motion, fixed nav clearance, no page/column scroll traps, short/long copy and unavailable media/maps.
- Verify guest Browse → Details → sign in and return, member Create → Details → Edit → Dashboard, claim drawer → validation/duplicate/success → Dashboard, and comment load/reply/image/delete. Exercise stale/missing items, raw authorization/status handling, and Privacy and recovery links. Confirm retries preserve data and never automatically repeat unsafe writes.
