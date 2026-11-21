# Browse pagination

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Input current page, previous and next availability and the complete search parameters. Render existing Prev/Next navigation using real GET links. Omit unavailable links or render noninteractive context. Keep a labelled nav region and avoid a dummy link when the result set has no next page.

**Current integration** — [Views/Shared/_Pagination.cshtml](../../src/TraceBack/Views/Shared/_Pagination.cshtml)

**Page plans** — [Browse reports](../pages/04-browse.md)

## FigCraft references

Embedded in the Browse frames. Use the Last page and Empty later examples referenced in the [Browse plan](../pages/04-browse.md).

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
