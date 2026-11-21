# Browse reports

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — `GET /Items or /Items/Index`

**Current view** — [Items/Index.cshtml](../../src/TraceBack/Views/Items/Index.cshtml)

Build an image-led results grid. The 1440 px layout keeps filtering and results visible together. The 390 px layout keeps the compact search header and opens the designed filter region on demand. Reserve exactly two lines for the card heading and two for its location, use second-line ellipsis and keep metadata aligned, with full text available on Item Details. Make missing and failed images use their respective placeholders without layout shift.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `7791` | Browse / motive background / Laptop (`34666`, 1440 × 1050). Scrollable Browse content (`7792`, 1440 × 1050). Report an item (`7795`, 180 × 48). Filters / Laptop / Light / Default (`7797`, 1200 × 186) |
| `8550` | Browse / motive background / Phone (`34722`, 390 × 844). Scrollable Browse content (`8551`, 390 × 844). Report an item (`8554`, 180 × 48). Filters / Phone / Light / Default (`8556`, 350 × 154) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Default | Open Browse without filters | Fetch Active reports by default. Render results using stable item IDs and preserve paging. | Designed · `7791`, `8550`, `9379`, `10194` |
| Applied | Submit Query, Type, Status, Category or Location | Reflect parameter values in controls and chips. Reset Page to 1 for a new or cleared filter. | Designed · `7960`, `8873`, `9556`, `10533` |
| Expanded | Open filters on mobile | Expose filters inline, maintain focus order, and collapse on the applicable apply/close action. | Designed · `8705`, `10357` |
| Found filter | Follow Dashboard’s Find found reports link | Pass Type=Found. Preserve the selected type in pagination and remove it on chip clear. | Designed · `46378`, `46561`, `46730`, `46921` |
| Empty match | Filters match no reports | Explain the filtered empty result and offer filter clearing. | Designed · `8067`, `8966`, `9671`, `10634` |
| No active | No active results without narrowing filters | Show the distinct unfiltered empty message with an appropriate route CTA for the role. | Designed · `8170`, `9055`, `9782`, `10731` |
| Empty later | Requested later page has no results | Show recovery to a valid earlier page and retain filters. | Designed · `8250`, `9121`, `9870`, `10805` |
| Last page | Server reports no next page | Render only available paging actions, retaining each query parameter. | Designed · `8441`, `9284`, `10077`, `10984` |
| Retry | Search returns service unavailable | Render designed retry state. Retry same GET with the same query. Preserve HTTP 503. | Designed · `8337`, `9194`, `9965`, `10886` |
| Prototype interaction | FigCraft demonstrates the flow | Use the destination of the representative prototype card as a flow reference. Every production card uses its own item ID and real filter/search state. | Prototype simulation |

## Reusable components and assets

- [TraceBack brand mark](../components/brand.md)
- [Empty, unavailable and recovery layouts](../components/empty-recovery.md)
- [Browse filters and removable chips](../components/filters.md)
- [Footer](../components/footer.md)
- [Home and Browse search input](../components/home-search.md)
- [Lucide icon primitives](../components/icons.md)
- [Glass navigation and menus](../components/navigation.md)
- [Browse pagination](../components/pagination.md)
- [Report card and image fallback](../components/report-card.md)
- [Route-specific graphic backgrounds](../components/route-backgrounds.md)
- [Report and claim status](../components/status-badge.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Reuse ItemSearch and BrowsePage with the existing 24-item max page size and Next/Previous logic.
2. Align the card grid, filter chip state, and illustration surface with the 1440/390 frames.
3. Test zero, one, full, and last-page rows, long titles/locations, mobile expansion, broken images and query-preserving retries.

Verify keyboard and touch interaction and inspect the two reference widths in both themes. Check intermediate widths, zoom, focus visibility, and that fixed regions never conceal actions.

## FigCraft references

Section **04 · Browse reports**, node `36427`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `7791` | Browse / 1440 / Light / Default | 1440 × 1050 |
| `46378` | Browse / 1440 / Light / Found filter | 1440 × 1050 |
| `8550` | Browse / 390 / Light / Default | 390 × 844 |
| `46561` | Browse / 390 / Light / Found filter | 390 × 844 |
| `9379` | Browse / 1440 / Dark / Default | 1440 × 1050 |
| `46730` | Browse / 1440 / Dark / Found filter | 1440 × 1050 |
| `10194` | Browse / 390 / Dark / Default | 390 × 844 |
| `46921` | Browse / 390 / Dark / Found filter | 390 × 844 |
| `7960` | Browse / 1440 / Light / Applied | 1440 × 1050 |
| `8873` | Browse / 390 / Light / Applied | 390 × 844 |
| `9556` | Browse / 1440 / Dark / Applied | 1440 × 1050 |
| `10533` | Browse / 390 / Dark / Applied | 390 × 844 |
| `8067` | Browse / 1440 / Light / Empty match | 1440 × 1050 |
| `8966` | Browse / 390 / Light / Empty match | 390 × 844 |
| `9671` | Browse / 1440 / Dark / Empty match | 1440 × 1050 |
| `10634` | Browse / 390 / Dark / Empty match | 390 × 844 |
| `8170` | Browse / 1440 / Light / No active | 1440 × 1050 |
| `9055` | Browse / 390 / Light / No active | 390 × 844 |
| `9782` | Browse / 1440 / Dark / No active | 1440 × 1050 |
| `10731` | Browse / 390 / Dark / No active | 390 × 844 |
| `8250` | Browse / 1440 / Light / Empty later | 1440 × 1050 |
| `9121` | Browse / 390 / Light / Empty later | 390 × 844 |
| `9870` | Browse / 1440 / Dark / Empty later | 1440 × 1050 |
| `10805` | Browse / 390 / Dark / Empty later | 390 × 844 |
| `8337` | Browse / 1440 / Light / Retry | 1440 × 1050 |
| `9194` | Browse / 390 / Light / Retry | 390 × 844 |
| `9965` | Browse / 1440 / Dark / Retry | 1440 × 1050 |
| `10886` | Browse / 390 / Dark / Retry | 390 × 844 |
| `8441` | Browse / 1440 / Light / Last page | 1440 × 1050 |
| `9284` | Browse / 390 / Light / Last page | 390 × 844 |
| `10077` | Browse / 1440 / Dark / Last page | 1440 × 1050 |
| `10984` | Browse / 390 / Dark / Last page | 390 × 844 |
| `8705` | Browse / 390 / Light / Expanded | 390 × 844 |
| `10357` | Browse / 390 / Dark / Expanded | 390 × 844 |
