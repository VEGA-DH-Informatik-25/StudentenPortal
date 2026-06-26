# AGENTS.md

This file is the repo-level operating guide and primary source of truth for AI agents working on CampusConnect. It reflects the current workspace state as verified on 2026-06-26. Prefer live code and configuration over prose docs when they disagree, and update this file when project-wide facts change.

## Project Identity

CampusConnect is a web-based student portal for DHBW Loerrach. It centralizes student-life information and workflows that would otherwise be spread across emails, notices, spreadsheets, and chat groups.

Primary users are students. Secondary users are lecturers and university administration staff.

Core product areas:

- Authentication, browser session handling, and profile management.
- News feed with grouped announcements, posts, comments, and reactions.
- Mensa menu integration through the SWFR XML API.
- Exam calendar and DHBW timetable views.
- Manual grade tracking with weighted-average simulation.
- Learning and campus group discovery, membership, and permissions.
- Contact book for campus contacts and profile details.
- Admin user and course management.

The interface supports German and English. Add both translations for new user-facing text; use German terminology as the product-language reference when choosing labels.

## Repository Shape

The Git repository root is this workspace root. The main application lives in `CampusConnect/`.

```text
./
  README.md
  prd-mvp.md
  AGENTS.md
  .github/
    copilot-instructions.md
  CampusConnect/
    .github/
      PULL_REQUEST_TEMPLATE.md
      workflows/ci.yml
    docker-compose.yml
    docs/
      README.md
      api.md
      architecture.md
      abgabe-und-uebergabe.md
      code-review.md
      contributing.md
      demo-checkliste.md
      demo-data.md
      frontend.md
      anforderungsstatus.md
      project-overview.md
      qa-nachweis.md
      roles.md
      testing.md
      concepts/
      media/
      product/
      wireframes/
    backend/
      CampusConnect.slnx
      CampusConnect.API/
      CampusConnect.Application/
      CampusConnect.Domain/
      CampusConnect.Infrastructure/
      CampusConnect.API.Tests/
      CampusConnect.Application.Tests/
    frontend/
      angular.json
      package.json
      proxy.conf.json
      src/
        app/
          core/
          features/
          layout/
          shared/ui/
```

Important source documents:

- `CampusConnect/docs/README.md`: central documentation index.
- `CampusConnect/docs/project-overview.md`: setup overview, stack summary, local URLs, and documentation map.
- `CampusConnect/docs/product/projektbeschreibung.md`: product scope, MVP boundaries, target users, and feature list.
- `CampusConnect/docs/abgabe-und-uebergabe.md`: delivery, handover, evidence, and readiness checklist.
- `CampusConnect/docs/anforderungsstatus.md`: current requirements status matrix against the protected MVP PRD.
- `CampusConnect/docs/demo-checkliste.md`: reproducible demo flows and fallback guidance.
- `CampusConnect/docs/qa-nachweis.md`: QA evidence, commands, test counts, CI gates, and known QA gaps.
- `prd-mvp.md`: protected MVP product requirements; do not change requirements casually.
- `CampusConnect/docs/architecture.md`: frontend/backend architecture and auth flow.
- `CampusConnect/docs/api.md`: current API surface and domain behavior.
- `CampusConnect/docs/testing.md`: current testing conventions.
- `CampusConnect/docs/contributing.md`: branch, commit, PR, and test conventions.
- `CampusConnect/docs/concepts/`: planning concepts. Check live code before treating them as implemented behavior.
- `.github/copilot-instructions.md`: short GitHub Copilot entry point that defers to this file.

## Current Stack

Frontend:

- Angular 21.2 with standalone components only.
- Angular Router with lazy-loaded route components via `loadComponent`.
- Signals for component-local state where practical.
- Zoneless change detection through `provideZonelessChangeDetection()`.
- Functional guards and functional HTTP interceptors.
- A custom English/German translation layer under `core/i18n/`.
- A frontend theme service under `core/services/theme.ts` with light, dark, and system modes.
- SCSS component styles.
- npm 11.6.2 package manager metadata.
- TypeScript 5.9, RxJS 7.8, Zone.js 0.15.
- Angular CLI/build 21.2.7, Vitest 4, Playwright 1.61, jsdom 28, Prettier 3.8.

Backend:

- ASP.NET Core Web API targeting `net10.0`.
- Clean Architecture-style solution with API, Application, Domain, Infrastructure, and test projects.
- OpenAPI and Swagger through `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore`.
- Authentication supports JWT Bearer API clients and HttpOnly browser cookies.
- EF Core 10.0.7 with SQLite provider.
- xUnit tests.

