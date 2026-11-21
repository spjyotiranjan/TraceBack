# Comment and reply composer

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Place Lucide image attachment and right-arrow submit controls inside the input surface, with accessible names and aligned hit targets. Bind `body`, optional `parentId`, optional `imageFile` and `imageAlt` to the current multipart endpoint. Preserve text, reply target, file and alt during recoverable errors. Require either 1–2,000 text characters or one supported image with 1–200 alt characters. Use the 5 MiB upload limit and allow inline recovery on HTTP 400, 413 or 503. If the report became inactive, preserve the draft and update the composer to the read-only state instead of prompting repeated submissions. Clear the draft only after success. Guests can read and use a sign-in link. Keep empty/invalid/pending and server-returned errors explicit.

**Current integration** — [wwwroot/js/discussion.js](../../src/TraceBack/wwwroot/js/discussion.js) · [wwwroot/css/discussion.css](../../src/TraceBack/wwwroot/css/discussion.css)

**Page plans** — [Item Details](../pages/05-item-details.md)

## FigCraft references

Map and discussion library `11163`. Inspect the Image attached frame and related composer states.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `11277` | Reply / Laptop / Light | Component |
| `11332` | Post error / Laptop / Light | Component |
| `11370` | Posting / Laptop / Light | Component |
| `11506` | Reply / Phone / Light | Component |
| `11561` | Post error / Phone / Light | Component |
| `11599` | Posting / Phone / Light | Component |
| `11735` | Reply / Laptop / Dark | Component |
| `11790` | Post error / Laptop / Dark | Component |
| `11828` | Posting / Laptop / Dark | Component |
| `11964` | Reply / Phone / Dark | Component |
| `12019` | Post error / Phone / Dark | Component |
| `12057` | Posting / Phone / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
