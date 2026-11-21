# Access Denied

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Account/Denied`

**Current view** — [Account/Denied.cshtml](../../src/TraceBack/Views/Account/Denied.cshtml)

Use the designed member shell and recovery illustration with the existing access message. Keep the action and footer in normal scroll flow on narrow or zoomed screens.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `45164` | Access denied / Background / Desktop / Light (`45165`, 1440 × 1040). Access denied / Recovery content / Desktop / Light (`45172`, 590 × 328). Navbar / Member / Laptop / Light (`45183`, 1248 × 72) |
| `45235` | Access denied / Background / Mobile / Light (`45236`, 390 × 844). Access denied / Recovery content / Mobile / Light (`45243`, 350 × 348). Navbar / Member / Mobile / Light (`45254`, 366 × 60). Bottom navbar / Light / None (`45269`, 350 × 64) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Member | ASP.NET cookie authorization routes a forbidden browser request to Denied | Render the message and Go Home link. Preserve the middleware behavior for different request types and the existing authorization policy. | Designed · `45164`, `45235`, `45303`, `45382` |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Empty, unavailable and recovery layouts](../components/empty-recovery.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants.

## Build steps and acceptance checks

1. Reuse the recovery layout on Denied.cshtml and retain the link to Home.
2. Verify restricted entry as a member, keyboard reachability, and direct navigation in the four designed combinations.


## FigCraft references

Section **10 · Access Denied**, node `45141`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `45164` | Access denied / 1440 / Light / Member | 1440 × 1040 |
| `45235` | Access denied / 390 / Light / Member | 390 × 844 |
| `45303` | Access denied / 1440 / Dark / Member | 1440 × 1040 |
| `45382` | Access denied / 390 / Dark / Member | 390 × 844 |
