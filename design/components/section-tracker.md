# Responsive report section tracker

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Drive from the same section anchors for What happened, Where and when, How to reconnect, and Image. Desktop has a vertical active rail. Mobile has horizontal progress and one label directly beneath it. Mark the current section semantically. Scroll and anchor interactions update it. Use scroll margin for floating navigation and reduced motion for jump behavior.

**Current integration** — [Views/Items/Editor.cshtml](../../src/TraceBack/Views/Items/Editor.cshtml) · [wwwroot/css/detail-report.css](../../src/TraceBack/wwwroot/css/detail-report.css)

**Page plans** — [Report an Item](../pages/06-report-an-item.md) · [Edit Report](../pages/09-edit-report.md)

## FigCraft references

Tracker variants appear in Create and Edit. Saved tracker label and active-state variables are in the Report section tracker collection.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `36445` | Report section tracker / Vertical / Light | Component |
| `36468` | Report section tracker / Horizontal / Light | Component |
| `36482` | Report section tracker / Vertical / Dark | Component |
| `36505` | Report section tracker / Horizontal / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
