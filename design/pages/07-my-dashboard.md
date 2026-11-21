# My Dashboard

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `Authorized GET /Dashboard or /Dashboard/Index`

**Current view** — [Dashboard/Index.cshtml](../../src/TraceBack/Views/Dashboard/Index.cshtml)

Present My Reports and My Claims in the designed reading order. Use the responsive report cards and compact claim rows. Keep the Report an Item button on desktop and route mobile members to Create using the shared bottom dock. Keep live status/state text legible in both palettes.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `37211` | Dashboard / motive background / Laptop (`37212`, 1440 × 1040). Scrollable Dashboard content (`37218`, 1440 × 1040). Report an item (`37222`, 171 × 48). Report card / Laptop / Light / Blue water bottle (`37224`, 384 × 408) |
| `37803` | Dashboard / motive background / Phone (`37804`, 390 × 844). Scrollable Dashboard content (`37810`, 390 × 844). Report card / Phone / Light / Blue water bottle (`37816`, 350 × 408). Report card / Phone / Light / Black headphones (`37829`, 350 × 408) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Overview | Member has reports and claims | Render live data in both regions. Open report details by ID. Link claims to existing items only. | Designed · `37211`, `37803`, `38389`, `39029` |
| Reports only | Reports exist and claims are empty | Render populated reports and the claim empty state with Browse /Items?Type=Found. | Designed · `37316`, `37907`, `38502`, `39141` |
| Claims only | Claims exist and reports are empty | Show claims and the designed report empty state. Mobile report entry stays in the dock. | Designed · `37415`, `38005`, `38609`, `39247` |
| No activity | Both sets are empty | Show both purposeful empty states. Preserve Browse Found and access to Create. | Designed · `37497`, `38086`, `38699`, `39336` |
| Claim submitted | Claim POST has just succeeded | Show the one-time success notice and the submitted Pending claim under My Claims. | Designed · `37568`, `38156`, `38778`, `39414` |
| Report deleted | Owner has just deleted a report | Show the one-time deletion notice and refresh both regions from current data. | Designed · `37656`, `38243`, `38874`, `39509` |
| Claim target missing | Claim exists with no current item title or ID available for linking | Render the current missing-item wording without a dead link. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | Use authenticated live dashboard data and server-provided flash messages for the shown report and claim combinations. | Prototype simulation |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Dashboard claim row](../components/claim-row.md)
- [Empty, unavailable and recovery layouts](../components/empty-recovery.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Report card and image fallback](../components/report-card.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)
- [Report and claim status](../components/status-badge.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Bind Dashboard page data to the shared card and claim row. Render Pending, Approved and Rejected as text plus theme-specific treatment.
2. Keep missing report claim rows as unlinked text and preserve existing notices. Do not add claim management or an Admin screen.
3. Verify the report CTA at intermediate and mobile widths, both empty states, deleted report recovery, and long claim/report text.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **07 · My Dashboard**, node `37163`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `37211` | My dashboard / 1440 / Light / Overview | 1440 × 1040 |
| `37316` | My dashboard / 1440 / Light / Reports only | 1440 × 1040 |
| `37415` | My dashboard / 1440 / Light / Claims only | 1440 × 1040 |
| `37497` | My dashboard / 1440 / Light / No activity | 1440 × 1040 |
| `37568` | My dashboard / 1440 / Light / Claim submitted | 1440 × 1040 |
| `37656` | My dashboard / 1440 / Light / Report deleted | 1440 × 1040 |
| `37803` | My dashboard / 390 / Light / Overview | 390 × 844 |
| `37907` | My dashboard / 390 / Light / Reports only | 390 × 844 |
| `38005` | My dashboard / 390 / Light / Claims only | 390 × 844 |
| `38086` | My dashboard / 390 / Light / No activity | 390 × 844 |
| `38156` | My dashboard / 390 / Light / Claim submitted | 390 × 844 |
| `38243` | My dashboard / 390 / Light / Report deleted | 390 × 844 |
| `38389` | My dashboard / 1440 / Dark / Overview | 1440 × 1040 |
| `38502` | My dashboard / 1440 / Dark / Reports only | 1440 × 1040 |
| `38609` | My dashboard / 1440 / Dark / Claims only | 1440 × 1040 |
| `38699` | My dashboard / 1440 / Dark / No activity | 1440 × 1040 |
| `38778` | My dashboard / 1440 / Dark / Claim submitted | 1440 × 1040 |
| `38874` | My dashboard / 1440 / Dark / Report deleted | 1440 × 1040 |
| `39029` | My dashboard / 390 / Dark / Overview | 390 × 844 |
| `39141` | My dashboard / 390 / Dark / Reports only | 390 × 844 |
| `39247` | My dashboard / 390 / Dark / Claims only | 390 × 844 |
| `39336` | My dashboard / 390 / Dark / No activity | 390 × 844 |
| `39414` | My dashboard / 390 / Dark / Claim submitted | 390 × 844 |
| `39509` | My dashboard / 390 / Dark / Report deleted | 390 × 844 |
