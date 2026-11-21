# TraceBack foundations

[Handoff index](README.md) · [FigCraft source](TraceBack-Campus-Signal.figcraft)

Use the saved **Shared foundations and navigation** section (`36442`). Its Identity and colors frame is `2`, and the typography reference frames are `1634` (Light) and `1656` (Dark). Align the current [theme CSS](../src/TraceBack/wwwroot/css/theme.css) and [font declarations](../src/TraceBack/wwwroot/css/fonts.css) to these roles at implementation.

## Color

| Role | Light | Dark | Theme variable ID |
|---|---|---|---|
| Canvas | `#F2F4FF` | `#111326` | `VariableID:1` |
| Surface | `#FFFFFF` | `#1C203A` | `VariableID:2` |
| Text | `#151932` | `#F5F5FF` | `VariableID:3` |
| Muted | `#505A7B` | `#BCC4E0` | `VariableID:4` |
| Primary | `#3447D8` | `#AAB6FF` | `VariableID:5` |
| Accent | `#E76458` | `#FF9B8F` | `VariableID:6` |
| Border | `#CBD2EB` | `#515C7D` | `VariableID:7` |

Use the shaded light field surface `#EAEEFA` for text entry across pages. In dark mode use the corresponding field layer from the saved component variant. Maintain differentiated hover, focus, invalid, disabled, and selected states. In report type choices, use the label foreground for the icon too. White is used on the cobalt selected tile. Text and essential icons on coral, red, and status fills must pass contrast for their actual size and background.

## Typography

Inter is the saved type family for both themes. The project currently bundles Source Sans 3. Wire Inter in the implementation with its license and reasonable system fallback. Use the same type roles across themes and derive text color from theme roles.

| Role | Inter weight | Size / line height in px | FigCraft style ID |
|---|---|---:|---|
| Display | Bold | 56 / 64 | `S:1` |
| Heading 1 | Bold | 32 / 40 | `S:2` |
| Heading 2 | Semi Bold | 24 / 32 | `S:3` |
| Heading 3 | Semi Bold | 20 / 28 | `S:4` |
| Body | Regular | 16 / 24 | `S:5` |
| Label | Medium | 16 / 24 | `S:6` |
| Helper | Regular | 14 / 20 | `S:7` |
| Button | Semi Bold | 16 / 24 | `S:8` |

Use a single semantic page heading. Keep the full Item Details title readable at small widths. Clamp only the documented fields. Use muted foreground for support text and preserve strong contrast over both canvas and glass.

## Layout, interaction and motion

- Reference widths are 1440 and 390 px. The broad desktop content examples use a 1200 px rail and the phone content rail uses 350 px with 20 px side margins. Keep these as maximum or responsive references. Fluid intermediate widths may collapse multi-column layouts when content no longer fits.
- Desktop glass header is 1248 × 72 px at the reference and mobile header is 366 × 60 px. Signed-in mobile dock is 350 × 64 px, detached from the canvas with no backing strip. Reserve content clearance plus safe-area insets. Use the [navigation component](components/navigation.md) for behavior.
- Match the saved glass with tinted theme surface (about 72% opacity), blur around 18 px, a fine border and soft shadow. Use an opaque theme surface when backdrop filtering or transparency reduces legibility. Sample the saved layout’s specific radii instead of reusing the dock’s radius for fields and cards.
- Use accessible focus outlines independent of browser hover, 44 px targets where possible, labelled form controls and status announcements. Check normal text at 4.5:1 and meaningful boundaries/focus at 3:1, including over decorative artwork. Verify 200% zoom and narrow reflow.
- Respect System/Light/Dark and `prefers-reduced-motion`. Use fixed artwork poses and instant or short non-displacing transitions with reduced motion. Keep background art behind content, pointer-transparent and hidden from assistive technology.

## Asset register

| Graphic | Native source | Implementation use |
|---|---|---|
| Revised brand mark | Brand / revised vector mark `1036` | Export SVG for the shared Home link. See [brand spec](components/brand.md). |
| Lucide icons | Light `1110`, Dark `1372`, and expanded variants | Use original Lucide SVG paths and named glyphs. See [native icon inventory](components/icons.md). |
| Home | Home / belongings coming together sets `47325`, `47344`, `47363`, `47380`, `47397`, `47416`, `47435`, `47452` | Use the responsive guest/member vector composition and its phase variants. |
| Sign in and Create account | Account / animated vector background library `5293` | Use the blended gradient/vector background and keep the form contrast surface. |
| Browse | Browse / motive background sets `34582`, `34591`, `34600`, `34609` | Use the search/discovery motif. |
| Item Details | Details / motive background sets `35266`, `35283`, `35300`, `35317` | Use the report and connection motif beneath the media and detail layout. |
| Report and Edit | Report / motive background sets `34618`, `34630`, `34642`, `34654` | Use one editor decoration for both Create and Edit. |
| Dashboard | Dashboard / motive background sets `37117`, `37130`, `37143`, `37156` | Use activity-specific background behind Reports and Claims. |
| Recovery views | Background component variants for Access denied, Item not found and Something went wrong | Use the recovery page library variants and preserve action legibility. |
| Privacy | Privacy boundary components `47842`, `47942`, `48041`, `48157` | Use the quiet boundary vectors behind reading content. |
| Home hero art | Original generated hero artwork `1694`, mobile crop `2072` | Use the native hero bitmap with responsive crop. Prefer optimized WebP and keep reserved dimensions. |
| Missing and failed photos | Browse component library `7012`, Details section `36432`, Claim drawer `39621` | Use the matching missing-photo or error vector state. Reuse the same media aspect ratio. |
| Map and camera previews | Map/discussion component library `11163`, camera overlay section `47278` | Use as visual fixtures. Real maps consume stored coordinates and real camera capture comes from browser controls. |

Export vector backgrounds and placeholder illustrations as SVG. Use conditional theme and viewport variants only when they differ. Use production photo URLs for actual reports and discussion attachments. Confirm exportability and license/provenance before distributing any illustrative assets. The native document provides placement and file identity.

### Embedded bitmap inventory

| Archive image hash | Dimensions | Sample saved usage | Production handling |
|---|---:|---|---|
| `193727c1ea2703ab0f45e0f18e771a5cd8ed0840e4610461d2edd7a678a6b40a` | 1254 × 1254 | Illustrative report photograph (`2074`) | Prototype sample only |
| `21c45f24473c663e3094bfc2b2bb7612c66110d1ed88e3cf6376e72dd15621fb` | 1254 × 1254 | Indexed asset with no matching visible node fill in the saved inventory | Confirm editor usage before any export |
| `3625ce4ae7346843d6a346e5d67cdd8838b1cd6b4da6cb8b2204c52e7480e586` | 627 × 627 | Coral umbrella photograph (`7054`) | Prototype sample only |
| `770653df01001b4eedb7c5422a0233799adf6c6feba3a37d730e0fa04f8965c9` | 627 × 627 | Blue water bottle photograph (`7015`) | Prototype sample only |
| `8a36ab98c310e6af14039a37e6a1f1d269440c2a1344df58b4334669a5f2e0ef` | 1254 × 1254 | Original generated hero artwork (`1694`) | Home hero artwork |
| `96525abb450660f38a8be506a67b7d4b77beba20b67bdd8ca0880d5205b67364` | 627 × 627 | Silver key ring photograph (`7041`) | Prototype sample only |
| `e967cb5fd630f9e8683b87b4c8ff0be3740bf213e728c8240ebd14acfc101635` | 627 × 627 | Black headphones photograph (`7028`) | Prototype sample only |
