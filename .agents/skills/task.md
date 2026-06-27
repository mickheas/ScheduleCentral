# Demo Mode v2 — Task Tracker

- `[x]` Check csproj for dependencies, add InMemory EF provider + RateLimiting if needed
- `[x]` Create DemoDbContextConnectionCache.cs — SQLite connection persist cache
- `[x]` Create DemoDataSeeder.cs — per-session in-memory DB with full data seeding
- `[x]` Create DemoAuthMiddleware.cs — fake auth injection + demo mode flag
- `[x]` Create _DemoBanner.cshtml partial — banner + role switcher
- `[x]` Modify _Layout.cshtml — include demo banner partial + adjust padding
- `[x]` Modify Program.cs — DI config, middleware pipeline, rate limiting
- `[x]` Rewrite DemoController.cs — entry/exit/switch/reset only
- `[x]` Delete old _DemoLayout.cshtml and Views/Demo/Index.cshtml
- `[x]` Clean up HomeController.cs Demo action
- `[x]` Build and verify