Data and external systems:

- SQLite database through Entity Framework Core migrations.
- SWFR Mensa XML API through backend infrastructure only.
- DHBW timetable service through backend infrastructure only, with iCal URL template and course aliases configured under `Timetable`.

Infrastructure status:

- GitHub Actions CI is implemented in `CampusConnect/.github/workflows/ci.yml` with backend restore/build/test, frontend install/test/build, and Playwright smoke-test jobs.
- `CampusConnect/docker-compose.yml` is still a placeholder and is not production-ready.

## Backend Architecture

Respect dependency direction:

- `CampusConnect.Domain` has no project dependencies.
- `CampusConnect.Application` depends on Domain.
- `CampusConnect.Infrastructure` depends on Application and reaches Domain transitively through Application.
- `CampusConnect.API` is the HTTP and composition boundary and references Application and Infrastructure.

Projects:

- `CampusConnect.API`: controllers, API DTOs, `Program.cs`, auth scheme wiring, Swagger setup.
- `CampusConnect.Application`: feature services, commands, result types, application interfaces, password hashing.
- `CampusConnect.Domain`: entities, enums, repository interfaces, value objects.
- `CampusConnect.Infrastructure`: EF Core DbContext, migrations, repositories, external API clients, JWT service, startup initialization.
- `CampusConnect.API.Tests`: API boundary, authorization, repository, migration, seeding, and parser tests.
- `CampusConnect.Application.Tests`: application service and security tests.

Current application feature services:

- `AuthService`
- `CoursesService`
- `FeedService`
- `GroupsService`
- `GradesService`
- `CalendarService`
- `ContactsService`
- `AdminUsersService`

Controller rules:

- Controllers stay thin: accept HTTP input, read current user info, call Application services, map service results to HTTP responses.
- Do not put business logic, persistence logic, external API parsing, password hashing, or JWT creation in controllers.
- Request/response API contracts belong under `CampusConnect/backend/CampusConnect.API/DTOs/`.
- Use `ActionResult<T>` or `IActionResult` consistently with nearby controllers.
- Keep route prefixes in the form `api/<resource>`.
- Protect user-specific and admin endpoints. Use `[AllowAnonymous]` only where a route must be public.

Application/domain rules:

- Use the existing `Result<T>` pattern for expected validation and domain failures.
- Do not throw exceptions for normal user mistakes or expected business-rule failures.
- Keep business rules in Application services or Domain entities/value objects, not in Angular components.
- Use async APIs all the way through. Do not use `.Result` or `.Wait()`.
- Use file-scoped namespaces, nullable reference types, and records for command/result shapes when consistent with nearby files.

Infrastructure rules:

- Repositories implement interfaces from Domain or Application.
- External APIs are called only from Infrastructure services.
- The frontend must never call SWFR or DHBW timetable sources directly.
- JWT creation lives in Infrastructure behind `IJwtService`.

## Current Persistence State

The backend uses `CampusConnectDbContext` with EF Core SQLite:

- Connection string key: `ConnectionStrings:CampusConnect`.
- Default value: `Data Source=campusconnect.db`.
- Local runtime database path: `CampusConnect/backend/CampusConnect.API/campusconnect.db` when running the API from `CampusConnect/backend`.
- EF migrations manage the schema.
- `DatabaseInitializer` baselines older local SQLite databases that were created before migrations, then runs `MigrateAsync`.
- `DevelopmentDemoDataSeeder` seeds configured demo courses, demo users, groups, feed posts, grades, and exam entries in Development when `DemoData:Enabled` is true.

Persisted entities currently include:

- Users
- Courses
- CampusGroups
- FeedPosts
- Grades
- ExamEntries

Structured JSON columns currently store feed comments, feed reactions, group settings, assigned user IDs, and group member roles. Feed posts also persist a publication status (`Pending` or `Published`) and their own comment setting.

Repository registrations currently use entity-backed EF repositories for users, courses, feed, groups, grades, and exams. Legacy in-memory repository classes still exist but are not the normal runtime registrations.

Git/database note:

- `CampusConnect/backend/CampusConnect.API/campusconnect.db` exists locally and is intentionally allowed through `.gitignore` so it can be tracked if the team wants a shared demo database.
- SQLite sidecar files `*.db-shm` and `*.db-wal` remain ignored.
- The database may contain local/demo account emails and password hashes. Inspect before committing. Do not commit real personal data, real secrets, or production data.

