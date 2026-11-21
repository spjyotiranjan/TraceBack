# Route-specific graphic backgrounds

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Implementation contract

- Export editable decorative SVG artwork and compose behind content, with theme/device variants from the saved file. Use reuniting belongings on Home, blended account art on auth, discovery motifs on Browse, report/story motifs on Details, editor motifs on Create/Edit, activity on Dashboard and a quiet boundary on Privacy. Keep errors visually calm. Use subtle transform/opacity animation where phased states exist, a static reduced-motion pose and pointer-events none.

**Current integration** — [wwwroot/css/home-browse.css](../../src/TraceBack/wwwroot/css/home-browse.css) · [wwwroot/css/detail-report.css](../../src/TraceBack/wwwroot/css/detail-report.css) · [wwwroot/css/site.css](../../src/TraceBack/wwwroot/css/site.css)

**Page plans** — [Home](../pages/01-home.md) · [Sign in](../pages/02-sign-in.md) · [Create account](../pages/03-create-account.md) · [Browse reports](../pages/04-browse.md) · [Item Details](../pages/05-item-details.md) · [Report an Item](../pages/06-report-an-item.md) · [My Dashboard](../pages/07-my-dashboard.md) · [Edit Report](../pages/09-edit-report.md) · [Access Denied](../pages/10-access-denied.md) · [Item Not Found](../pages/11-item-not-found.md) · [Something Went Wrong](../pages/12-something-went-wrong.md) · [Privacy](../pages/13-privacy.md)

## FigCraft references

