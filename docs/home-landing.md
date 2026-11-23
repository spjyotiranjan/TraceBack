# Home landing verification

Home uses the authenticated principal to select guest or member actions and the route background. Member search is a native `GET /Items` form with only `Query`; a blank submission keeps Browse's Active default. Recent reports use the shared card partial with server data and real report IDs. Retry preserves the exact Home GET and HTTP 503 until the service recovers. Browse remains independently reachable in every member state.

## Design-parity audit

References: the single-page sheets in [01 · Home](../design/UI%20Design/01%20%C2%B7%20Home.pdf) and [Home and account components](../design/UI%20Design/Home%20and%20account%20components.pdf), with the [Home checklist](../design/pages/01-home.md) for frame IDs. The [Home backgrounds](../design/UI%20Design/Home/animated%20graphic%20backgrounds.pdf) and [Shared foundations](../design/UI%20Design/Shared%20foundations%20and%20navigation.pdf) sheets cover the separate shell layer. Baseline: `6c29a30`, including the shared navigation/background PR #43. Both passes use the real Razor app, the same isolated roles and report fixtures, and the static background pose A.

| PDF p1 frame / component | Observable baseline delta and owner | Result |
|---|---|---|
| Guest `1695/1882/1786/1989`, member `2079/2154/2113/2188` — hero | `Home/Index` + `home-browse.css`: 1 px border, rectangular gradient, clipped square asset. The original rendered composition is a motif reference, not the target art. | Original transparent SVG: separate bottle, headphones and umbrella float on staggered loops alongside a 12 s route pulse that fades out at the seam. The hero scrolls with the section; the shared route background remains fixed. No motion in the reduced-motion pose. |
| Member `2079/2154/2113/2188` — search and actions | Home hero used outlined/filled member actions where desktop frames show subordinate links. The search label, field, primary action and responsive order were already present. | Quiet secondary treatment keeps both member routes, target sizes and existing form geometry. The label, search icon, clear control and native request are unchanged. The mobile hero shortcuts remain as existing functional actions even though they are absent in the phone frames. |
| Guest `1695/1882`, guidance components `1731/1751/1765`, `1812/1832/1846` | `_ProcessPath` rendered tiles, a connecting line and package icon instead of outline icons beside the titles and a checked shield. | Desktop columns and mobile single column retain Report → Search → Reunite; simple icons and compact text replace the timeline. Member Home uses the short guidance from its frame's content layer, after Recent reports. |
| Member `2079/2154/2113/2188`, report card components `2073/2148` | `home-browse.css` displayed Home media beside the details; the component has a top image and details below. | Home cards use the stacked preview and roughly 360 px component width. The production 4:3 media contract is retained instead of the wider prototype crop, along with six real reports, status chips, location icon, category/full report date and no-photo/error fallbacks. Prototype photos and sample data are not shipped. |
| Empty `2310/2446/2244/2380`, error `2345/2481/2277/2413` | `Home/Index` reused a wide, centered orbit panel where the visible desktop frames show left-aligned status text. The status also used a peer heading instead of nesting under Recent reports. | Flat, nested status presentation, with the existing explanatory error copy, Retry/Create and independent Browse. Real 200-empty and 503-error responses were captured and tested in both themes. The mobile PDF clips the recovery body and the offscreen member guidance; their full-page pixel parity is not claimed. |

The hero title, art slot, intro and actions retain their measured boxes across the 1440/390 role/theme comparisons. The art slot remains 464 × 464 on desktop and 350 × 200 on phone. The PDF's narrower desktop outer gutter and different headline wrap/scale remain deliberate differences under the requirement to preserve content geometry. No dedicated Home async loading frame is supplied: recent data is server-rendered; delayed photo and decorative background loads were checked for layout shifts rather than adding a client loading state.

## Run the checks

With Playwright on `NODE_PATH` and full Chromium installed (or `CHROME_PATH` set to an installed Chrome):

```sh
TRACEBACK_BROWSER_TESTS=1 dotnet test TraceBack.slnx -c Release --nologo \
  --filter 'FullyQualifiedName~Home_landing|FullyQualifiedName~Home_recent_recovery|FullyQualifiedName~Route_backgrounds_stay|FullyQualifiedName~Shared_shell_in_browser' \
  --logger 'console;verbosity=detailed'
```

Set `TRACEBACK_HOME_EVIDENCE` to an existing directory to record the role/state/width/theme screenshots and box measurements. Home fixtures use isolated local report and comment services on disposable Kestrel hosts, not live data. The Home browser suite covers 1440 and 390 px in Light and Dark, the 895/896 breakpoint, 320 px, native 200% Chromium reflow, keyboard focus, touch, filled/cleared search, report navigation, delayed report media, fixed-shell clearance, reduced motion and animation/scroll behavior. The MVC suite also tests recovery after a failed load. No production object storage or accounts are involved.
