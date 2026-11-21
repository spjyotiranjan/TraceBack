# Home hero and guidance

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Compose identity line/headline, artwork, supporting text, role-specific actions, and later guidance/recent reports. Keep the centered mobile order headline → image → text → actions and use the saved mobile crop. Reserve artwork space and keep actions above the floating dock in the initial member layout. Use empty alt for purely decorative artwork.

**Current integration** — [Views/Home/Index.cshtml](../../src/TraceBack/Views/Home/Index.cshtml) · [Views/Shared/_ProcessPath.cshtml](../../src/TraceBack/Views/Shared/_ProcessPath.cshtml)

**Page plans** — [Home](../pages/01-home.md)

## FigCraft references

Home library `1678`. Mobile hero crop example `2072`.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `1693` | Home / hero artwork / Light | Component |
| `1731` | Home / process Report / laptop / Light | Component |
| `1751` | Home / process Search / laptop / Light | Component |
| `1765` | Home / process Reunite / laptop / Light | Component |
| `1812` | Home / process Report / mobile / Light | Component |
| `1832` | Home / process Search / mobile / Light | Component |
| `1846` | Home / process Reunite / mobile / Light | Component |
| `1880` | Home / hero artwork / Dark | Component |
| `1934` | Home / process Report / laptop / Dark | Component |
| `1954` | Home / process Search / laptop / Dark | Component |
| `1968` | Home / process Reunite / laptop / Dark | Component |
| `2015` | Home / process Report / mobile / Dark | Component |
| `2035` | Home / process Search / mobile / Dark | Component |
| `2049` | Home / process Reunite / mobile / Dark | Component |
| `2070` | Home / hero artwork / mobile / Light | Component |
| `2145` | Home / hero artwork / mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
