# Report an Item and Edit Report — frame comparison

References: design revision `0e9664c426637bb13a53c695792299bc6c2ffdd4`; the PDFs have one sheet each. Frame IDs below identify the crops on PDF page 1; the same source archive identifies the clipped child layers. Local baseline: `4a40bc1`. The app comparisons use the owner/member on loopback Razor with disposable MongoDB, test-only image storage, controlled Leaflet tiles, fixture values matching the examples where safe, and reduced motion to freeze the background. The navigation and route-art overrides are not part of this change.

## Baseline and intended corrections

| PDF page 1 / frame IDs or layers | Baseline observation | Owner / correction |
|---|---|---|
| 06 `29165` / `30468`, 09 `40776` / `41867` and corresponding dark/state frames | At 1440, the app's 720 px form starts at x=493 instead of x=360; H1 is 32 instead of 44 px. At 390, form width is correct, but the 72 px tracker and body spacing push the heading below the reference (tracker y=82–130; H1 box starts y=147). Desktop tracker uses a tinted block and separate bars instead of a connected rail, dots and active pill. | `Editor.cshtml`, `report-editor.css`: center the form independently, match tracker geometry, use 44/32 px heading, retain anchor/focus behavior. |
| 06 same default/filled frames, 09 all frames; reusable editor fields `29032` | Runtime labels say “Type” and use separate “required” tokens; Edit has no lock. The form starts immediately after a top rule; the sample has none. The runtime description box is shorter; differences in title/category/help spacing accumulate. | Shared editor: 48 px inputs, 136 px description, 14/20 labels, field spacing, section rules and progress; render locked-type icon without changing posted Type. Use app-appropriate hints and copy. |
| 06 `29165`, `29328`, `29663` (phone/dark equivalents); 09 `40776` node `40841`, `41867` node `41937`; component family `11163` | Status box, disclosure and side-by-side desktop coordinates precede the 320/280 px standalone map. Source puts the selected/empty panel and 184 px map before full-width stacked coordinates; retry is part of the map card. Location description and coordinate instructions are longer and displaced. | Shared editor + editor-scoped CSS: group status, real Leaflet, disclosure and retry in a single card; put instructions/stacked inputs beneath it. Retain real zoom, pin, retry, manual text, and legacy use. |
| 06 image states `29801` / `29965` and equivalents; 09 `40993`, `41211`, `41431`, `41650` and all equivalents; lower content under PDF clipping | A raw file chooser follows a wide preview and status/remove controls; in the source upload/replace and camera precede the 270 px preview, status and remove or undo. A large dashed box is present on the default app but absent in the design. Actions stack at 390 vs the source's adjacent 200/118 px actions. | `_ReportImagePicker`, `report-image.js`, `report-editor.css`: progressive enhancement of native file chooser, image state/order/geometry, pending and action layout. Preserve server limits and native controls without JS. |
| Camera `47279`, `47288`, `47297`, `47306`; 06 camera scenarios | Runtime has a smaller heading, a 288 px viewfinder, and variable action widths/heights; phone review controls fight for space. Source body is 420/350 px, with a 291 px viewfinder and a 128 px Cancel beside the primary action. | `_ReportCamera`, `report-camera.js`, `report-editor.css`: match measured frame/action hierarchy and isolate runtime-only guidance and retake. Keep permission, capture and review separate. |
| 09 checklist: conflict/reload, invalid form, absent/legacy | PDF 09 does not supply those variants. Saving frames do not depict a stale response. | Keep existing real 409, reload, ownership, missing/inactive responses and validation; verify against behavior, not an invented visual reference. |

## Frame coverage and recheck

Each row lists separate page-1 crops; four columns give the device/theme. The browser fixture captures the initial viewport plus the relevant scrolled or overlay state. In the Edit PDF, all 20 viewports stop above the photo and actions; the desktop frames cut off just as map guidance starts, and the mobile ones partway through the description. Those portions were compared visually against all PDF crops. For the lower states, the frame's child geometry and the visible component PDF are supplemental evidence—not a claim that the PDF contains the missing pixels.

