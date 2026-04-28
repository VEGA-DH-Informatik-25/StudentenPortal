# Copilot Project Instructions - CampusConnect

Read this file before making changes in this repository. It is the project-level source of truth for GitHub Copilot behavior.

## Project Identity

CampusConnect is a web-based student portal for DHBW Loerrach. It centralizes student-life information and workflows that are otherwise split across emails, notices, spreadsheets, and chat groups.

Primary users are students. Secondary users are lecturers and university administration staff.

Core product areas:

- Authentication and profile management
- News feed and official announcements
- Mensa menu integration through the SWFR XML API
- Exam calendar and timetable views
- Grade tracking
- Learning group matching
- Admin user and content management

The main application lives in `CampusConnect/`. Root-level files such as `README.md` and `projektbeschreibung.md` provide project context.

## Source Documents

Use these docs before changing related behavior:

- `projektbeschreibung.md` - product scope, MVP boundaries, target users, and feature list
- `CampusConnect/README.md` - setup overview, architecture summary, contribution rules
- `CampusConnect/docs/architecture.md` - frontend and backend architecture decisions
- `CampusConnect/docs/api.md` - planned API surface
- `CampusConnect/docs/roles.md` - team responsibilities and review ownership
- `CampusConnect/CONTRIBUTING.md` - branch, commit, PR, and test conventions
- `CampusConnect/frontend/README.md` - Angular CLI commands

When documentation conflicts with the actual implementation, prefer the live project files for code changes and update the docs if the task touches that behavior. Current known implementation differences: backend projects target `net10.0`; the frontend uses Angular 21; Infrastructure currently wires SQLite and several in-memory repositories, while older docs mention PostgreSQL and .NET 9.

## Repository Layout

```text
CampusConnect/
  backend/
    CampusConnect.slnx
    CampusConnect.API/              ASP.NET Core controllers, API DTOs, Program.cs
    CampusConnect.Application/      Application feature services, result types, interfaces
    CampusConnect.Domain/           Entities, enums, domain interfaces
    CampusConnect.Infrastructure/   EF Core, repositories, external services, JWT service
    CampusConnect.API.Tests/        xUnit API tests
    CampusConnect.Application.Tests/xUnit application tests
  frontend/
    src/app/
      core/                         singleton services, models, guards, interceptors
      features/                     route-level feature pages
      layout/                       app shell, navbar, sidebar
      shared/ui/                    reusable presentational UI components
  docs/                             architecture, API, roles, wireframes
  .github/                          PR template and workflow files
```

## Current Stack

Frontend:

- Angular 21 with standalone components
- Angular Router with lazy-loaded route components
- SCSS component styles
- Signals for local component state
- Zoneless change detection via `provideZonelessChangeDetection()`
- Functional guards and functional HTTP interceptors
- npm scripts from `CampusConnect/frontend/package.json`
- Angular CLI unit-test builder with Vitest dependencies present

Backend:

- ASP.NET Core Web API targeting `net10.0`
- Clean Architecture-style solution split into API, Application, Domain, Infrastructure, and test projects
- JWT Bearer authentication
- EF Core SQLite in the current implementation
- Entity-backed users plus in-memory feed, grade, and exam repositories in the current implementation
- xUnit test projects

External systems:

- SWFR Mensa XML API through `CampusConnect.Infrastructure/ExternalServices/MensaApiClient.cs`
- DHBW timetable service through `CampusConnect.Infrastructure/ExternalServices/DhbwTimetableService.cs`

CI and Docker:

- `CampusConnect/.github/workflows/ci.yml` is currently a placeholder
- `CampusConnect/docker-compose.yml` is currently a placeholder
- Do not claim Docker or CI is production-ready until those files are implemented

## General Working Rules

- Keep changes focused on the user's request. Do not refactor unrelated areas.
- Follow existing patterns in the nearest files before introducing a new abstraction.
- Prefer small, explicit changes over speculative architecture rewrites.
- Do not add npm or NuGet packages unless the user explicitly asks or the task cannot be completed without one.
- Do not create EF migrations unless the user explicitly asks.
- Do not commit secrets, API keys, real tokens, or private credentials.
- Keep user-facing app text in German unless the surrounding feature already uses English.
- Keep code comments rare and useful. Do not leave TODO comments as placeholders.
- If behavior changes, update the relevant documentation or mention the doc gap in the final response.
- Do not create native mobile app, LMS, live chat, official Dualis grade integration, multi-tenant university support, or gamification features unless explicitly requested.

## Backend Architecture Rules

Respect dependency direction:

- `CampusConnect.Domain` has no project dependencies.
- `CampusConnect.Application` may depend on Domain only.
- `CampusConnect.Infrastructure` may depend on Application and Domain.
- `CampusConnect.API` is the composition and HTTP boundary. Controllers should depend on Application services and API DTOs, not repositories or DbContext directly.
- `CampusConnect.API/Program.cs` may reference Infrastructure for dependency injection registration.

Current backend feature pattern:

- Application features are service-based, for example `AuthService`, `FeedService`, `GradesService`, `CalendarService`, and `AdminUsersService`.
- Commands and DTO-like records are currently colocated with their application service files.
- The project does not currently use MediatR. Do not generate MediatR handlers unless the user asks for that migration or the package is intentionally introduced.

Controller rules:

