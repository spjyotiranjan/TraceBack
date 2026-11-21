# Item Not Found

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Items/Details/{id} for an unknown or deleted report`

**Current view** — [Items/NotFound.cshtml](../../src/TraceBack/Views/Items/NotFound.cshtml)

Render the intended recovery illustration, headline and Browse action. Use the shared shell for the current role. The canvas shows member references. Guest shell reuse follows the public Details route.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `45462` | Item not found / Background / Desktop / Light (`45463`, 1440 × 1040). Item not found / Recovery content / Desktop / Light (`45470`, 590 × 328). Navbar / Member / Laptop / Light (`45480`, 1248 × 72) |
| `45531` | Item not found / Background / Mobile / Light (`45532`, 390 × 844). Item not found / Recovery content / Mobile / Light (`45539`, 350 × 348). Navbar / Member / Mobile / Light (`45549`, 366 × 60). Bottom navbar / Light / None (`45564`, 350 × 64) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Member | Details lookup returns no item | Maintain HTTP 404 and explain that the report is unavailable. Browse links back to the normal active results view. | Designed · `45462`, `45531`, `45597`, `45674` |
| Guest missing report | Anonymous visitor opens a missing report URL | Reuse the public shell with the same 404 status and Browse recovery. | Existing app / no dedicated frame |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Empty, unavailable and recovery layouts](../components/empty-recovery.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants.

## Build steps and acceptance checks

1. Use Items/NotFound.cshtml for the existing missing Details result. Avoid repurposing it as a site-wide catch-all.
2. Verify deleted, unknown and guest details links, correct HTTP status, Browse recovery and small-screen/zoom flow.


## FigCraft references

Section **11 · Item Not Found**, node `45440`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `45462` | Item not found / 1440 / Light / Member | 1440 × 1040 |
| `45531` | Item not found / 390 / Light / Member | 390 × 844 |
| `45597` | Item not found / 1440 / Dark / Member | 1440 × 1040 |
| `45674` | Item not found / 390 / Dark / Member | 390 × 844 |
