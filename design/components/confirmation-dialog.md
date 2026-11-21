# Resolve and delete confirmation

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use a labelled modal with clear Cancel, Resolve or Delete and concise consequences. Focus the safe action initially, trap focus while open and restore on Escape/cancel. Confirm with the existing protected POST and guard repeats. Keep destructive and normal visual roles distinct.

**Current integration** — [Views/Items/Details.cshtml](../../src/TraceBack/Views/Items/Details.cshtml) · [wwwroot/js/site.js](../../src/TraceBack/wwwroot/js/site.js)

**Page plans** — [Item Details](../pages/05-item-details.md)

## FigCraft references

Embedded in the Details Confirm resolve and Confirm delete frames.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
