# CampusConnect

CampusConnect ist ein Studierendenportal für die DHBW Lörrach. Es bietet einen News-Feed, den Mensa-Speiseplan, einen Prüfungskalender/Stundenplan, eine Notenverwaltung, eine Lerngruppen-Matching-Funktion sowie ein Adressbuch mit wichtigen Kontakten (z. B. Professoren, Sekretariat, Studiengangsleitung).

---

## Inhaltsverzeichnis

1. [Tech-Stack](#tech-stack)
2. [Erste Schritte](#erste-schritte)
3. [Architektur](#architektur)
4. [API-Referenz](#api-referenz)
5. [Team und Rollen](#team-und-rollen)
6. [Branch-Zuständigkeiten](#branch-zuständigkeiten)
7. [Beitragen](#beitragen)
8. [Links](#links)

---

## Tech-Stack

| Schicht | Technologie |
|---|---|
| Frontend | Angular 21 |
| Backend | ASP.NET Core 10 |
| Datenbank | SQLite |
| Authentifizierung | JWT |
| Containerisierung | Docker Compose *(Platzhalter, noch nicht produktiv eingerichtet)* |
| CI/CD | GitHub Actions |

---

## Erste Schritte

1. Repository klonen.
2. Voraussetzungen installieren: Node.js und .NET 10 SDK.
3. Im Ordner `frontend/` den Befehl `npm install` ausführen.
4. Im Ordner `backend/` den Befehl `dotnet restore .\CampusConnect.slnx` ausführen.
5. Die API mit `dotnet run --project .\CampusConnect.API\CampusConnect.API.csproj` starten.
6. Das Frontend mit `npm start` im Ordner `frontend/` starten.
7. Im Browser `http://localhost:4200` aufrufen.

---

## Architektur

### Systemüberblick

CampusConnect verwendet eine Drei-Schichten-Architektur: eine Angular-Single-Page-Application als Präsentationsschicht, eine ASP.NET-Core-REST-API als Geschäfts- und Datenzugriffsschicht sowie eine SQLite-Datenbank als aktuelle Persistenzschicht. Jede Schicht kommuniziert ausschließlich mit der benachbarten Schicht.

### Frontend-Architektur

Das Frontend basiert auf **Angular 21** und verwendet ausschließlich eigenständige Komponenten (Standalone Components), sodass NgModules nicht benötigt werden. Die wichtigsten Architekturentscheidungen:

- **Signals** (`signal()`, `computed()`) als Standard-Reaktivitätsmodell.
- **Zoneless-ready**: `provideZonelessChangeDetection()`.
- **Lazy Loading**: Alle Feature-Bereiche werden über `loadComponent` erst bei Bedarf geladen.
- **Functional Guards & Interceptors**: `CanActivateFn` und `HttpInterceptorFn`, registriert via `provideHttpClient(withInterceptors([...]))`.
- **`withComponentInputBinding()`**: Route-Parameter direkt an Component-Inputs bindbar.

### Backend-Architektur

Das Backend folgt der **Clean Architecture** mit vier Schichten:

| Schicht | Projekt | Abhängigkeit |
|---|---|---|
| Domain | `CampusConnect.Domain` | *(keine)* |
| Application | `CampusConnect.Application` | Domain |
| Infrastructure | `CampusConnect.Infrastructure` | Application |
| API | `CampusConnect.API` | Application |

Abhängigkeiten zeigen stets nach innen zur Domain-Schicht. Infrastructure und API implementieren Interfaces, die in der Application-Schicht definiert sind.

### Externe APIs

Die SWFR-Mensa-XML-API ist unter `swfr.de/apispeiseplan` verfügbar und erfordert einen API-Schlüssel von SWFR. Um CORS-Probleme zu vermeiden und den Schlüssel geheim zu halten, leitet das Backend alle Anfragen an diesen Dienst weiter, bevor die aufbereiteten Daten an das Angular-Frontend übergeben werden.

### Authentifizierungsablauf

CampusConnect verwendet zustandslose JWT-basierte Authentifizierung:

1. Der Benutzer sendet seine Anmeldedaten an `POST /api/auth/login`.
2. Das Backend prüft die Anmeldedaten und stellt ein signiertes JWT aus.
3. Das Angular-Frontend speichert das Token **ausschließlich im Arbeitsspeicher** (nicht in localStorage).
4. Jede folgende Anfrage an einen geschützten Endpunkt enthält den Header `Authorization: Bearer <token>`.
5. Das Backend prüft Signatur und Ablaufzeit des Tokens bei jeder Anfrage.

---

## API-Referenz

### Implementierte Endpunkte

| Methode | Endpunkt | Beschreibung | Authentifizierung |
|---|---|---|---|
| POST | `/api/auth/register` | Registrierung mit Hochschul-E-Mail-Adresse | Nein |
| POST | `/api/auth/login` | Anmeldung und JWT-Empfang | Nein |
| GET | `/api/auth/me` | Aktuelles Benutzerprofil abrufen | Ja |
| PUT | `/api/auth/me` | Eigenes Benutzerprofil aktualisieren | Ja |
| GET | `/api/courses` | Aktive Kursauswahl für Registrierung und Profil abrufen | Nein |
| GET | `/api/admin/courses` | Kurse in der Administration auflisten | Ja, Admin |
| POST | `/api/admin/courses` | Neuen Kurs mit Code, Studiengang und Semester anlegen | Ja, Admin |
| GET | `/api/admin/users` | Benutzer in der Administration auflisten | Ja, Admin |
| PATCH | `/api/admin/users/{id}/role` | Rolle eines Benutzers ändern | Ja, Admin |
| PATCH | `/api/admin/users/{id}/course` | Kurszuordnung eines Benutzers ändern | Ja, Admin |
| DELETE | `/api/admin/users/{id}` | Benutzer löschen | Ja, Admin |
| GET | `/api/feed` | Paginierten News-Feed mit Gruppenkontext abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag in einer Gruppe erstellen | Ja |
| DELETE | `/api/feed/{id}` | Eigenen Beitrag löschen | Ja |
| POST | `/api/feed/{id}/comments` | Kommentar unter einem Beitrag erstellen | Ja |
| DELETE | `/api/feed/{postId}/comments/{commentId}` | Eigenen Kommentar löschen | Ja |
| POST | `/api/feed/{id}/reactions` | Emoji-Reaktion an einem Beitrag umschalten | Ja |
| GET | `/api/mensa` | Mensa-Speiseplan für die aktuelle Woche abrufen | Ja |
| GET | `/api/calendar` | Prüfungskalender-Einträge abrufen | Ja |
| POST | `/api/calendar` | Persönlichen Prüfungseintrag hinzufügen | Ja |
| DELETE | `/api/calendar/{id}` | Eigenen Prüfungseintrag löschen | Ja |
| GET | `/api/grades` | Noteneinträge abrufen | Ja |
| POST | `/api/grades` | Noteneintrag hinzufügen | Ja |
| DELETE | `/api/grades/{id}` | Eigenen Noteneintrag löschen | Ja |
| GET | `/api/timetable` | Stundenplan für einen Kurs abrufen | Ja |
| GET | `/api/groups` | Kursgruppen, offizielle Gruppen und Campusgruppen auflisten | Ja |
| POST | `/api/groups` | Eigene Campusgruppe erstellen | Ja |
| GET | `/api/groups/{id}/settings` | Bearbeitbare Gruppendetails inklusive zuweisbarer Konten abrufen | Ja |
| PUT | `/api/groups/{id}/settings` | Gruppeneinstellungen wie Kommentare, Freigabe und Schreibrechte ändern | Ja |
| PUT | `/api/groups/{id}/assignments` | Konten einer bearbeitbaren Gruppe zuweisen | Ja |
| PUT | `/api/groups/{id}/member-permissions` | Berechtigungen zugewiesener Gruppenmitglieder setzen | Ja |
| POST | `/api/groups/{id}/join` | Einer öffentlichen Campusgruppe beitreten | Ja |

> **Hinweis:** Alle authentifizierungspflichtigen Endpunkte erwarten folgenden HTTP-Header:
> ```
> Authorization: Bearer <token>
> ```
> Das Token wird über `POST /api/auth/login` bezogen und muss bei jeder Anfrage an eine geschützte Ressource mitgesendet werden.

---

## Team und Rollen

| Rolle | Teammitglied | Aufgaben | Hauptbereiche |
|---|---|---|---|
| Projektleitung / Full-Stack | Jakob | Architekturentscheidungen, Code-Reviews, Sprint-Planung, Meilenstein-Präsentationen | `frontend/` und `backend/` |
| Backend-Entwicklung | Theo | REST-API, Datenbankschema, Authentifizierung, Geschäftslogik | `backend/CampusConnect.API`, `backend/CampusConnect.Application` |
| Frontend-Entwicklung | Simon | Angular-Komponenten, Routing, UI/UX, API-Integration | `frontend/src` |
| QA und Github |Julius| Testkonzept, CI/CD-Pipeline, Deployment, technische Dokumentation | `.github/`, alle Schichten |

---

## Branch-Zuständigkeiten

| Rolle | Primäre Review-Verantwortung |
|---|---|
| Projektleitung / Full-Stack | Alle Branches – abschließende Freigabe vor dem Merge in `main` |
| Backend-Entwicklung | `feature/*`- und `fix/*`-Branches mit Änderungen in `backend/` |
| Frontend-Entwicklung | `feature/*`- und `fix/*`-Branches mit Änderungen in `frontend/` |
| QA und DevOps | `test/*`-, `chore/*`- und `.github/`-Workflow-Branches |

---

## Beitragen

### Branch-Benennung

Präfix gefolgt von einer kurzen Kebab-Case-Beschreibung:

- `feature/` – neue Funktionalität
- `fix/` – Fehlerbehebungen
- `docs/` – Dokumentationsänderungen
- `chore/` – Wartungsaufgaben, Abhängigkeitsaktualisierungen
- `test/` – Tests hinzufügen oder aktualisieren

Beispiele: `feature/mensa-speiseplan`, `fix/auth-abgelaufenes-jwt`, `docs/api-notenendpunkte`

### Commit-Nachrichtenformat

Es gilt die [Conventional Commits](https://www.conventionalcommits.org/)-Spezifikation:

```
<typ>(<bereich>): <kurze Beschreibung>
```

Beispiele:
- `feat(mensa): Wochenspeiseplan-Komponente hinzufügen`
- `fix(auth): abgelaufenes JWT korrekt behandeln`
- `docs(api): Notenendpunkte dokumentieren`

### Pull-Request-Prozess

1. PR gegen den Branch `main` erstellen.
2. PR-Template vollständig ausfüllen.
3. Mindestens ein Review von einem Teammitglied anfordern.
4. Alle Review-Kommentare vor dem Merge adressieren.
5. Nach Freigabe **Squash-Merge** verwenden.

### Tests ausführen

**Frontend:**
```bash
cd frontend
ng test
```

**Backend:**
```bash
cd backend
dotnet test
```

---

## Links

- [GitHub-Projects-Board](#) *(Platzhalter)*
- [API-Dokumentation](docs/api.md)
- [SWFR-Mensa-API](#) *(Platzhalter – swfr.de/apispeiseplan)*