| Scenario | 1440 Light | 390 Light | 1440 Dark | 390 Dark |
|---|---:|---:|---:|---:|
| Create: Default | 29165 | 30468 | 31723 | 33090 |
| Create: Filled | 29328 | 30625 | 31894 | 33255 |
| Create: Validation | 29491 | 30782 | 32065 | 33420 |
| Create: Map error | 29663 | 30948 | 32245 | 33594 |
| Create: Image error | 29801 | 31080 | 32391 | 33734 |
| Create: Photo selected | 29965 | 31238 | 32563 | 33900 |
| Create: Publishing | 30132 | 31399 | 32738 | 34069 |
| Create: Camera | 30295 | 31556 | 32909 | 34234 |
| Edit: Current photo | 40776 | 41867 | 42918 | 44049 |
| Edit: Replacement selected | 40993 | 42076 | 43143 | 44266 |
| Edit: Removal pending | 41211 | 42286 | 43369 | 44484 |
| Edit: Image error | 41431 | 42498 | 43597 | 44704 |
| Edit: Saving | 41650 | 42709 | 43824 | 44923 |
| Dedicated camera | 47279 | 47288 | 47297 | 47306 |

- **Geometry:** Edit desktop form moves x=493 → 360 and H1 y=128 → 140; the tracker moves to x=120, y=303 with a 218×236 box. On phone, tracker y=104 → 82 and h=72 → 48; Edit H1 y=192 → 147. The form starts at x=20 with width 350. Headings are 44/32 px; inputs and description are 48/136 px. Locked Type, dividers and active tracker markers follow the source; section titles remain anchorable and announced without duplicate visible headings.
- **Lower form:** The pin card precedes stacked coordinates, Date reported and the public contact text field. The real canvas is 560×184 desktop and 310×184 phone, within 600/350 px cards. Tile failure removes unusable map controls but retains typed values and retry. The link uses the displayed normalized point and opens only on activation. Upload/Replace and Take photo align above the 270×270 preview; Save/Publish and Cancel share a 48 px row at both reference widths. The decorative draft card between media and actions is not mounted. The browser fixture freezes the real pending DOM for a capture; a separate browser flow verifies one real multipart request and its Details redirect.
- **Interaction:** The member/owner journeys cover text and keyboard, typed coordinates, tile/asset outage and retry, click/touch placement, drag, zoom, contact round-trip, invalid submissions, actual file chooser, preview replacement/removal/undo, camera denial/cancel/retake/confirmation, focus and track cleanup, and 409 reload with and without page scripts. The application still validates authorization, coordinates, revision and media on the server. The browser checks also cover the 895/896 px transition, 320 px, native 200% zoom, and fixed-background scroll plus a separate motion check.

## Conflicts and limits

- **09 `40776`, `41867`, `42918`, `44049` and subsequent states:** The design's lower layers mark map and both coordinates required (e.g. `40839`, `40876`, `40880`). Edit keeps those inputs optional: blank preserves a saved pin and supports legacy records without one. The visible keeper guidance is deliberately retained. The design's “signed-in viewers” copy (e.g. `40840`) is not used: the existing Details access rules allow anonymous viewers. The picker uses real Leaflet controls and attribution instead of the fictional campus tiles (`40848`, `41944`), never treating an illustration as geographic data.
- **09 image error `41431`, `42498`, `43597`, `44704`:** The source hides Remove image (desktop light node `41574`) while displaying an invalid-replacement error. The app preserves the current photo and keeps its independent Remove action available; selecting a bad file must not prevent an owner from choosing removal. The size limit is displayed as the actual 5 MiB, not the mock's 5 MB. The persisted current image has no prototype filename or size.
- **Camera `47279` / `47288` / `47297` / `47306`:** The mock shows only review and a pair of buttons. The real dialog adds a review-only Retake action below that pair and permits vertical reflow at high zoom. Waiting, live capture, denial and fallback have no matching camera frames. The actual viewfinder uses the user's media geometry, not a bottle illustration.
- **09 conflict/reload and legacy/absent data:** No dedicated PDF frames. These were tested as existing behaviors, not visually redesigned. Full pixel parity of the clipped Edit photo/map/action states cannot be certified from the PDF. The card had no separate reproduction screenshot attachment; comparisons used the provided design assets and the live form. No physical phone camera/keyboard, assistive-technology pass, live map endpoint or production storage was used.
