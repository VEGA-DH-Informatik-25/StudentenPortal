# Projektstand, Rollen und erledigte Aufgaben

Stand: 2026-05-20

Dieses Dokument macht den aktuellen Projektstand im Repository nachvollziehbar. Es ergänzt das PRD in `docs/prd-mvp.md` und die UI-Beschreibung in `docs/ui-prototype.md`.

## 1. Rollenverteilung

| Rolle | Teammitglied | Hauptverantwortung | Repository-Bereiche |
|---|---|---|---|
| Projektleitung / Full-Stack | Jakob | Architekturentscheidungen, Code-Reviews, Sprint-Planung, Meilenstein-Präsentationen | `frontend/`, `backend/`, Dokumentation |
| Backend-Entwicklung | Theo | REST-API, Datenbankschema, Authentifizierung, Geschäftslogik | `backend/CampusConnect.API`, `backend/CampusConnect.Application`, `backend/CampusConnect.Infrastructure` |
| Frontend-Entwicklung | Simon | Angular-Komponenten, Routing, UI/UX, API-Integration | `frontend/src/app` |
| QA und GitHub/DevOps | Julius | Testkonzept, CI/CD-Pipeline, technische Dokumentation, Repository-Pflege | `.github/`, Tests, Dokumentation |

## 2. Aktueller technischer Stand

| Bereich | Stand |
|---|---|
| Frontend | Angular 21 mit Standalone Components, Lazy Routes, Signals, Guards und Interceptors |
| Backend | ASP.NET Core 10 Web API mit Clean-Architecture-Projektstruktur |
| Persistenz | SQLite über EF Core 10, inklusive Migrationen und Development-Demo-Seeding |
| Authentifizierung | JWT Bearer für API-Clients, HttpOnly-Cookie für Browser-Sitzungen |
| Externe Daten | Mensa über SWFR-XML-API; Stundenplan über serverseitige iCal-Integration; Studienplan-PDF-Parsing im Backend |
| Tests | xUnit-Testprojekte im Backend; Angular/Vitest-Setup im Frontend |
| CI | GitHub Actions Workflow mit Backend- und Frontend-Jobs vorhanden |
| Docker | `docker-compose.yml` ist weiterhin ein Platzhalter |

## 3. Erledigte Aufgaben im Repository

### Dokumentation und Konzept

- Projektbeschreibung mit Zielgruppe, Nutzen, Kernfunktionen und Abgrenzung erstellt.
- Architektur-Dokumentation für Frontend, Backend, Persistenz, externe APIs und Auth-Flow erstellt.
- API-Referenz mit implementierten Endpunkten erstellt.
- Marktanalyse erstellt.
- Demo-Daten dokumentiert.
- Rollen, Review-Zuständigkeiten und Beitragsregeln dokumentiert.
- PRD/MoSCoW-Anforderungen für den MVP aktualisiert.
- UI-Prototyp und Seitenstruktur dokumentiert.

### Backend

- Solution mit Domain, Application, Infrastructure, API und Testprojekten eingerichtet.
- Authentifizierung mit Registrierung, Login, Logout und Profilabruf implementiert.
- Rollenmodell `Student`, `Lecturer`, `Admin` implementiert.
- Kurskatalog und Admin-Kursverwaltung implementiert.
- Admin-Benutzerverwaltung für Listen, Rolle ändern, Kurs ändern und Löschen implementiert.
- Gruppen-, Feed-, Kalender-, Noten-, Mensa-, Stundenplan-, Kontakte- und Kurs-Endpunkte implementiert.
- SQLite-Persistenz mit EF Core und Migrationen eingerichtet.
- Development-Demo-Seeder für Präsentationsdaten implementiert.
- SWFR-Mensa-Client, DHBW-Stundenplanservice und Studienplan-Parser im Backend gekapselt.

### Frontend

- Angular-App mit geschützter App-Shell und Login-/Registrierungsseite eingerichtet.
- Lazy-loaded Feature-Seiten für Feed, Mensa, Prüfungen, Stundenplan, Noten, Gruppen, Kontakte, Profil und Admin eingerichtet.
- Navbar mit Hauptnavigation, Profilmenü und rollenabhängigem Admin-Link umgesetzt.
- Core-Services, Modelle, Guards und Interceptors für API-Kommunikation umgesetzt.
- UI für Feed, Gruppen, Gruppeneinstellungen, Mensa, Kalender, Stundenplan, Noten, Kontakte, Profil und Admin erstellt.
- Frontend-Tests für mehrere Feature-Seiten und Services angelegt.

### Qualitätssicherung

- Backend-Testprojekte mit xUnit vorhanden.
- Frontend-Testsetup über Angular CLI Unit-Test Builder und Vitest vorhanden.
- CI-Workflow für Backend Restore/Build/Test sowie Frontend Install/Test/Build vorhanden.
- API-Dokumentation und Swagger/OpenAPI im Development-Modus vorhanden.

## 4. Bekannte offene Punkte

| Bereich | Offener Punkt | Priorität |
|---|---|---|
| Admin/Nutzer | Direkte Nutzeranlage mit Initialpasswort fehlt | Should-have |
| Admin/Nutzer | Passwort-Reset und verpflichtender Erstlogin-Passwortwechsel fehlen | Should-have |
| Rollen | Separate Verwaltungsrolle ist noch nicht definiert/implementiert | Should-have |
| Onboarding | Konzept vorhanden, aber UI und Backend-Statusfeld fehlen | Should-have |
| Gruppenmoderation | `requiresApproval` ist modelliert, vollständiger Freigabeprozess fehlt | Should-have |
| UI-Dokumentation | Separate grafische Mockup-Dateien sind nicht im Repository abgelegt | Could-have |
| Docker | Compose-Datei ist Platzhalter und nicht produktionsbereit | Won't-have für aktuellen Meilenstein |

## 5. Nachweis im Repository

| Nachweis | Ort |
|---|---|
| Must-have-Anforderungen | `docs/prd-mvp.md` |
| UI-/Prototypbeschreibung | `docs/ui-prototype.md` |
| API-Stand | `docs/api.md` |
| Architektur | `docs/architecture.md` |
| Demo-Daten | `docs/demo-data.md` |
| Rollen und Review-Verantwortung | `docs/roles.md` und dieses Dokument |
| Frontend-Routen | `frontend/src/app/app.routes.ts` |
| Backend-Controller | `backend/CampusConnect.API/Controllers/` |
| Tests | `backend/*Tests/` und `frontend/src/**/*.spec.ts` |

## 6. Hinweis zur Aufgabenplanung

Die Aufgabenverteilung für die laufende Arbeit kann zusätzlich im GitHub-Projects-Board gepflegt werden. Dieses Repository dokumentiert den nachvollziehbaren Stand über Commits, Testdateien, Feature-Dateien und die oben genannten Markdown-Dokumente.
