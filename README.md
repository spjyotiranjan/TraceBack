# TraceBack

TraceBack is a responsive campus lost-and-found app. It uses ASP.NET Core MVC with Bootstrap for pages, ASP.NET Core Web API for JSON endpoints, MongoDB for storage, and JWT bearer tokens for the API. The MVC pages use a secure, HTTP-only sign-in cookie. Both interfaces share the same services and data models.

## Project layout

```text
src/TraceBack/
  Controllers/Api/   JSON endpoints and API authorization
  Controllers/       MVC page actions
  Data/              MongoDB collections and indexes
  Models/            Persisted entities and validated input forms
  Services/          Account, item, claim, and admin operations
  Views/             Razor pages and reusable partials
  wwwroot/           Bootstrap, CSS, JavaScript, and static assets
docs/ux.md           Screen map and responsive design guidance
tests/TraceBack.Tests/  Image upload validation tests
```

Keep page presentation in `Views`, HTTP concerns in controllers, business rules in `Services`, and MongoDB setup in `Data`. Add new features within these modules instead of putting query or authorization logic in Razor views.

## Local setup

1. Install the .NET 10 SDK and run MongoDB locally on `mongodb://localhost:27017`, or set a connection string for an existing MongoDB instance.
2. Set configuration through a `.env` file (see `.env.example`), process environment variables, or local user secrets. Do not commit credentials.

   Create a `.env` file at the repository root:

   ```env
   Mongo__ConnectionString=mongodb://localhost:27017
   Mongo__Database=TraceBack
   Jwt__Issuer=TraceBack
   Jwt__Secret=<at least 32 random characters>
   ```

   For report image uploads, create an R2 API token with **Object Read & Write** permission scoped to the existing bucket. Add these settings to `.env` or your deployment secret store:

   ```env
   R2__AccountId=<Cloudflare account ID>
   R2__Bucket=<existing bucket name>
   R2__AccessKeyId=<R2 Access Key ID>
   R2__SecretAccessKey=<R2 Secret Access Key>
   ```

   Keep the bucket private. The server stores uploaded files there and serves report images through `/Items/Image/{id}`. The R2 secret is never sent to browsers or returned by the API. Image uploads are unavailable until all four settings are present.

3. Start the app:

   ```powershell
   dotnet restore TraceBack.slnx
   dotnet run --project src/TraceBack/TraceBack.csproj
   ```

Open the address printed by `dotnet run`. Development can start without `Jwt__Secret`; in that case the app generates a new signing key for that run, so existing API tokens expire after restart. Set `Jwt__Secret` in any persistent environment. The app requires it outside Development.

## Map tiles

Report create, edit, and detail pages load the bundled Leaflet 1.9.4 assets. Configure an OSM-compatible tile provider
with `MapTiles__UrlTemplate`, `MapTiles__AttributionHtml`, and `MapTiles__MaxZoom`. The defaults use public OpenStreetMap
tiles. Keep attribution visible, browser caching and the normal Referer header enabled, and do not prefetch tiles. Tile
failures leave coordinate entry and the report's Google Maps link available. Review provider terms before deployment.
Attribution supports plain text and absolute HTTP(S) links. Other markup and link schemes are discarded before Leaflet
renders it, so configure provider credit using text and `<a href="https://...">` links only.

## Azure App Service deployment

Use a **Windows App Service** with the **.NET 10** runtime. The GitHub Actions workflow at `.github/workflows/main_traceback.yml` builds and tests the solution on pushes and pull requests to `main`. Pushes to `main` and manual runs from `main` publish `src/TraceBack/TraceBack.csproj` and deploy only the web app. The Windows deployment package excludes unused non-Windows SkiaSharp binaries and debug symbols to reduce its size. It retains both x86 and x64 native libraries for either App Service process setting.

Set these App Service environment variables under **Settings → Environment variables** before deploying:

```text
Mongo__ConnectionString=<reachable MongoDB connection string>
Mongo__Database=TraceBack
Jwt__Secret=<random secret of at least 32 characters>
Jwt__Issuer=TraceBack
```

After Azure login, the workflow sets `WEBSITE_RUN_FROM_PACKAGE=1` on the `TraceBack` App Service before deployment. The Azure identity must be allowed to list the app and update its settings. This makes App Service mount the ZIP instead of extracting it into `wwwroot`, which addresses the disk-space failure in the original deployment. Keep credentials in App Service settings, not in Git. If using MongoDB Atlas, permit connections from the App Service outbound network. Configure the four `R2__*` settings shown above if image uploads are needed. The app creates MongoDB indexes when its data service is first used, so an unreachable database prevents normal site operation.

After saving settings, rerun the GitHub workflow from `main` or merge the deployment change into `main`. Open the Azure site and check the home page, sign-in, and an item flow. If the Azure default welcome page remains, check the workflow's deploy job and App Service log stream. A disk-space error even with the reduced package and run-from-package setting requires clearing App Service storage or increasing the plan's storage capacity.

To provision the first admin, set `BootstrapAdmin__Email` and `BootstrapAdmin__Password` (at least 12 characters) before startup. `BootstrapAdmin__Name` is optional. The configured account is created or promoted to Admin, and its password is reset to the configured value on every startup while these settings remain present. Remove these settings after the initial provision, or manage them as protected deployment secrets. Ordinary registration always creates a Student account.

## Main flows

- Register or sign in, report a lost or found item, browse and filter active reports, and manage your own reports.
- Submit a claim for another member's active found item, then track its status on your dashboard.
- An admin can review claims, approve or reject them, moderate reports, and mark reports resolved.

The API starts under `/api`; login returns a JWT for requests that need a bearer token. Report forms accept optional JPEG, PNG, GIF, or WebP images up to 5 MB. Owners and admins can replace or remove an image. API clients can use `PUT /api/items/{id}/image` with a multipart `file` field and `DELETE /api/items/{id}/image`. Existing reports with external image URLs still display, but new reports use R2 uploads. Notification delivery is a future addition.

### Report discussion routes

Signed-in viewers can list text comments with `GET /Items/{itemId}/Comments` using the cookie session, or `GET /api/items/{itemId}/comments` using a bearer token. Both return JSON with `comments` and `nextCursor`. Omit `parentId` for roots; pass a comment ID to list its direct replies. Pass `cursor` for the next page and `limit` from 1 to 50 (default 20). Replies can nest to any depth by listing each parent's children. A missing report or a parent outside that report returns 404; a malformed or unrelated cursor returns 400.

Create with `POST` to the same route, sending multipart fields `body` (1–2000 characters after trimming) and optional `parentId`. New posts require an active report. The cookie route also accepts URL-encoded form fields and requires an anti-forgery token on writes. Bearer API routes use multipart form data. The response is a comment with a stable author name and creation time. Comment images are a separate follow-up feature.

An author or Admin can soft-delete through `POST /Items/{itemId}/Comments/{commentId}/Delete` with a cookie and anti-forgery token, or `DELETE /api/items/{itemId}/comments/{commentId}` with a bearer token. The deleted comment remains as a reply anchor with `deleted: true` and `body: null`. Deleting a report removes its comment records.

## Verification

Run `dotnet test TraceBack.slnx -c Release` for the build and image validation tests. A running MongoDB instance and configured R2 bucket are needed to exercise complete report upload, retrieval, replacement, and deletion workflows. Check the site at phone and desktop widths when changing Razor views or styles.
