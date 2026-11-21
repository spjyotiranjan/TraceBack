# Discussion and replies

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Keep a bounded scrollable thread with empty, loading, read-only, error/retry and nested reply states. Fetch cursor batches at scroll end, guard overlap and announce appended content without stealing focus. Preserve deleted parent placeholders where children remain. Use GET and POST `/Items/{itemId}/Comments`. Pass `parentId`, `cursor` and `limit` when fetching replies or another batch. Keep existing per-comment image/remove/delete routes and authorize all writes independently. Suppress Load more buttons and retain a clear retry on failure.

**Current integration** — [wwwroot/js/discussion.js](../../src/TraceBack/wwwroot/js/discussion.js) · [wwwroot/css/discussion.css](../../src/TraceBack/wwwroot/css/discussion.css) · [Controllers/CommentsController.cs](../../src/TraceBack/Controllers/CommentsController.cs)

**Page plans** — [Item Details](../pages/05-item-details.md)

## FigCraft references

Map and discussion library `11163`. Use the Reply, Posting and Discussion error frames in [Item Details](../pages/05-item-details.md).

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `11255` | Thread / Laptop / Light | Component |
| `11356` | Discussion loading / Laptop / Light | Component |
| `11358` | Discussion error / Laptop / Light | Component |
| `11362` | Read only / Laptop / Light | Component |
| `11484` | Thread / Phone / Light | Component |
| `11585` | Discussion loading / Phone / Light | Component |
| `11587` | Discussion error / Phone / Light | Component |
| `11591` | Read only / Phone / Light | Component |
| `11713` | Thread / Laptop / Dark | Component |
| `11814` | Discussion loading / Laptop / Dark | Component |
| `11816` | Discussion error / Laptop / Dark | Component |
| `11820` | Read only / Laptop / Dark | Component |
| `11942` | Thread / Phone / Dark | Component |
| `12043` | Discussion loading / Phone / Dark | Component |
| `12045` | Discussion error / Phone / Dark | Component |
| `12049` | Read only / Phone / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
