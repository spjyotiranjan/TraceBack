# Report maps: local verification

Details shows the saved point beneath the report photo. Create and Edit use the same Leaflet surface, but the latitude and longitude inputs remain editable without it. MVC binding and `MapLocationRules` decide what is saved. Both coordinates are required on Create; on Edit, leave both blank to keep the existing point (or no point for a legacy report). A preview never infers a point from the written location or a design fixture.

## .NET checks

Start an isolated local MongoDB instance and set `TRACEBACK_TEST_MONGO_URI` to its connection string. Without this variable the MongoDB-backed tests return early. Then run:

```sh
dotnet test -c Release --nologo --filter 'FullyQualifiedName~MapLocationTests|FullyQualifiedName~MapLocationIntegrationTests'
dotnet test -c Release --no-build --nologo
```

## Chromium checks

Start TraceBack against a **disposable test database**. The script creates synthetic accounts and reports. Leave `MapTiles:UrlTemplate` at its default for this check so Playwright can intercept the tile host; it drives the loading, failure and recovery responses without depending on an external network or treating test images as map data.

```sh
export Mongo__ConnectionString="$TRACEBACK_TEST_MONGO_URI"
export Mongo__Database=tb_map_ui_check
export BootstrapAdmin__Email= BootstrapAdmin__Password=
dotnet run --project src/TraceBack -c Release --no-build --no-launch-profile --urls http://127.0.0.1:5187
```

From another shell, with Playwright and its Chromium browser installed (`NODE_PATH` can point to a dedicated install's `node_modules`):

```sh
node tests/ui/report-map-check.cjs
```

Set `TRACEBACK_URL` to override the local port. Set `TRACEBACK_MAP_EVIDENCE` to an output directory to save screenshots of the preview outage at desktop and phone widths.
