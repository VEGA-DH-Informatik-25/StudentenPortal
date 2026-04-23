# GitHub Copilot Instructions – CampusConnect

## Project overview

CampusConnect is a student portal for DHBW Lörrach. It provides a news feed,
mensa menu (via the SWFR XML API), exam calendar, grade tracker, and learning
group matching. The application is a web app only, no mobile app.

## Tech stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21 with standalone components, SCSS, Angular Router |
| Backend | ASP.NET Core 9 Web API, Clean Architecture |
| Database | PostgreSQL via Entity Framework Core 9 |
| Authentication | JWT Bearer tokens |
| Containerization | Docker, docker-compose |
| CI/CD | GitHub Actions |
| Testing | xUnit (backend), Jasmine + Karma (frontend), Cypress (E2E) |

## Repository structure

```
CampusConnect/
├── .github/workflows/      # GitHub Actions pipelines
├── docs/                   # Architecture, API docs, role definitions
├── frontend/               # Angular application
│   └── src/app/
│       ├── core/           # Services, guards, interceptors – singleton scope
│       ├── features/       # One folder per feature (feed, mensa, calendar, ...)
│       ├── layout/         # Shell, navbar, sidebar
│       └── shared/         # Reusable UI components
└── backend/
    ├── CampusConnect.API/           # Controllers, middleware, DTOs
    ├── CampusConnect.Application/   # Use cases, interfaces, CQRS handlers
    ├── CampusConnect.Domain/        # Entities, value objects, domain interfaces
    ├── CampusConnect.Infrastructure/# EF Core, repositories, external services
    ├── CampusConnect.API.Tests/
    └── CampusConnect.Application.Tests/
```

## Architecture rules

### Clean Architecture – dependency direction

Domain has no dependencies.
Application depends only on Domain.
Infrastructure depends on Application and Domain.
API depends on Application. API never imports Infrastructure directly.
Never reference a higher layer from a lower layer.

### Frontend – Thin Frontend principle

All business logic lives in the backend.
Angular components only handle rendering and user interaction.
Services in `core/services/` are the only place that call the HTTP API.
Components never call HttpClient directly.
Use Angular signals or RxJS observables to propagate state.
Never put if/else business rules inside a component.

### Backend – CQRS pattern

Use MediatR for all request handling.
Every feature in `CampusConnect.Application/Features/` follows this structure:
- `Queries/Get[Resource]/Get[Resource]Query.cs`
- `Queries/Get[Resource]/Get[Resource]QueryHandler.cs`
- `Commands/Create[Resource]/Create[Resource]Command.cs`
- `Commands/Create[Resource]/Create[Resource]CommandHandler.cs`

Controllers are thin. They validate the request, call MediatR, return the result.
No logic in controllers beyond input validation and mapping to a command or query.

## Coding conventions

### TypeScript / Angular

- Use standalone components everywhere, no NgModules.
- Use the `inject()` function instead of constructor injection.
- Use `OnPush` change detection on all components.
- File names: `feature-name.component.ts`, `feature-name.service.ts`.
- Interfaces for all API response types, placed in `core/models/`.
- No `any` types.
- Prefix private class members with an underscore: `_isLoading`.

### C# / ASP.NET Core

- Use file-scoped namespaces.
- Use primary constructors where it simplifies the code.
- Use `async`/`await` throughout. No `.Result` or `.Wait()`.
- Return `IActionResult` or `ActionResult<T>` from controllers.
- Use `record` types for DTOs, commands, and queries.
- Use `Result<T>` pattern for application layer responses, never throw exceptions
  for expected domain errors.
- Name controllers: `[Resource]Controller.cs`.
- Name handlers: `Get[Resource]QueryHandler.cs`, `Create[Resource]CommandHandler.cs`.

### General

- No commented-out code in commits.
- No hardcoded strings for configuration values. Use `appsettings.json` and
  the options pattern.
- All endpoints require JWT authentication unless explicitly marked with
  `[AllowAnonymous]`.

## External API – SWFR Mensa

The SWFR Mensa API returns XML, not JSON.
Endpoint: `https://www.swfr.de/apispeiseplan?type=98&tx_speiseplan_pi1[apiKey]=KEY&tx_speiseplan_pi1[ort]=671&tx_speiseplan_pi1[tage]=5`
Ort ID for Mensa Lörrach is `671`.
The API key must be stored in `appsettings.json` under `Mensa:ApiKey` and never
committed to git.
All calls to the SWFR API go through `CampusConnect.Infrastructure/ExternalServices/MensaApiClient.cs`.
The frontend never calls the SWFR API directly. It calls `/api/mensa` on the backend.

## Authentication flow

1. User registers with a valid `@student.dhbw-loerrach.de` email address.
2. On login the backend issues a short-lived JWT (15 minutes) and a refresh token.
3. The Angular `AuthInterceptor` in `core/interceptors/auth-token.interceptor.ts`
   attaches the Bearer token to every outgoing request automatically.
4. On 401 responses the interceptor calls the refresh endpoint and retries once.
5. Tokens are stored in memory only, never in localStorage.

## What Copilot should NOT do

- Do not generate code that puts logic inside Angular components.
- Do not generate code that calls the database from a controller.
- Do not suggest localStorage for token storage.
- Do not add NuGet or npm packages without being explicitly asked.
- Do not generate migration files unless explicitly asked.
- Do not write TODO comments as stubs. Either implement the feature properly
  or leave the method empty with a single-line XML doc comment describing
  what it should do.
