# Report image picker and editor photo

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Accept optional JPEG, PNG, GIF and WebP up to 5 MiB before multipart post. Cover empty, current, selected, invalid, remove pending and undo. Keep label and file input working with scripts disabled. Preserve current photo for invalid replacements, revoke local preview URLs when replaced, and reselect files after server redisplay when needed. Separate local Undo from persisted image changes.

**Current integration** — [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [wwwroot/js/report-image.js](../../src/TraceBack/wwwroot/js/report-image.js) · [Services/R2ImageStorage.cs](../../src/TraceBack/Services/R2ImageStorage.cs)

**Page plans** — [Report an Item](../pages/06-report-an-item.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Inspect the Photo selected and Image error Create frames, and Current photo, Replacement selected and Removal pending Edit frames.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
