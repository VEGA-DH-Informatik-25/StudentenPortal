# AGENTS.md

This file is the repo-level operating guide for AI agents working on CampusConnect. It reflects the current workspace state as of 2026-05-07. Prefer live code and configuration over older prose docs when they disagree, and update this file when project-wide facts change.

## Project Identity

CampusConnect is a web-based student portal for DHBW Loerrach. It centralizes student-life information and workflows that would otherwise be spread across emails, notices, spreadsheets, and chat groups.

Primary users are students. Secondary users are lecturers and university administration staff.

Core product areas:

- Authentication, browser session handling, and profile management.
- News feed with grouped announcements, posts, comments, and reactions.
- Mensa menu integration through the SWFR XML API.
- Exam calendar and DHBW timetable views.
- Grade tracking with optional DHBW study-plan parsing.
- Learning and campus group discovery, membership, and permissions.
- Contact book for campus contacts and profile details.
- Admin user and course management.

Keep user-facing app text in German unless the surrounding feature already uses English.

## Repository Shape

The Git repository root is this workspace root. The main application lives in `CampusConnect/`.

```text
./
  README.md
  projektbeschreibung.md
  AGENTS.md
  CampusConnect/
    .github/
      copilot-instructions.md
      workflows/ci.yml
    CONTRIBUTING.md
    README.md
    docker-compose.yml
    docs/
      api.md
      architecture.md
      code-review.md
      demo-data.md
      roles.md
      testing.md
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

- `projektbeschreibung.md`: product scope, MVP boundaries, target users, and feature list.
- `CampusConnect/README.md`: setup overview, architecture summary, endpoints, and contribution rules.
- `CampusConnect/docs/architecture.md`: frontend/backend architecture and auth flow.
- `CampusConnect/docs/api.md`: current API surface and domain behavior.
- `CampusConnect/docs/testing.md`: current testing conventions.
- `CampusConnect/CONTRIBUTING.md`: branch, commit, PR, and test conventions.
- `CampusConnect/.github/copilot-instructions.md`: project-specific Copilot rules.

## Current Stack

Frontend:

- Angular 21.2 with standalone components only.
- Angular Router with lazy-loaded route components via `loadComponent`.
- Signals for component-local state where practical.
- Zoneless change detection through `provideZonelessChangeDetection()`.
- Functional guards and functional HTTP interceptors.
- SCSS component styles.
- npm 11.6.2 package manager metadata.
- TypeScript 5.9, RxJS 7.8, Zone.js 0.15.
- Angular CLI/build 21.2.7, Vitest 4, jsdom 28, Prettier 3.8.

Backend:

- ASP.NET Core Web API targeting `net10.0`.
- Clean Architecture-style solution with API, Application, Domain, Infrastructure, and test projects.
- OpenAPI and Swagger through `Microsoft.AspNetCore.OpenApi` and `Swashbuckle.AspNetCore`.
- Authentication supports JWT Bearer API clients and HttpOnly browser cookies.
- EF Core 10.0.7 with SQLite provider.
- PdfPig for DHBW study-plan PDF parsing.
- xUnit tests.

Data and external systems:

- SQLite database through Entity Framework Core migrations.
- SWFR Mensa XML API through backend infrastructure only.
- DHBW timetable service through backend infrastructure only, with iCal URL template and course aliases configured under `Timetable`.
- DHBW study-plan index/PDF parsing through backend infrastructure only.

Infrastructure status:

- GitHub Actions CI is implemented in `CampusConnect/.github/workflows/ci.yml` with backend restore/build/test and frontend install/test/build jobs.
- `CampusConnect/docker-compose.yml` is still a placeholder and is not production-ready.

## Backend Architecture

Respect dependency direction:

- `CampusConnect.Domain` has no project dependencies.
- `CampusConnect.Application` depends on Domain.
- `CampusConnect.Infrastructure` depends on Application and Domain.
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
- The frontend must never call SWFR, DHBW timetable, or study-plan sources directly.
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

Structured JSON columns currently store feed comments, feed reactions, group settings, assigned user IDs, and group member permissions.

Repository registrations currently use entity-backed EF repositories for users, courses, feed, groups, grades, and exams. Legacy in-memory repository classes still exist but are not the normal runtime registrations.

Git/database note:

- `CampusConnect/backend/CampusConnect.API/campusconnect.db` exists locally and is intentionally allowed through `.gitignore` so it can be tracked if the team wants a shared demo database.
- SQLite sidecar files `*.db-shm` and `*.db-wal` remain ignored.
- The database may contain local/demo account emails and password hashes. Inspect before committing. Do not commit real personal data, real secrets, or production data.

## Authentication And Security

Current backend auth flow:

- `POST /api/auth/register` and `POST /api/auth/login` return a JWT and also sign in an HttpOnly browser cookie.
- Browser sessions use a cookie scheme with a 15-minute sliding inactivity timeout.
- API clients may use `Authorization: Bearer <token>`.
- Browser requests can authenticate through the HttpOnly cookie.
- `GET /api/auth/me` refreshes browser session state and returns the current profile.
- `POST /api/auth/logout` signs out the browser cookie.

Frontend token/session rules:

- Browser token persistence must stay in memory.
- Do not use `localStorage` or `sessionStorage` for auth tokens.
- Keep auth token handling centralized in `frontend/src/app/core/services/auth.ts` and `frontend/src/app/core/interceptors/auth-token-interceptor.ts`.

Current password hashing:

- New hashes use PBKDF2-SHA256 with per-password random salt and 210,000 iterations.
- Legacy SHA-256 verification remains for existing older hashes.

Registration domain rule:

- Current implementation accepts `@dhbw-loerrach.de` addresses.
- Product docs mention stricter student email scope. Do not change this casually; update tests and docs if the rule changes.

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
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Public |
| GET | `/api/auth/me` | User |
| PUT | `/api/auth/me` | User |
| GET | `/api/courses` | Public |
| GET | `/api/contacts` | User |
| GET | `/api/admin/courses` | Admin |
| POST | `/api/admin/courses` | Admin |
| GET | `/api/admin/users` | Admin |
| PATCH | `/api/admin/users/{id}/role` | Admin |
| PATCH | `/api/admin/users/{id}/course` | Admin |
| DELETE | `/api/admin/users/{id}` | Admin |
| GET | `/api/feed` | User |
| POST | `/api/feed` | User |
| DELETE | `/api/feed/{id}` | User |
| POST | `/api/feed/{id}/comments` | User |
| DELETE | `/api/feed/{postId}/comments/{commentId}` | User |
| POST | `/api/feed/{id}/reactions` | User |
| GET | `/api/mensa` | User |
| GET | `/api/calendar` | User |
| POST | `/api/calendar` | User |
| DELETE | `/api/calendar/{id}` | User |
| GET | `/api/grades` | User |
| GET | `/api/grades/plan` | User |
| POST | `/api/grades` | User |
| DELETE | `/api/grades/{id}` | User |
| GET | `/api/timetable` | User |
| GET | `/api/groups` | User |
| POST | `/api/groups` | User |
| GET | `/api/groups/{id}/settings` | User with group-management permission |
| PUT | `/api/groups/{id}/settings` | User with group-management permission |
| PUT | `/api/groups/{id}/assignments` | User with group-management permission |
| PUT | `/api/groups/{id}/member-permissions` | User with group-management permission |
| POST | `/api/groups/{id}/join` | User |

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

Frontend configuration facts:

- `app.config.ts` registers `de-DE`, zoneless change detection, router input binding, and auth/error interceptors.
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

Study plan:

- Backend services: `DhbwStudyPlanProvider` and `DhbwStudyPlanParser`.
- Parser uses PdfPig.
- `GET /api/grades/plan` resolves the logged-in user's course to DHBW study-plan data where possible.

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
- Setup, commands, or stack: update `CampusConnect/README.md` and this file.
- Testing conventions: update `CampusConnect/docs/testing.md`.
- Project scope changes: update `projektbeschreibung.md` when the requested change affects product boundaries.

When docs and implementation disagree, prefer the live implementation for code changes and either update the docs or mention the doc gap in the final response.