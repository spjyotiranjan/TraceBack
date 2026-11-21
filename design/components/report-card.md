# Report card and image fallback

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Input report ID, title, status/type, date/location and optional photo. Build Browse grid, Home and Dashboard variants with consistent image aspect and aligned metadata. Browse reserves two lines each for title and location, full values via Details. Distinguish no image from image error. Use one principal item link and avoid nested interactive controls. Treat the sample photos as prototype data.

**Current integration** — [Views/Shared/_ItemCard.cshtml](../../src/TraceBack/Views/Shared/_ItemCard.cshtml) · [Views/Shared/_CatalogRow.cshtml](../../src/TraceBack/Views/Shared/_CatalogRow.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Browse reports](../pages/04-browse.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Submit a Claim](../pages/08-submit-a-claim.md)

## FigCraft references

Browse library `7012` and dashboard Overview frame `37211`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `2073` | Home / recent report / Light | Component |
| `2148` | Home / recent report / Dark | Component |
| `7013` | Report card / Laptop / Light / Blue water bottle | Component |
| `7026` | Report card / Laptop / Light / Black headphones | Component |
| `7039` | Report card / Laptop / Light / Silver key ring | Component |
| `7052` | Report card / Laptop / Light / Coral umbrella | Component |
| `7065` | Report card / Laptop / Light / Canvas tote bag | Component |
| `7083` | Report card / Laptop / Light / TI-84 graphing calculator | Component |
| `7104` | Report card / Laptop / Light / Long title | Component |
| `7212` | Report card / Phone / Light / Blue water bottle | Component |
| `7225` | Report card / Phone / Light / Black headphones | Component |
| `7238` | Report card / Phone / Light / Silver key ring | Component |
| `7251` | Report card / Phone / Light / Coral umbrella | Component |
| `7264` | Report card / Phone / Light / Canvas tote bag | Component |
| `7282` | Report card / Phone / Light / TI-84 graphing calculator | Component |
| `7303` | Report card / Phone / Light / Long title | Component |
| `7402` | Report card / Laptop / Dark / Blue water bottle | Component |
| `7415` | Report card / Laptop / Dark / Black headphones | Component |
| `7428` | Report card / Laptop / Dark / Silver key ring | Component |
| `7441` | Report card / Laptop / Dark / Coral umbrella | Component |
| `7454` | Report card / Laptop / Dark / Canvas tote bag | Component |
| `7472` | Report card / Laptop / Dark / TI-84 graphing calculator | Component |
| `7493` | Report card / Laptop / Dark / Long title | Component |
| `7601` | Report card / Phone / Dark / Blue water bottle | Component |
| `7614` | Report card / Phone / Dark / Black headphones | Component |
| `7627` | Report card / Phone / Dark / Silver key ring | Component |
| `7640` | Report card / Phone / Dark / Coral umbrella | Component |
| `7653` | Report card / Phone / Dark / Canvas tote bag | Component |
| `7671` | Report card / Phone / Dark / TI-84 graphing calculator | Component |
| `7692` | Report card / Phone / Dark / Long title | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
