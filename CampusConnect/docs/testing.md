# Testing

Stand der letzten vollständigen lokalen Verifikation: **2026-06-24**.

CampusConnect has tests for the backend API boundary, backend application services, Angular frontend services, guards, interceptors, selected feature pages, and Playwright smoke coverage for central browser flows.

## Backend

Run all backend tests from `CampusConnect/backend`:

```powershell
dotnet test .\CampusConnect.slnx
```

The API test project uses `WebApplicationFactory<Program>` with an isolated SQLite database file and test-only JWT configuration. Add controller and authorization tests to `CampusConnect.API.Tests` when an endpoint is added or its auth behavior changes.

Application service tests live under `CampusConnect.Application.Tests/Features`. Use small fake repositories in tests for expected business-rule failures and service side effects. Keep test doubles in test projects; do not use prototype seed data in production code.

If the command fails with locked files under `CampusConnect.API/bin`, stop the locally running API process and run the command again.

## Frontend

Run all frontend tests from `CampusConnect/frontend`:

```powershell
npm test
```

HTTP service tests use `provideHttpClient()` with `provideHttpClientTesting()` and assert request methods, URLs, and bodies. Guard and interceptor specs should verify behavior, not only creation.

Run the production build when frontend templates, routing, configuration, styles, or build-sensitive code changes:

```powershell
npm run build
```

Run Playwright smoke tests from `CampusConnect/frontend`:

```powershell
npm run e2e
```

The `npm run e2e` script builds the Angular app first, then Playwright starts the API and a static Angular E2E server with an isolated E2E SQLite database under the frontend workspace. It does not use the local development database from `CampusConnect.API/campusconnect.db`.

## Verified Baseline

The following commands passed on 2026-06-24:

- `dotnet test .\CampusConnect.slnx`: 147 tests passed (102 Application, 45 API).
- `npm test -- --watch=false`: 33 test files and 134 tests passed.
- `npm run build`: production build completed successfully with SCSS budget warnings in `group-settings-page.scss`, `navbar.scss`, `admin-page.scss`, and `timetable-page.scss`.

The latest QA summary lives in [QA Evidence](qa-nachweis.md).

Counts are a dated baseline, not a permanently expected total. CI and current command output remain authoritative.

## Local Secrets

The API requires `Jwt:Secret` outside `appsettings.json`. Configure it with user secrets or environment variables before running the API locally:

```powershell
dotnet user-secrets set "Jwt:Secret" "<at-least-32-character-secret>" --project .\CampusConnect.API\CampusConnect.API.csproj
```

Optional bootstrap admin credentials can be configured through `Admin:Email` and `Admin:Password`. Do not commit real credentials.