## Authentication And Security

Current backend auth flow:

- User accounts are created only by admins through `POST /api/admin/users`; public self-registration is not available.
- `POST /api/auth/login` returns a JWT and also signs in an HttpOnly browser cookie.
- Browser sessions use a cookie scheme with a 15-minute sliding inactivity timeout.
- API clients may use `Authorization: Bearer <token>`.
- Browser requests can authenticate through the HttpOnly cookie.
- JWTs and browser cookies are accepted for protected requests only when their user id still resolves to an active database user.
- `GET /api/auth/me` refreshes browser session state and returns the current profile.
- `POST /api/auth/logout` signs out the browser cookie.

Frontend token/session rules:

- Browser token persistence must stay in memory.
- Do not use `localStorage` or `sessionStorage` for auth tokens.
- Keep auth token handling centralized in `frontend/src/app/core/services/auth.ts` and `frontend/src/app/core/interceptors/auth-token-interceptor.ts`.

Current password hashing:

- New hashes use PBKDF2-SHA256 with per-password random salt and 210,000 iterations.
- Legacy SHA-256 verification remains for existing older hashes.

Account creation domain rule:

- Admin-created users must use `@dhbw-loerrach.de` addresses.
- Product docs previously mentioned stricter student email scope. Do not change this casually; update tests and docs if the rule changes.

Local secrets:

- `Jwt:Secret` is required outside `appsettings.json`.
- Configure it with user secrets or environment variables; never commit real JWT secrets.
- Optional bootstrap admin credentials are configured through the `Admin` section; do not commit real credentials.

Example local secret command from `CampusConnect/backend`:

```powershell
dotnet user-secrets set "Jwt:Secret" "<at-least-32-character-secret>" --project .\CampusConnect.API\CampusConnect.API.csproj
```

## Current API Surface

Development API docs:

- Swagger UI: `http://localhost:5135/swagger`
- Swagger JSON: `http://localhost:5135/swagger/v1/swagger.json`
- ASP.NET OpenAPI JSON: `http://localhost:5135/openapi/v1.json`

Implemented endpoints:

| Method | Endpoint | Auth |
|---|---|---|
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Public |
| GET | `/api/auth/me` | User |
| PUT | `/api/auth/me` | User |
| GET | `/api/courses` | Public |
| GET | `/api/contacts` | User |
| GET | `/api/admin/courses` | Admin |
| POST | `/api/admin/courses` | Admin |
| GET | `/api/admin/users` | Admin |
| POST | `/api/admin/users` | Admin |
| PUT | `/api/admin/users/{id}` | Admin |
| PATCH | `/api/admin/users/{id}/status` | Admin |
| PATCH | `/api/admin/users/{id}/role` | Admin |
| PATCH | `/api/admin/users/{id}/course` | Admin |
| DELETE | `/api/admin/users/{id}` | Admin |
| GET | `/api/feed` | User |
| POST | `/api/feed` | User |
| DELETE | `/api/feed/{id}` | User |
| POST | `/api/feed/{id}/approve` | User with group-management permission |
| POST | `/api/feed/{id}/comments` | User |
| DELETE | `/api/feed/{postId}/comments/{commentId}` | User |
| POST | `/api/feed/{id}/reactions` | User |
| GET | `/api/mensa` | User |
| GET | `/api/calendar` | User |
| POST | `/api/calendar` | User |
| DELETE | `/api/calendar/{id}` | User |
| GET | `/api/grades` | User |
| POST | `/api/grades` | User |
| DELETE | `/api/grades/{id}` | User |
| GET | `/api/timetable` | User |
| GET | `/api/groups` | User |
| POST | `/api/groups` | User |
| GET | `/api/groups/{id}/settings` | User with group-management permission |
| PUT | `/api/groups/{id}/settings` | User with group-management permission |
| DELETE | `/api/groups/{id}` | Owner or Admin; course groups Admin only |
| GET | `/api/groups/{id}/pending-posts` | User with group-management permission |
| GET | `/api/groups/{id}/candidates` | User with group-management permission |
| POST | `/api/groups/{id}/members` | User with group-management permission |
| POST | `/api/groups/{id}/members/course` | User with group-management permission |
| DELETE | `/api/groups/{id}/members/{userId}` | User with group-management permission |
| PUT | `/api/groups/{id}/members/{userId}/role` | User with group-management permission |
| POST | `/api/groups/{id}/join` | User |
| POST | `/api/groups/{id}/requests/{userId}/approve` | User with group-management permission |
| POST | `/api/groups/{id}/requests/{userId}/reject` | User with group-management permission |
| POST | `/api/groups/{id}/invitations` | User with group-management permission |
| DELETE | `/api/groups/{id}/invitations/{userId}` | User with group-management permission |
| POST | `/api/groups/{id}/invitations/accept` | User |
| POST | `/api/groups/{id}/invitations/decline` | User |

