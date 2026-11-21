# Dashboard claim row

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Accept claim status, item title and optional item ID. Keep the whole title readable across widths, link when the item exists, otherwise render text. Clearly label Pending, Approved and Rejected, and avoid displaying the private message in the dashboard list unless the existing view already does so.

**Current integration** — [Views/Dashboard/Index.cshtml](../../src/TraceBack/Views/Dashboard/Index.cshtml)

**Page plans** — [My Dashboard](../pages/07-my-dashboard.md)

## FigCraft references

Dashboard overview and claims-only layouts under section `37163`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `37169` | Claim row / Pending / Blue water bottle / Desktop / Light | Component |
| `37174` | Claim row / Approved / Black umbrella with a small stitched repair on the handle and a red fabric sleeve / Desktop / Light | Component |
| `37179` | Claim row / Rejected / Silver key ring / Desktop / Light | Component |
| `37184` | Claim row / Rejected / Missing item / Desktop / Light | Component |
| `37761` | Claim row / Pending / Blue water bottle / Mobile / Light | Component |
| `37766` | Claim row / Approved / Black umbrella with a small stitched repair on the handle and a red fabric sleeve / Mobile / Light | Component |
| `37771` | Claim row / Rejected / Silver key ring / Mobile / Light | Component |
| `37776` | Claim row / Rejected / Missing item / Mobile / Light | Component |
| `38347` | Claim row / Pending / Blue water bottle / Desktop / Dark | Component |
| `38352` | Claim row / Approved / Black umbrella with a small stitched repair on the handle and a red fabric sleeve / Desktop / Dark | Component |
| `38357` | Claim row / Rejected / Silver key ring / Desktop / Dark | Component |
| `38362` | Claim row / Rejected / Missing item / Desktop / Dark | Component |
| `38987` | Claim row / Pending / Blue water bottle / Mobile / Dark | Component |
| `38992` | Claim row / Approved / Black umbrella with a small stitched repair on the handle and a red fabric sleeve / Mobile / Dark | Component |
| `38997` | Claim row / Rejected / Silver key ring / Mobile / Dark | Component |
| `39002` | Claim row / Rejected / Missing item / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
