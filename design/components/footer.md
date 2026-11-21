# Footer

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Use the shared document-flow footer with a Privacy link to GET /Home/Privacy. Retain public/member access and spacing above the mobile safe area and dock. Keep links underlined or otherwise unmistakable, with a visible focus ring.

**Current integration** — [Views/Shared/_Layout.cshtml](../../src/TraceBack/Views/Shared/_Layout.cshtml)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Browse reports](../pages/04-browse.md) · [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Edit Report](../pages/09-edit-report.md) · [Access Denied](../pages/10-access-denied.md) · [Item Not Found](../pages/11-item-not-found.md) · [Something Went Wrong](../pages/12-something-went-wrong.md) · [Privacy](../pages/13-privacy.md)

## FigCraft references

See Home frames `1695` / `1786` and Privacy frames `47857` / `47991`.

This component is extracted from the cited page frames for implementation. The saved file has no separately named native master for this component family.

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
