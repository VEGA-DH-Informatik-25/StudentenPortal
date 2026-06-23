# Project Overview

CampusConnect is a student portal for DHBW Loerrach. The application brings together authentication, feed communication, mensa menus, timetable and exam views, grade tracking, campus groups, contacts, profile management, and admin user/course management.

## Application Location

- Frontend: `CampusConnect/frontend`
- Backend: `CampusConnect/backend`
- Central documentation: `CampusConnect/docs`
- Protected MVP requirements: `../../prd-mvp.md`

## Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 21 |
| Backend | ASP.NET Core 10 |
| Database | SQLite with Entity Framework Core migrations |
| Authentication | JWT Bearer API clients and HttpOnly browser cookie sessions |
| CI | GitHub Actions for backend restore/build/test and frontend install/test/build |
| Containerization | `docker-compose.yml` exists as a placeholder and is not production-ready |

## Local Development

Install frontend dependencies:

```powershell
cd CampusConnect/frontend
npm install
```

Restore backend packages:

```powershell
cd CampusConnect/backend
dotnet restore .\CampusConnect.slnx
```

Run the API:

```powershell
cd CampusConnect/backend
dotnet run --project .\CampusConnect.API\CampusConnect.API.csproj
```

Run the frontend:

```powershell
cd CampusConnect/frontend
npm start
```

Expected local URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5135`
- Swagger: `http://localhost:5135/swagger`

## Documentation Map

- [Architecture](architecture.md)
- [API Reference](api.md)
- [Frontend Notes](frontend.md)
- [Testing](testing.md)
- [Contributing](contributing.md)
- [Roles And Responsibilities](roles.md)
- [Project Description](product/projektbeschreibung.md)
- [MVP PRD](../../prd-mvp.md)

Live code and configuration are authoritative for implemented behavior. Update the affected central document in this folder when API contracts, setup, architecture, testing conventions, or project scope change.
