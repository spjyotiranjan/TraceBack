# Submit a Claim

[Handoff index](../README.md) · [Foundations](../foundations.md)

## Route and layout

**Entry** — Item Details drawer or authorized `GET` and `POST /Claims/Create?itemId={id}`

**Current view** — [Claims/Create.cshtml](../../src/TraceBack/Views/Claims/Create.cshtml)

Open the drawer over Item Details from the right. Use about 30% desktop width (432 px at 1440) and full width at 390 px, with internal scrolling and reachable actions. Restore focus after dismissal, make the background inert, and use a reduced-motion transition. Direct GET and no-JS links render the same form as a normal page. The claim stays private, separate from the public discussion.

Match the 1440 px and 390 px references in both themes. Let content reflow between them. Use the shared shell’s breakpoint and safe-area clearance. Geometry below is measured in FigCraft units. Treat it as a reference for layout, spacing and responsive behavior.

| Light reference frame | Selected saved layers and dimensions |
|---|---|
| `39658` | Close claim drawer (`39663`, 44 × 44). Scrollable claim form (`39669`, 432 × 862). Item summary / Desktop / Light (`39670`, 384 × 96). Privacy notice / Desktop / Light (`39676`, 384 × 103) |
| `39948` | Close claim drawer (`39952`, 44 × 44). Scrollable claim form (`39958`, 390 × 666). Item summary / Mobile / Light (`39959`, 342 × 96). Privacy notice / Mobile / Light (`39965`, 342 × 103) |

## Scenarios

| Scenario | Trigger | Expected view, action and recovery | Evidence |
|---|---|---|---|
| Default | Eligible visitor chooses Submit a Claim | Show item context, privacy explanation, and one editable message. Preserve itemId and antiforgery. | Designed · `39658`, `39948`, `40232`, `40522` |
| Filled | Member writes a message | Display character feedback. Validate 10–1000 characters and the existing trimmed minimum. | Designed · `39697`, `39986`, `40271`, `40560` |
| Validation | Message is missing or invalid | Keep the drawer/page and submitted text. Associate messages and move focus to error summary or field. | Designed · `39736`, `40024`, `40310`, `40598` |
| Duplicate or failed | Existing duplicate/failed claim result returns | Keep the submitted text and render current server feedback, with dismissal and recovery. | Designed · `39784`, `40071`, `40358`, `40645` |
| Long title and no photo | Item summary exceeds ordinary size or lacks media | Wrap the title and use designed placeholder without pushing actions out of the scroll region. | Designed · `39870`, `40155`, `40444`, `40729` |
| Submitting | Valid message posts | Show pending state, guard repeat posts and honor redirect to Dashboard with success notice. | Designed · `39831`, `40117`, `40405`, `40691` |
| Direct route fallback | Drawer JavaScript is unavailable or link is opened directly | Render the same private form as a standalone page. Preserve authorization and Dashboard redirect. | Existing app / no dedicated frame |
| Prototype interaction | FigCraft demonstrates the flow | Preset outcomes demonstrate drawer behavior. Run the real protected POST and choose the success/error UI from its result. | Prototype simulation |

## Reusable components and assets

- [Buttons and submit actions](../components/button.md)
- [Submit a Claim drawer and direct form](../components/claim-drawer.md)
- [Report summary and metadata](../components/detail-summary.md)
- [Validation, status and retry feedback](../components/feedback.md)
- [Form fields](../components/form-field.md)
- [Lucide icon primitives](../components/icons.md)
- [Report card and image fallback](../components/report-card.md)

Use the [asset register](../foundations.md#asset-register) for the logo, artwork, placeholder and theme variants. Use report data and image endpoints for user content.

## Build steps and acceptance checks

1. Use _ClaimPanel.cshtml, Create.cshtml and claim.js for a single form contract in both presentations.
2. Preserve server eligibility for an active Found item and signed-in non-owner. Maintain existing forbidden/not-found behavior for direct access or a stale item.
3. Test direct/no-JS fallback, Escape, return focus, mobile keyboard and scroll, stale item, duplicate, invalid and successful submission.


## FigCraft references

Section **08 · Submit a Claim · Item Details drawer**, node `39621`, in [TraceBack-Campus-Signal.figcraft](../TraceBack-Campus-Signal.figcraft). The scenario matrix lists the applicable frame IDs.

| Node ID | Exact frame name | Frame size |
|---:|---|---:|
| `39658` | Claim drawer / Desktop / Light / Default | 432 × 1040 |
| `39697` | Claim drawer / Desktop / Light / Filled | 432 × 1040 |
| `39736` | Claim drawer / Desktop / Light / Validation | 432 × 1040 |
| `39784` | Claim drawer / Desktop / Light / Duplicate or failed | 432 × 1040 |
| `39831` | Claim drawer / Desktop / Light / Submitting | 432 × 1040 |
| `39870` | Claim drawer / Desktop / Light / Long title and no photo | 432 × 1040 |
| `39948` | Claim drawer / Mobile / Light / Default | 390 × 844 |
| `39986` | Claim drawer / Mobile / Light / Filled | 390 × 844 |
| `40024` | Claim drawer / Mobile / Light / Validation | 390 × 844 |
| `40071` | Claim drawer / Mobile / Light / Duplicate or failed | 390 × 844 |
| `40117` | Claim drawer / Mobile / Light / Submitting | 390 × 844 |
| `40155` | Claim drawer / Mobile / Light / Long title and no photo | 390 × 844 |
| `40232` | Claim drawer / Desktop / Dark / Default | 432 × 1040 |
| `40271` | Claim drawer / Desktop / Dark / Filled | 432 × 1040 |
| `40310` | Claim drawer / Desktop / Dark / Validation | 432 × 1040 |
| `40358` | Claim drawer / Desktop / Dark / Duplicate or failed | 432 × 1040 |
| `40405` | Claim drawer / Desktop / Dark / Submitting | 432 × 1040 |
| `40444` | Claim drawer / Desktop / Dark / Long title and no photo | 432 × 1040 |
| `40522` | Claim drawer / Mobile / Dark / Default | 390 × 844 |
| `40560` | Claim drawer / Mobile / Dark / Filled | 390 × 844 |
| `40598` | Claim drawer / Mobile / Dark / Validation | 390 × 844 |
| `40645` | Claim drawer / Mobile / Dark / Duplicate or failed | 390 × 844 |
| `40691` | Claim drawer / Mobile / Dark / Submitting | 390 × 844 |
| `40729` | Claim drawer / Mobile / Dark / Long title and no photo | 390 × 844 |
