# Home landing verification

Home uses the authenticated principal to select guest or member actions and the route background. Member search is an independent, native `GET /Items` form with only `Query`; blank search keeps Browse's Active default. Recent reports use the shared card partial and real report IDs, with separate empty and 503/retry presentations. The retry preserves the original Home GET; Browse stays independently reachable.

The hero follows the shared shell's `56rem` navigation breakpoint. The decorative illustration sits in a reserved square desktop frame or a centered 7:4 phone/tablet crop. The page uses the original SVG replacement from UI 02, not the unlicensed FigCraft bitmap. The separate guest/member route backgrounds still use UI 02's device and theme variants, including the single static pose under reduced motion. See [route artwork](route-art.md) for provenance and limitations.

With Playwright on `NODE_PATH` and full Chromium installed (or `CHROME_PATH` set to an installed Chrome):

```sh
TRACEBACK_BROWSER_TESTS=1 dotnet test TraceBack.slnx -c Release --nologo \
  --filter 'FullyQualifiedName~Home_landing|FullyQualifiedName~Home_recent_recovery' \
  --logger 'console;verbosity=detailed'
```

Set `TRACEBACK_HOME_EVIDENCE` to an existing directory for desktop/mobile, light/dark guest/member captures. The browser fixture runs the real MVC view and cookie authorization on loopback with isolated report-service data. It checks desktop/mobile layout, the 895/896 px boundary, 320 px, native Chromium 200% zoom, delayed artwork and report media, reduced motion, keyboard and touch, fresh Browse GETs, fixed-shell clearance, and report navigation by ID. The MVC fixture verifies empty and 503 responses independently. These checks do not use live MongoDB or object storage.
