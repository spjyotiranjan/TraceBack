# Privacy reading content

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Keep the three existing paragraphs in order and one semantic page heading. Use the designed 720 px desktop or 350 px mobile reading panel and keep all content reachable at zoom. Use the same policy copy and panel in guest and member shells.

**Current integration** — [Views/Home/Privacy.cshtml](../../src/TraceBack/Views/Home/Privacy.cshtml)

**Page plans** — [Privacy](../pages/13-privacy.md)

## FigCraft references

See Privacy section `47836`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `47848` | Privacy / original policy copy / Desktop / Light | Component |
| `47948` | Privacy / original policy copy / Mobile / Light | Component |
| `48047` | Privacy / original policy copy / Desktop / Dark | Component |
| `48163` | Privacy / original policy copy / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
