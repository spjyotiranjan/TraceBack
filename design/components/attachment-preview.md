# Comment image preview and remove control

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Place preview below composer. Put the original Lucide `trash-2` icon in a properly centered target at the preview’s top right with inset padding. Reveal on photo hover or focus with shadow. Use the saved dark red `#861D2C` and shadow only when the control itself is hovered or focused. The hover master `28859` uses a 24 px visual disc, an approximately 16 px `trash-2` glyph and a 0 × 3 px offset with a 7 px shadow radius. Use a larger transparent interactive hit area so the published control remains easy to operate. Show the control on touch. Keep an accessible label and restore focus after removal. Keep alt input associated with the selected image.

**Current integration** — [wwwroot/js/discussion.js](../../src/TraceBack/wwwroot/js/discussion.js) · [wwwroot/css/discussion.css](../../src/TraceBack/wwwroot/css/discussion.css)

**Page plans** — [Item Details](../pages/05-item-details.md)

## FigCraft references

Map and discussion library `11163`. Identify the original Delete/Photo component set and Lucide trash-2 primitive in the component inventory.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `11303` | Image attached / Laptop / Light | Component |
| `11532` | Image attached / Phone / Light | Component |
| `11761` | Default | Variant of `28877` |
| `11990` | Image attached / Phone / Dark | Component |
| `28856` | Property 1=Default | Variant of `28858` |
| `28858` | Attachment delete | Component set |
| `28859` | State=Hover | Variant of `28858` |
| `28877` | Image attached / Laptop | Component set |
| `28878` | Default | Variant of `28877` |
| `29011` | Property 1=Default | Variant of `29013` |
| `29013` | Attachment photo | Component set |
| `29014` | Property 1=Photo hovered | Variant of `29013` |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
