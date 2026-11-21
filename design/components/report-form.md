# Shared Create/Edit report editor

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Pass mode, form values, persisted image, errors, layout sections, and submit URL. Create defaults to Lost. Edit retains a locked type and data. Divide the single form with subtle rules and semantic headings. Lost uses search-x. Found uses package-check. Selected icons inherit selected label color. Bind existing form validations and antiforgery multipart submission.

**Current integration** — [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [Controllers/ItemsController.cs](../../src/TraceBack/Controllers/ItemsController.cs)

**Page plans** — [Report an Item](../pages/06-report-an-item.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Editor library `29032` and Create `29165` / `30468`. Edit `40776` / `41867`.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