Graphic libraries `5293` and `34505`. See the [asset register](../foundations.md#asset-register) for the production vs fixture distinction.

| Node ID | Native name | Kind or containing set |
|---:|---|---|
| `5294` | Phase=A | Variant of `5304` |
| `5299` | Phase=B | Variant of `5304` |
| `5304` | Animated background / Laptop / Light | Component set |
| `5305` | Phase=A | Variant of `5315` |
| `5310` | Phase=B | Variant of `5315` |
| `5315` | Animated background / Mobile / Light | Component set |
| `5316` | Phase=A | Variant of `5326` |
| `5321` | Phase=B | Variant of `5326` |
| `5326` | Animated background / Laptop / Dark | Component set |
| `5327` | Phase=A | Variant of `5337` |
| `5332` | Phase=B | Variant of `5337` |
| `5337` | Animated background / Mobile / Dark | Component set |
| `34506` | Property 1=Default | Variant of `34582` |
| `34514` | Property 1=Default | Variant of `34591` |
| `34522` | Property 1=Default | Variant of `34600` |
| `34530` | Property 1=Default | Variant of `34609` |
| `34538` | Property 1=Default | Variant of `34618` |
| `34549` | Property 1=Default | Variant of `34630` |
| `34560` | Property 1=Default | Variant of `34642` |
| `34571` | Property 1=Default | Variant of `34654` |
| `34582` | Browse / motive background / Laptop | Component set |
| `34583` | Phase=B | Variant of `34582` |
| `34591` | Browse / motive background / Phone | Component set |
| `34592` | Phase=B | Variant of `34591` |
| `34600` | Browse / motive background / Laptop | Component set |
| `34601` | Phase=B | Variant of `34600` |
| `34609` | Browse / motive background / Phone | Component set |
| `34610` | Phase=B | Variant of `34609` |
| `34618` | Report / motive background / Laptop | Component set |
| `34619` | Phase=B | Variant of `34618` |
| `34630` | Report / motive background / Phone | Component set |
| `34631` | Phase=B | Variant of `34630` |
| `34642` | Report / motive background / Laptop | Component set |
| `34643` | Phase=B | Variant of `34642` |
| `34654` | Report / motive background / Phone | Component set |
| `34655` | Phase=B | Variant of `34654` |
| `35258` | Property 1=Default | Variant of `35266` |
| `35266` | Details / motive background / Laptop | Component set |
| `35267` | Phase=B | Variant of `35266` |
| `35275` | Property 1=Default | Variant of `35283` |
| `35283` | Details / motive background / Phone | Component set |
| `35284` | Phase=B | Variant of `35283` |
| `35292` | Property 1=Default | Variant of `35300` |
| `35300` | Details / motive background / Laptop | Component set |
| `35301` | Phase=B | Variant of `35300` |
| `35309` | Property 1=Default | Variant of `35317` |
| `35317` | Details / motive background / Phone | Component set |
| `35318` | Phase=B | Variant of `35317` |
| `37111` | Property 1=Default | Variant of `37117` |
| `37117` | Dashboard / motive background / Laptop | Component set |
| `37118` | Phase=B | Variant of `37117` |
| `37124` | Property 1=Default | Variant of `37130` |
| `37130` | Dashboard / motive background / Phone | Component set |
| `37131` | Phase=B | Variant of `37130` |
| `37137` | Property 1=Default | Variant of `37143` |
| `37143` | Dashboard / motive background / Laptop | Component set |
| `37144` | Phase=B | Variant of `37143` |
| `37150` | Property 1=Default | Variant of `37156` |
| `37156` | Dashboard / motive background / Phone | Component set |
| `37157` | Phase=B | Variant of `37156` |
| `45146` | Access denied / Background / Desktop / Light | Component |
| `45217` | Access denied / Background / Mobile / Light | Component |
| `45285` | Access denied / Background / Desktop / Dark | Component |
| `45364` | Access denied / Background / Mobile / Dark | Component |
| `45445` | Item not found / Background / Desktop / Light | Component |
| `45514` | Item not found / Background / Mobile / Light | Component |
| `45580` | Item not found / Background / Desktop / Dark | Component |
| `45657` | Item not found / Background / Mobile / Dark | Component |
| `45736` | Something went wrong / Background / Desktop / Light | Component |
| `45843` | Something went wrong / Background / Mobile / Light | Component |
| `45949` | Something went wrong / Background / Desktop / Dark | Component |
| `46072` | Something went wrong / Background / Mobile / Dark | Component |
| `47316` | Phase=A | Variant of `47325` |
| `47325` | Home / belongings coming together / Guest / Desktop | Component set |
| `47326` | Phase=B | Variant of `47325` |
| `47335` | Phase=A | Variant of `47344` |
| `47344` | Home / belongings coming together / Member / Desktop | Component set |
| `47345` | Phase=B | Variant of `47344` |
| `47354` | Phase=A | Variant of `47363` |
| `47363` | Home / belongings coming together / Guest / Mobile | Component set |
| `47364` | Phase=B | Variant of `47363` |
| `47373` | Phase=A | Variant of `47380` |
| `47380` | Home / belongings coming together / Member / Mobile | Component set |
| `47381` | Phase=B | Variant of `47380` |
| `47388` | Phase=A | Variant of `47397` |
| `47397` | Home / belongings coming together / Guest / Desktop | Component set |
| `47398` | Phase=B | Variant of `47397` |
| `47407` | Phase=A | Variant of `47416` |
| `47416` | Home / belongings coming together / Member / Desktop | Component set |
| `47417` | Phase=B | Variant of `47416` |
| `47426` | Phase=A | Variant of `47435` |
| `47435` | Home / belongings coming together / Guest / Mobile | Component set |
| `47436` | Phase=B | Variant of `47435` |
| `47445` | Phase=A | Variant of `47452` |
| `47452` | Home / belongings coming together / Member / Mobile | Component set |
| `47453` | Phase=B | Variant of `47452` |
| `47842` | Privacy / boundary background / Desktop / Light | Component |
| `47942` | Privacy / boundary background / Mobile / Light | Component |
| `48041` | Privacy / boundary background / Desktop / Dark | Component |
| `48157` | Privacy / boundary background / Mobile / Dark | Component |

## Review

Match the referenced states in both themes. Follow the consuming page’s scenario checks for data, keyboard/touch behavior, fallback and responsive layout.
