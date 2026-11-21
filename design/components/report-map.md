# Map display and pin picker

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use existing Leaflet and labeled coordinates. Provide display pin, collapsed coordinates, privacy notice, loading, tiles failure, retry and legacy no-pin variants. In edit/create bind MapLocation coordinates and keep manual coordinate entry when scripts or tiles fail. Persist pin while refreshing. Decorative map previews are fixtures, never geographic source data.

**Current integration** — [wwwroot/js/report-map.js](../../src/TraceBack/wwwroot/js/report-map.js) · [wwwroot/css/report-map.css](../../src/TraceBack/wwwroot/css/report-map.css) · [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [Views/Items/Details.cshtml](../../src/TraceBack/Views/Items/Details.cshtml)

**Page plans** — [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Map and discussion library `11163`, plus the Create/Edit page frames.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `11164` | Pin / Laptop / Light | Component |
| `11197` | Coordinates / Laptop / Light | Component |
| `11231` | Map loading / Laptop / Light | Component |
| `11239` | Map error / Laptop / Light | Component |
| `11248` | Legacy / Laptop / Light | Component |
| `11393` | Pin / Phone / Light | Component |
| `11426` | Coordinates / Phone / Light | Component |
| `11460` | Map loading / Phone / Light | Component |
| `11468` | Map error / Phone / Light | Component |
| `11477` | Legacy / Phone / Light | Component |
| `11622` | Pin / Laptop / Dark | Component |
| `11655` | Coordinates / Laptop / Dark | Component |
| `11689` | Map loading / Laptop / Dark | Component |
| `11697` | Map error / Laptop / Dark | Component |
| `11706` | Legacy / Laptop / Dark | Component |
| `11851` | Pin / Phone / Dark | Component |
| `11884` | Coordinates / Phone / Dark | Component |
| `11918` | Map loading / Phone / Dark | Component |
| `11926` | Map error / Phone / Dark | Component |
| `11935` | Legacy / Phone / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