When adding, removing, or changing endpoints, update `CampusConnect/docs/api.md` and add focused API tests.

## Current Frontend Architecture

Frontend app root: `CampusConnect/frontend/src/app/`.

Key folders:

- `core/services/`: singleton API services. Components must not call `HttpClient` directly.
- `core/models/`: API request/response interfaces and domain models.
- `core/guards/`: functional route guards such as `authGuard` and `adminGuard`.
- `core/interceptors/`: functional HTTP interceptors.
- `features/`: route-level feature pages.
- `layout/`: shell, navbar, sidebar.
- `shared/ui/`: reusable presentational UI components.

Current route-level features:

- `login`
- authenticated shell at `/`
- `feed`
- `mensa`
- `calendar`
- `timetable`
- `grades`
- `groups`
- `groups/:id/settings`
- `groups/:id`
- `contacts`
- `profile`
- `admin` behind `adminGuard`

Frontend implementation rules:

- Use standalone components only. Do not add NgModules.
- Use `inject()` for dependencies instead of constructor injection.
- Use `ChangeDetectionStrategy.OnPush` on components.
- Use signals for local component state where practical.
- Add new route pages through `app.routes.ts` with `loadComponent`.
- Keep one route-level feature folder per feature under `features/`.
- Put API models in `core/models/` and HTTP calls in `core/services/`.
- Avoid `any`; model data explicitly.
- Preserve the current separate `.ts`, `.html`, and `.scss` component-file style.
- Preserve nearby private/protected member style, including underscore-prefixed private state where present.
- Match the existing application shell and operational UI style. Avoid marketing-style landing pages inside the app.
- Use accessible labels, focus states, semantic HTML, and responsive layouts.

Frontend internationalization rules:

- User-facing interface text uses translation keys from `core/i18n/translations.ts`; do not hard-code new labels, messages, button text, or accessibility text in templates or components.
- Import the standalone `TranslatePipe` in components that render translated template text and use `{{ 'translation.key' | translate }}`.
- Use the injected `I18n` service for translated text or locale-sensitive formatting in TypeScript.
- Add both English and German values for every new `TranslationKey`.
- Use `I18n.readError(error, fallbackKey)` for HTTP/API errors shown in the UI. Do not display raw backend `error` strings directly in components.
- The selected language is a non-sensitive UI preference stored under `campusconnect.language` in `localStorage`. This does not relax the prohibition on storing authentication tokens in browser storage.
- The initial language follows the saved preference or defaults to German. `LOCALE_ID` is `de-DE`; runtime date and number formatting that follows the selected language uses `I18n.locale()`.

Frontend theme rules:

- The settings gear in the navbar contains language and appearance controls.
- Appearance is managed by `core/services/theme.ts` through the `Theme` service and supports `system`, `light`, and `dark`.
- The selected appearance preference is a non-sensitive UI preference stored under `campusconnect.theme` in `localStorage`.
- Without a saved explicit preference, the app uses `system` and follows `prefers-color-scheme`; the service writes the resolved value to `document.documentElement.dataset.theme` and `color-scheme`.
- Use global theme tokens from `styles.scss` for visible colors. New components must work in both light and dark modes without hard-coded light surfaces.

Frontend configuration facts:

- `app.config.ts` registers German and English locale data, sets the static Angular `LOCALE_ID` to `de-DE`, and enables zoneless change detection, router input binding, and auth/error interceptors. `App` initializes `I18n` and `Theme` so document language and theme are applied before the shell is used.
- `proxy.conf.json` proxies `/api` to `http://localhost:5135`.
- Start the API before using API-backed frontend pages locally.

## External Integrations

Mensa:

- Backend service: `CampusConnect.Infrastructure/ExternalServices/MensaApiClient.cs`.
- Configuration section: `Mensa`.
- Current default base URL: `https://www.swfr.de/apispeiseplan`.
- Current default `LocationId`: `677`.
- Current default days: `5`.
- Keep API keys out of Git.

Timetable:

