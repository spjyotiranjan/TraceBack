# Something Went Wrong

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Home/Error`, also used by the production exception handler

**Current view** — [Shared/Error.cshtml](../../src/TraceBack/Views/Shared/Error.cshtml)

Use the shared error layout with the correct role-based shell and optional request reference. Keep decoration behind text and preserve readable focus/contrast. Never expose exception messages, stack traces, request payloads or secrets.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `45755` | Something went wrong / Background / Desktop / Light (`45756`, 1440 × 1040). Something went wrong / Recovery content / Desktop / Light (`45762`, 590 × 352). Navbar / Guest / Laptop / Light (`45775`, 1248 × 72) |
| `45862` | Something went wrong / Background / Mobile / Light (`45863`, 390 × 844). Something went wrong / Recovery content / Mobile / Light (`45869`, 350 × 386). Navbar / Guest / Mobile / Light (`45882`, 366 × 60) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Guest | Public page fails and no reference is rendered | Show the generic error and recovery. Retry only a safe GET, otherwise link to a stable GET or Home. | Designed · `45755`, `45862`, `45968`, `46091` |
| Member / Reference | Authenticated view supplies a request identifier | Render the reference as selectable text with the same safe navigation. Keep the mobile dock accessible during document scrolling. | Designed · `45788`, `45897`, `46009`, `46134` |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Empty, unavailable and recovery layouts](../components/empty-recovery.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants.

## Build steps and acceptance checks

1. Adapt the shared Error view and existing Home/Error wiring. Preserve production response behavior.
2. Verify both shells, with and without reference, long request IDs, a real GET failure and recovery after an unsafe request.


## FigCraft references

Section **12 · Something Went Wrong**, node `45731`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `45755` | Something went wrong / 1440 / Light / Guest | 1440 × 1040 |
| `45788` | Something went wrong / 1440 / Light / Member / Reference | 1440 × 1040 |
| `45862` | Something went wrong / 390 / Light / Guest | 390 × 844 |
| `45897` | Something went wrong / 390 / Light / Member / Reference | 390 × 844 |
| `45968` | Something went wrong / 1440 / Dark / Guest | 1440 × 1040 |
| `46009` | Something went wrong / 1440 / Dark / Member / Reference | 1440 × 1040 |
| `46091` | Something went wrong / 390 / Dark / Guest | 390 × 844 |
| `46134` | Something went wrong / 390 / Dark / Member / Reference | 390 × 844 |
