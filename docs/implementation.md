# TraceBack implementation plan

The first delivery is a usable local campus lost-and-found app. Work stays in the existing `src/TraceBack` project, with separate API, MVC, service, model, and data modules. MongoDB is the only required external runtime service. Jira is not used for this project.

| Order | Work | Owner | Acceptance |
| --- | --- | --- | --- |
| 1 | Screen map and responsive patterns | Bret | UX guide describes all main screens and mobile behavior. |
| 2 | MongoDB models, account/item/claim services, and JSON API | Kimball | App builds; API enforces JWT, ownership, validation, and Admin role. |
| 3 | Razor screens, shared layout, and responsive styling | Wayne | Members can browse, report, claim, and manage items; admin can review claims. |
| 4 | Configuration, integration, and local setup guide | Patrick | Local setup is documented; build and available smoke checks pass. |
| 5 | Review of the completed changes | Grace | Findings are resolved before a merge. |

The API and MVC pages share services and data contracts. Wayne's screens depend on the account, item, and claim services; Grace's review follows the integrated build. There is no AI feature or separate low-level design task in this delivery, so Terressa and Jason are not assigned implementation work now.

## Later work

- Exercise registration, item, claim, and admin workflows against a running MongoDB instance.
- Add image upload/storage only after choosing a storage destination; the first delivery accepts an image URL.
- Add notifications and deployment after the core workflows have passed end-to-end checks.

Keep each new feature in the existing module boundaries. Update this plan and the project layout in `README.md` when adding or moving folders.
