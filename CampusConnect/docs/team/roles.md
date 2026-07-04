# Rollen und Verantwortlichkeiten

| Rolle | Aufgaben | Hauptbereiche |
|---|---|---|
| Projektleitung / Full-Stack | Architekturentscheidungen, Code-Reviews, Sprint-Planung, Meilenstein-Präsentationen | `frontend/` und `backend/` |
| Backend-Entwicklung | REST-API, Datenbankschema, Authentifizierung, Geschäftslogik | `backend/CampusConnect.API`, `backend/CampusConnect.Application` |
| Frontend-Entwicklung | Angular-Komponenten, Routing, UI/UX, API-Integration | `frontend/src` |
| QA und DevOps | Testkonzept, CI/CD-Pipeline, Deployment, technische Dokumentation | `.github/`, alle Schichten |

## Branch-Zuständigkeiten

| Rolle | Primäre Review-Verantwortung |
|---|---|
| Projektleitung / Full-Stack | Alle Branches – abschließende Freigabe vor dem Merge in `main` |
| Backend-Entwicklung | `feature/*`- und `fix/*`-Branches mit Änderungen in `backend/` |
| Frontend-Entwicklung | `feature/*`- und `fix/*`-Branches mit Änderungen in `frontend/` |
| QA und DevOps | `test/*`-, `chore/*`- und `.github/`-Workflow-Branches |
