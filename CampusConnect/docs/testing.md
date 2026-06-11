# Testing

Stand der letzten vollständigen lokalen Verifikation: **2026-06-11**.

CampusConnect has tests for the backend API boundary, backend application services, and Angular frontend services, guards, interceptors, and selected feature pages.

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

## Verified Baseline

The following commands passed on 2026-06-11:

- `dotnet test .\CampusConnect.slnx --configuration Release`: 102 tests passed (69 Application, 33 API).
- `npm test -- --watch=false`: 31 test files and 88 tests passed.
- `npm run build`: production build completed successfully.

Counts are a dated baseline, not a permanently expected total. CI and current command output remain authoritative.

## Local Secrets

The API requires `Jwt:Secret` outside `appsettings.json`. Configure it with user secrets or environment variables before running the API locally:

```powershell
dotnet user-secrets set "Jwt:Secret" "<at-least-32-character-secret>" --project .\CampusConnect.API\CampusConnect.API.csproj
```

Optional bootstrap admin credentials can be configured through `Admin:Email` and `Admin:Password`. Do not commit real credentials.
