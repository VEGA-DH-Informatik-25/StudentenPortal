# Code Review Findings

Review date: 2026-05-04  
Scope: CampusConnect backend, frontend, configuration, docs, and validation commands.

> **Historical review snapshot:** This file records findings as observed on 2026-05-04. It is not the current source of truth for architecture, test counts, or implementation status, and an item must not be treated as still open without rechecking the current code. Current technical behavior is documented in `architecture.md`, `api.md`, and `testing.md`.

## Current Verification Note

On 2026-06-11, the full backend test run passed with 102 tests, the frontend passed 88 tests across 31 test files, and the production frontend build passed. This newer validation supersedes the counts and locked-file result in the historical validation section below. The individual security and product findings have not all been re-audited; their original wording is retained as review evidence rather than an active backlog.

This report lists verified weaknesses, bugs, and risk areas found during a code review. Severity reflects likely impact if the project were exposed beyond a trusted local development/demo setting.

## Critical

### 1. Contact search exposes broad personal data to every authenticated user

- Location: [ContactsService.cs](../backend/CampusConnect.Application/Features/Contacts/ContactsService.cs#L20), [ContactsController.cs](../backend/CampusConnect.API/Controllers/ContactsController.cs#L20)
- Evidence: `SearchAsync` loads all users, excludes only the current user, takes the first 50 matches, and returns email, phone number, location, profile note, course, role, and study metadata.
- Risk: Any authenticated account can enumerate personal profile data across the portal. This is a privacy/GDPR risk, especially because optional contact fields are shown without an explicit visibility/consent model.
- Recommendation: Add a visibility policy. At minimum, limit results to shared courses/groups or explicitly public profiles, separate public directory fields from private profile fields, and add authorization tests for contact search boundaries.

### 2. Group managers can list all user accounts from group settings

- Location: [GroupsService.cs](../backend/CampusConnect.Application/Features/Groups/GroupsService.cs#L208), [GroupsService.cs](../backend/CampusConnect.Application/Features/Groups/GroupsService.cs#L250)
- Evidence: `ToSettingsDetailsAsync` lists every user account with display name, email, role, course, assignment state, and permission. `CanManage` permits admins, lecturers assigned to course groups, group owners, and members with `Manage` permission.
- Risk: A non-admin social group owner or delegated manager can receive a tenant-wide account directory through a single group settings call.
- Recommendation: Return only users already assigned to the group plus a carefully filtered assignable list. Restrict global account search to admins, or require a scoped lookup endpoint with server-side query limits and privacy rules.

## High

### 3. Registration and login do not validate null or weak password input

- Location: [AuthDtos.cs](../backend/CampusConnect.API/DTOs/Auth/AuthDtos.cs#L3), [AuthService.cs](../backend/CampusConnect.Application/Features/Auth/AuthService.cs#L23), [AuthService.cs](../backend/CampusConnect.Application/Features/Auth/AuthService.cs#L41), [AuthService.cs](../backend/CampusConnect.Application/Features/Auth/AuthService.cs#L56)
- Evidence: API records use non-nullable strings but no `[Required]`, minimum length, or service-level null guards. The service calls `cmd.Email.Trim()` and hashes/verifies `cmd.Password` directly.
- Risk: Empty or very weak passwords can be registered. JSON `null` for email/password can produce unhandled exceptions instead of validation responses.
- Recommendation: Add request DTO validation and service guards for email, password, display name, and course. Enforce a minimum password policy and return controlled `400` responses for malformed auth requests.

### 4. Login endpoint has no rate limiting

- Location: [AuthController.cs](../backend/CampusConnect.API/Controllers/AuthController.cs), [Program.cs](../backend/CampusConnect.API/Program.cs)
- Evidence: The public login endpoint accepts unlimited attempts; no ASP.NET rate limiter or throttling middleware is configured.
- Risk: Brute-force attacks and credential stuffing are possible.
- Recommendation: Add IP/user/email based rate limiting for login, with conservative limits and tests for throttling behavior.

### 5. JWT signing secret only has a non-empty check

- Location: [Program.cs](../backend/CampusConnect.API/Program.cs#L44), [Program.cs](../backend/CampusConnect.API/Program.cs#L59)
- Evidence: Startup rejects a missing `Jwt:Secret`, but accepts any non-whitespace string as the HMAC signing key.
- Risk: A short or low-entropy secret can make bearer tokens forgeable.
- Recommendation: Enforce at least 32 bytes of secret material for HS256, fail startup with a clear message when it is weaker, and add a startup/configuration test for this rule.

### 6. Development demo password is committed and used for all seeded accounts

- Location: [appsettings.Development.json](../backend/CampusConnect.API/appsettings.Development.json#L8), [appsettings.Development.json](../backend/CampusConnect.API/appsettings.Development.json#L10), [DemoDataOptions.cs](../backend/CampusConnect.Infrastructure/Persistence/DemoDataOptions.cs#L9), [DevelopmentDemoDataSeeder.cs](../backend/CampusConnect.Infrastructure/Persistence/DevelopmentDemoDataSeeder.cs#L76)
- Evidence: Development config and option defaults contain `CampusDemo2026!`, and the seeder hashes that same password for every demo user.
- Risk: If development mode or demo data reaches a shared server, every seeded account has a public password. The value is also present in repository history.
- Recommendation: Keep demo data disabled by default unless explicitly enabled, require `DemoData:Password` from user secrets/environment for shared deployments, and avoid a committed password default.

### 7. Feed pagination is applied before authorization filtering

- Location: [FeedService.cs](../backend/CampusConnect.Application/Features/Feed/FeedService.cs#L28), [EntityFeedRepository.cs](../backend/CampusConnect.Infrastructure/Repositories/EntityFeedRepository.cs#L10), [EntityFeedRepository.cs](../backend/CampusConnect.Infrastructure/Repositories/EntityFeedRepository.cs#L14)
- Evidence: The repository returns the global page of newest posts, then `FeedService.GetFeedAsync` filters out posts the current user cannot read.
- Risk: A user can receive fewer than `pageSize` posts, or an empty page, even when accessible posts exist later in the global feed. Pagination will be inconsistent and can hide valid posts.
- Recommendation: Move authorization-aware filtering into the query/repository layer, or over-fetch with a stable cursor until enough visible posts are collected. Add tests with mixed visible/invisible posts.

## Medium

### 8. Feed `page` and `pageSize` query values are not validated

- Location: [FeedController.cs](../backend/CampusConnect.API/Controllers/FeedController.cs#L15), [EntityFeedRepository.cs](../backend/CampusConnect.Infrastructure/Repositories/EntityFeedRepository.cs#L14)
- Evidence: Client-supplied `page` and `pageSize` flow directly into `Skip((page - 1) * pageSize)` and `Take(pageSize)`.
- Risk: Negative values can produce provider-specific failures, and huge page sizes can force expensive database reads and serialization.
- Recommendation: Clamp or reject invalid values, for example `page >= 1` and `1 <= pageSize <= 100`, and add API tests for invalid values.

### 9. Group detail page only shows posts from the first global feed page

- Location: [group-detail-page.ts](../frontend/src/app/features/groups/group-detail-page/group-detail-page.ts#L276), [feed.ts](../frontend/src/app/core/services/feed.ts#L10)
- Evidence: The page calls `getFeed()` with default page 1 and then filters those 20 global posts by group id.
- Risk: Older posts for a group disappear from the group detail page if they are not in the first global feed page.
- Recommendation: Add a backend endpoint for group-scoped posts or support a `groupId` filter on `/api/feed`, then paginate group posts independently.

### 10. Feed UI has no way to load additional pages

- Location: [feed-page.ts](../frontend/src/app/features/feed/feed-page/feed-page.ts#L68), [feed.ts](../frontend/src/app/core/services/feed.ts#L10)
- Evidence: The service accepts a `page` argument, but the page component always calls `getFeed()` once with the default page.
- Risk: Users can only see the first 20 visible posts.
- Recommendation: Add cursor/page state and a load-more or infinite-scroll control. Prefer a cursor once backend authorization-aware pagination is fixed.

### 11. API accepts content lengths that the UI does not, and some limits are only enforced by persistence

- Location: [FeedDtos.cs](../backend/CampusConnect.API/DTOs/Feed/FeedDtos.cs#L3), [FeedDtos.cs](../backend/CampusConnect.API/DTOs/Feed/FeedDtos.cs#L5), [FeedService.cs](../backend/CampusConnect.Application/Features/Feed/FeedService.cs#L53), [FeedService.cs](../backend/CampusConnect.Application/Features/Feed/FeedService.cs#L89), [feed-page.html](../frontend/src/app/features/feed/feed-page/feed-page.html#L86), [feed-page.html](../frontend/src/app/features/feed/feed-page/feed-page.html#L226)
- Evidence: The UI limits posts to 600 characters and comments to 360, but the backend only checks non-empty content. Feed post storage allows 4000 characters, and comments are stored inside serialized JSON without equivalent per-comment validation.
- Risk: Direct API callers can bypass UI limits, create unexpectedly large content, or trigger database/persistence errors instead of friendly validation responses.
- Recommendation: Enforce the same max lengths in backend DTO/service validation and add tests for boundary values.

### 12. Calendar and grades inputs can exceed database limits

- Location: [CalendarDtos.cs](../backend/CampusConnect.API/DTOs/Calendar/CalendarDtos.cs#L3), [CalendarService.cs](../backend/CampusConnect.Application/Features/Calendar/CalendarService.cs#L21), [GradeDtos.cs](../backend/CampusConnect.API/DTOs/Grades/GradeDtos.cs#L3), [GradesService.cs](../backend/CampusConnect.Application/Features/Grades/GradesService.cs#L125), [CampusConnectDbContext.cs](../backend/CampusConnect.Infrastructure/Persistence/CampusConnectDbContext.cs#L104), [CampusConnectDbContext.cs](../backend/CampusConnect.Infrastructure/Persistence/CampusConnectDbContext.cs#L113)
- Evidence: Calendar and manual grade fields check only emptiness or grade range. Database mappings cap module names, locations, and notes, but service validation does not mirror those limits.
- Risk: Oversized payloads can fail at the persistence layer, producing 500 responses or provider-specific behavior instead of controlled validation errors.
- Recommendation: Add explicit service/API validation for module names, locations, notes, and reasonable ECTS bounds.

### 13. Delete operations report success even when nothing was deleted

- Location: [GradesService.cs](../backend/CampusConnect.Application/Features/Grades/GradesService.cs#L105), [CalendarService.cs](../backend/CampusConnect.Application/Features/Calendar/CalendarService.cs#L38), [GradesController.cs](../backend/CampusConnect.API/Controllers/GradesController.cs#L70), [CalendarController.cs](../backend/CampusConnect.API/Controllers/CalendarController.cs#L48)
- Evidence: Grade/exam repositories silently return when the item is missing or belongs to another user, while services/controllers always return success/no-content.
- Risk: The UI removes an item locally even if the server did not delete it. API clients cannot distinguish success from stale ids or ownership mismatch.
- Recommendation: Have repositories return a boolean or result enum, then map missing/unauthorized cases to explicit API responses.

### 14. Cross-user and privacy authorization tests are thin

- Location: [ApiAuthorizationTests.cs](../backend/CampusConnect.API.Tests/ApiAuthorizationTests.cs#L9), [ApiAuthorizationTests.cs](../backend/CampusConnect.API.Tests/ApiAuthorizationTests.cs#L58)
- Evidence: Tests cover unauthenticated access, admin route forbidden for a student token, and a simple current-user grades response, but do not cover cross-user data boundaries, contacts privacy, group settings account visibility, or feed filtering with mixed groups.
- Risk: The most sensitive authorization behavior can regress without a failing test.
- Recommendation: Add integration tests for PII visibility, group manager account scope, feed visibility, delete ownership, and invalid input boundaries.

## Low

### 15. Timetable course history is stored in `localStorage`

- Location: [timetable.ts](../frontend/src/app/core/services/timetable.ts#L30), [timetable.ts](../frontend/src/app/core/services/timetable.ts#L39), [timetable.ts](../frontend/src/app/core/services/timetable.ts#L41)
- Evidence: Selected course and recent course history persist across browser sessions.
- Risk: On shared devices, another person can infer course affiliation/history even after the in-memory auth session expires.
- Recommendation: Consider `sessionStorage`, an explicit clear action, or documenting this as a local preference separate from auth state.

### 16. Docker Compose is still a placeholder

- Location: [docker-compose.yml](../docker-compose.yml#L1)
- Evidence: The file contains only a TODO service block.
- Risk: The project cannot be started as a composed stack despite having a compose file.
- Recommendation: Either implement backend/frontend/database services or remove/label the file clearly as not supported.

### 17. Backend validation is hard to run while the API is active

- Location: [docs/testing.md](testing.md#L7)
- Evidence: `dotnet test .\CampusConnect.slnx` compiled and ran 47 tests successfully, but the full command exited with MSBuild copy errors because `CampusConnect.API (PID 19472)` locked API DLLs.
- Risk: Developers may see failing validation even though tests pass, which slows reviews and CI-like local checks.
- Recommendation: Document the stop-server step next to the test command or use a workflow that avoids rebuilding the running API output directory.

## Historical Validation Performed On 2026-05-04

- `dotnet test .\CampusConnect.slnx` from `CampusConnect/backend`: 47 tests passed, but command exited failed because a running `CampusConnect.API` process locked copied DLLs.
- `npm run build` from `CampusConnect/frontend`: passed.
- `npm test -- --watch=false` from `CampusConnect/frontend`: 31 test files passed, 76 tests passed.
- VS Code diagnostics via `get_errors`: no errors reported for backend or frontend folders.

## Notes On Items Excluded

- The frontend keeps JWTs only in memory. That is documented in the architecture and README, so it was treated as an intentional security tradeoff rather than a bug.
- Angular `HttpClient` request subscriptions complete after one response. The component subscriptions reviewed here are not automatically memory leaks solely because they lack manual unsubscribe logic.
- Earlier repository notes mentioned missing authorization on grades/calendar/mensa; current controllers are decorated with `[Authorize]`, so those older findings are resolved.
