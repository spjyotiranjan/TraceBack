# Privacy

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Home/Privacy`

**Current view** — [Home/Privacy.cshtml](../../src/TraceBack/Views/Home/Privacy.cshtml)

Use the quiet boundary graphic and a restrained reading column. Keep the three existing paragraphs and their semantic order. Ensure long lines wrap appropriately, decor does not compete with copy, and the last paragraph and footer remain reachable beneath floating navigation.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `47857` | Privacy / boundary background / Desktop / Light (`47858`, 1440 × 1040). Scrollable privacy content (`47864`, 1440 × 936). Privacy / original policy copy / Desktop / Light (`47866`, 720 × 430). Navbar / Guest / Laptop / Light (`47876`, 1248 × 72) |
| `47957` | Privacy / boundary background / Mobile / Light (`47958`, 390 × 844). Scrollable privacy content (`47964`, 390 × 760). Privacy / original policy copy / Mobile / Light (`47966`, 350 × 542). Navbar / Guest / Mobile / Light (`47976`, 366 × 60) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Guest | Visitor follows Privacy from a public footer | Show public shell and policy content. Keep Home and account navigation available. | Designed · `47857`, `47957`, `48056`, `48172` |
| Member | Member follows Privacy from the shared footer | Show member shell with mobile dock where applicable and the same policy content. | Designed · `47889`, `47991`, `48096`, `48214` |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Footer](../components/footer.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Privacy reading content](../components/privacy.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants.

## Build steps and acceptance checks

1. Style Privacy.cshtml with the shared shell, reading-width tokens and saved Privacy artwork.
2. Verify direct GET and footer links across public and member pages, both themes, mobile scroll and keyboard access.


## FigCraft references

Section **13 · Privacy**, node `47836`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `47857` | Privacy / 1440 / Light / Guest | 1440 × 1040 |
| `47889` | Privacy / 1440 / Light / Member | 1440 × 1040 |
| `47957` | Privacy / 390 / Light / Guest | 390 × 844 |
| `47991` | Privacy / 390 / Light / Member | 390 × 844 |
| `48056` | Privacy / 1440 / Dark / Guest | 1440 × 1040 |
| `48096` | Privacy / 1440 / Dark / Member | 1440 × 1040 |
| `48172` | Privacy / 390 / Dark / Guest | 390 × 844 |
| `48214` | Privacy / 390 / Dark / Member | 390 × 844 |
