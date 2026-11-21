# Report and claim status

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Expose text for Lost/Found type and Active/Claimed/Resolved report status. Claim rows expose Pending/Approved/Rejected separately. Use semantic theme roles and maintain foreground contrast. Color is supplementary. Keep type and status rules out of the visual component.

**Current integration** — [Views/Shared/_StatusBadge.cshtml](../../src/TraceBack/Views/Shared/_StatusBadge.cshtml)

**Page plans** — [Browse reports](../pages/04-browse.md) · [Item Details](../pages/05-item-details.md) · [My Dashboard](../pages/07-my-dashboard.md)

## FigCraft references

Embedded in card/detail/claim layouts. Inspect Browse `7791`, Details `12080` and Dashboard `37211`.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