- Controllers stay thin: validate/accept HTTP input, call the Application service, map the result to an HTTP response.
- Do not put business logic, persistence logic, external API parsing, or password/token logic in controllers.
- Request/response API contracts belong under `CampusConnect.API/DTOs/`.
- Use `ActionResult<T>` or `IActionResult` consistently with surrounding controllers.
- Keep route prefixes in the form `api/<resource>`.

Application and domain rules:

- Use the `Result<T>` pattern for expected validation and domain failures.
- Do not throw exceptions for normal user mistakes or expected business-rule failures.
- Keep business rules in Application services or Domain entities/value objects, not in the frontend.
- Use async APIs all the way through. Do not use `.Result` or `.Wait()`.
- Use file-scoped namespaces and nullable reference types.
- Prefer records for command/request/result data shapes when consistent with nearby code.

Infrastructure rules:

- Repositories implement interfaces from Domain or Application.
- External APIs are called only from Infrastructure services.
- The frontend must never call SWFR directly. It calls the backend `/api/mensa` endpoint.
- Mensa configuration lives under the `Mensa` section in appsettings. Keep the API key out of git.
- JWT creation lives in Infrastructure behind `IJwtService`.

Authentication rules:

- Tokens are JWT Bearer tokens.
- Browser token persistence must stay in memory. Do not use `localStorage` or `sessionStorage` for auth tokens.
- Registration email-domain behavior is security-sensitive. The current implementation accepts `@dhbw-loerrach.de`; product docs mention student DHBW email scope. Do not change this rule casually without tests and doc updates.
- Protect user-specific or admin endpoints when implementing new endpoints. Use `[AllowAnonymous]` only for endpoints that must be public, such as login and registration.

## Frontend Architecture Rules

Use Angular 21 patterns already present in the repo:

- Use standalone components only. Do not add NgModules.
- Use `inject()` for dependencies instead of constructor injection.
- Use `ChangeDetectionStrategy.OnPush` on components.
- Use signals for component-local state where practical.
- Use functional guards (`CanActivateFn`) and functional interceptors (`HttpInterceptorFn`).
- Add new lazy-loaded feature pages through `src/app/app.routes.ts` with `loadComponent`.
- Keep one route-level feature folder per feature under `src/app/features/`.
- Keep shared presentational components under `src/app/shared/ui/`.
- Keep singleton services, API clients, guards, interceptors, and models under `src/app/core/`.

Component and service rules:

- Components handle rendering, interaction, and lightweight UI state only.
- Components must not call `HttpClient` directly. Use services in `core/services/` for backend calls.
- Put API response/request interfaces in `core/models/`.
- Avoid `any`. Model data explicitly.
- Keep auth token handling centralized in `core/services/auth.ts` and `core/interceptors/auth-token-interceptor.ts`.
- Do not store tokens in browser storage.
- Use separate `.ts`, `.html`, and `.scss` files to match existing component layout.
- Preserve the existing private/protected member style, including underscore-prefixed private state where nearby files use it.

Styling and UX rules:

- Match the existing application shell and feature-page visual language.
- Prefer clear operational UI over marketing-style landing pages.
- Use accessible labels, focus states, and semantic HTML for forms and navigation.
- Keep layouts responsive and avoid text overflow on small screens.
- Avoid introducing a new design system unless explicitly requested.

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

Frontend API proxy:

- `CampusConnect/frontend/proxy.conf.json` sends `/api` requests to `http://localhost:5135`.
- Start the API before using API-backed frontend pages locally.

Known build note:

- If backend build fails because `CampusConnect.API/bin/...` files are locked, stop the running API process and rebuild.

## Testing Expectations

- For backend logic changes, add or update xUnit tests in the nearest test project.
- For frontend component/service behavior changes, add or update the nearest `.spec.ts` file.
- Run the smallest relevant test/build command that validates the change.
- Before browser-based validation, restart the existing local backend and frontend dev servers instead of starting duplicate servers on alternate ports. Use the normal API proxy target on `http://localhost:5135` and the normal Angular dev server on `http://localhost:4200`, stopping any stale listener first when needed.
- If tests cannot be run, explain why in the final response.
- Do not fix unrelated failing tests unless the user asks.

## API Conventions

- Public auth endpoints: `POST /api/auth/register`, `POST /api/auth/login`.
- User profile endpoint: `GET /api/auth/me`.
- Feature endpoints follow `/api/<resource>` naming.
- Authenticated requests use `Authorization: Bearer <token>`.
- Return validation failures with a clear `{ error = ... }` shape where controllers already follow that pattern.
- Keep API docs in `CampusConnect/docs/api.md` aligned when endpoints are added, removed, or changed.

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

## Before Starting Any Task

1. Identify whether the task touches frontend, backend, docs, CI, or cross-cutting behavior.
2. Read the nearest source files and tests before editing.
3. Check the docs listed above when changing product behavior or API contracts.
4. Preserve the current architecture boundaries.
5. Make the smallest complete change that satisfies the request.
6. Validate with relevant builds/tests when feasible.
7. Summarize changed files and validation results clearly.

## Things Copilot Should Not Do

- Do not put database calls in controllers.
- Do not put business rules in Angular components.
- Do not call external APIs directly from the frontend.
- Do not store JWTs in browser storage.
- Do not add packages, migrations, Docker services, or CI jobs without a clear task requiring them.
- Do not overwrite generated or user-made changes outside the task scope.
- Do not invent endpoints that are not represented in code or docs without updating both.
- Do not present placeholder Docker/CI files as finished infrastructure.
