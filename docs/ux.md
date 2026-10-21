# TraceBack UX direction

TraceBack helps campus members report lost or found belongings, browse reports, and resolve claims. The interface should feel calm and trustworthy: warm off-white canvas, dark ink text, deep teal actions, and a restrained amber accent for found items. Use clear words and dates over decorative graphics. Bootstrap 5 supplies the grid, controls, and responsive navigation; `site.css` supplies a small set of brand tokens and component styles.

## Navigation and screens

| Screen | Primary purpose | Main action |
| --- | --- | --- |
| Home | Explain the service, show recent reports, offer search | Browse items / Report an item |
| Browse | Search and filter by keyword, Lost/Found, category, location, status | Open an item |
| Item detail | Show photo, description, location, reported date, status, and safe contact preference | Submit a claim |
| Report item | Create a Lost or Found report | Publish report |
| Sign in / Register | Give members access to reports and claims | Continue |
| My reports | Review and manage one's posted items | View/edit/resolve report |
| My claims | Track claim status | View claim |

Desktop header: brand left; Browse and Report in the center; account links or account menu right. Mobile header: brand and one clearly labeled menu button; collapsed menu contains the same links in a single column. Keep Report an item visually prominent. Footer has a short campus service note and Privacy link. Do not expose another member's email or private claim message in public listings.

## Page composition

```text
HOME
Header
Hero: Find what matters. / one-sentence explanation
[Search belongings________________] [Search]
[Browse lost items] [Browse found items] [Report an item]
Recent reports: card grid
How it works: Report → Match → Reunite
Footer

BROWSE
Breadcrumb / heading / result count
Search field and Search button
Filters: Type, Category, Location, Status; Clear filters
Result cards: image or placeholder | Lost/Found badge | title | location | date | status
Pagination with current page announced

DETAIL
Back to results / breadcrumb
Image or placeholder | title, type and status
Description, category, location, date reported
Claim action and guidance about proving ownership
Claims or management controls only for authorized users

REPORT
Heading and short privacy guidance
Type → Title → Category → Description → Location → Date → Contact preference → Image URL
Reviewable validation errors / Submit
```

## Responsive behavior

- Design at a 320 px minimum viewport, then use Bootstrap breakpoints. Main content max width around 1140 px, with comfortable horizontal padding.
- Home hero and detail page use one column on phones, two columns from `lg` upward. Report forms stay a readable single column, max width around 720 px.
- Cards use one column on phones, two on tablets, three on desktop. Keep card action and metadata in consistent positions without truncating essential item details.
- Browse filters stack below search on phones. On wider screens they sit in a compact row or sidebar. The filter UI must work without JavaScript; applying it submits a GET form.
- Touch targets should be at least 44 px tall. Allow long titles, locations, and status text to wrap. Never rely on horizontal scrolling for the main flow.

## Reusable Razor components

Use a shared layout for header, footer, skip link, and flash messages. Suggested partials: `_ItemCard`, `_StatusBadge`, `_SearchFilters`, `_EmptyState`, `_FormField`, and `_ValidationSummary`. Keep controller and view models explicit; partials render presentation and do not fetch data. Use one badge vocabulary throughout: **Lost**, **Found**, **Active**, **Resolved**, and claim states **Pending**, **Approved**, **Rejected**.

## Forms, feedback, and accessibility

- Each input has a visible `<label>`; mark required fields in text and explain optional fields. Do not use placeholder text as the label. Provide examples for location and contact preference.
- Put an error summary before the form, link each error to its field, set `aria-invalid="true"` on invalid controls, and associate inline error text via `aria-describedby`. Preserve entered values after validation failure.
- Keep server validation authoritative. Success and failure messages should be text, not color alone; announce them with an appropriate live region or focus target after navigation.
- Give the page one H1, logical heading order, a visible keyboard focus ring, and a skip-to-content link. Ensure menu, filters, cards, and form controls work by keyboard.
- Item images need descriptive alt text where meaningful; decorative placeholders use empty alt text. Badges include their status words. Meet WCAG AA contrast for text and controls.
- Empty results should repeat the search context and offer Clear filters and Report an item. Loading or service errors should say what happened and provide a retry path.

## Implementation scope note

The MVC screens are implemented under `Views` with shared layout and partials. Routes and authorization are owned by the corresponding MVC controllers; the JSON API has separate controllers under `Controllers/Api`.
