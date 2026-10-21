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

To provision the first admin, set `BootstrapAdmin__Email` and `BootstrapAdmin__Password` (at least 12 characters) before startup. `BootstrapAdmin__Name` is optional. The configured account is created or promoted to Admin, and its password is reset to the configured value on every startup while these settings remain present. Remove these settings after the initial provision, or manage them as protected deployment secrets. Ordinary registration always creates a Student account.

## Main flows

- Register or sign in, report a lost or found item, browse and filter active reports, and manage your own reports.
- Submit a claim for another member's active found item, then track its status on your dashboard.
- An admin can review claims, approve or reject them, moderate reports, and mark reports resolved.

The API starts under `/api`; login returns a JWT for requests that need a bearer token. Report forms accept optional JPEG, PNG, GIF, or WebP images up to 5 MB. Owners and admins can replace or remove an image. API clients can use `PUT /api/items/{id}/image` with a multipart `file` field and `DELETE /api/items/{id}/image`. Existing reports with external image URLs still display, but new reports use R2 uploads. Notification delivery is a future addition.

## Verification

Run `dotnet test TraceBack.slnx -c Release` for the build and image validation tests. A running MongoDB instance and configured R2 bucket are needed to exercise complete report upload, retrieval, replacement, and deletion workflows. Check the site at phone and desktop widths when changing Razor views or styles.