- Backend service: `DhbwTimetableService`.
- Frontend calls only the backend `/api/timetable` endpoint.
- `GET /api/timetable` can omit `course`; the API then uses the authenticated user's profile course.
- `Timetable:CalendarUrlTemplate` contains `{course}` for iCal lookup, and `Timetable:CourseAliases` maps visible course codes to calendar mailbox aliases.

## Commands

Use PowerShell-compatible commands on Windows.

Install frontend dependencies:

```powershell
cd CampusConnect/frontend
npm install
```

Run the frontend dev server:

```powershell
cd CampusConnect/frontend
npm start
```

Build the frontend:

```powershell
cd CampusConnect/frontend
npm run build
```

Run frontend tests:

```powershell
cd CampusConnect/frontend
npm test
```

Run frontend Playwright smoke tests:

```powershell
cd CampusConnect/frontend
npm run e2e
```

Restore backend packages:

```powershell
cd CampusConnect/backend
dotnet restore .\CampusConnect.slnx
```

Build the backend:

```powershell
cd CampusConnect/backend
dotnet build .\CampusConnect.slnx
```

Run backend tests:

```powershell
cd CampusConnect/backend
dotnet test .\CampusConnect.slnx
```

Run the API locally:

```powershell
cd CampusConnect/backend
dotnet run --project .\CampusConnect.API\CampusConnect.API.csproj
```

Expected local URLs:

- API: `http://localhost:5135`
- Swagger: `http://localhost:5135/swagger`
- Frontend: `http://localhost:4200`

Known local build note:

- If backend build fails because `CampusConnect.API/bin/...` files are locked, stop the running API process and rebuild.

## Testing Expectations

- For backend behavior changes, add or update xUnit tests in the nearest backend test project.
- For controller, endpoint, or auth behavior changes, add or update tests in `CampusConnect.API.Tests`.
- For application service rules, add or update tests in `CampusConnect.Application.Tests`.
- For frontend service, guard, interceptor, or component behavior changes, add or update the nearest `.spec.ts` file.
- For browser-level user flows, add or update Playwright smoke tests under `CampusConnect/frontend/e2e`.
- Run the smallest relevant build/test command that validates the change.
- If a change touches both frontend and backend contracts, validate both sides.
- If tests cannot be run, say why in the final response.
- Do not fix unrelated failing tests unless explicitly asked.

Before browser-based validation, prefer the normal local ports. Restart existing local backend/frontend dev servers instead of starting duplicate servers when feasible.

## Contribution Conventions

Branch names:

- `feature/<short-kebab-description>`
- `fix/<short-kebab-description>`
- `docs/<short-kebab-description>`
- `chore/<short-kebab-description>`
- `test/<short-kebab-description>`

Commit format:

```text
<type>(<scope>): <short description>
```

Examples:

- `feat(mensa): wochenspeiseplan anzeigen`
- `fix(auth): abgelaufenes jwt behandeln`
- `docs(api): notenendpunkte dokumentieren`

Pull requests target `main`, request at least one review, address review comments, and use squash merge after approval.

## Hard Rules For Future Agents

- Keep changes focused on the user's request.
- Read the nearest source files and tests before editing.
- Prefer existing patterns over new abstractions.
- Do not add npm or NuGet packages unless the task clearly requires it.
- Do not create EF migrations unless explicitly asked or clearly required by the requested schema change.
- Do not put database calls in controllers.
- Do not put business rules in Angular components.
- Do not call external APIs directly from the frontend.
- Do not store JWTs in browser storage.
- Do not commit secrets, real API keys, real tokens, real credentials, or production data.
- Do not claim Docker is production-ready while `docker-compose.yml` remains a placeholder.
- Do not present generated/demo/local database content as production data.
- Do not create native mobile apps, LMS features, live chat, official Dualis grade integration, multi-tenant university support, or gamification unless explicitly requested.
- Do not overwrite unrelated user changes. If the worktree is dirty, preserve changes you did not make.

## Documentation Maintenance

Update documentation when behavior changes:

- API contracts: update `CampusConnect/docs/api.md`.
- Architecture or auth flow: update `CampusConnect/docs/architecture.md`.
- Setup, commands, or stack: update `CampusConnect/docs/project-overview.md` and this file.
- Testing conventions: update `CampusConnect/docs/testing.md`.
- Project scope changes: update `CampusConnect/docs/product/projektbeschreibung.md` when the requested change affects product boundaries. Do not alter `prd-mvp.md` requirements unless the user explicitly asks for a PRD change.

When docs and implementation disagree, prefer the live implementation for code changes and either update the docs or mention the doc gap in the final response.
