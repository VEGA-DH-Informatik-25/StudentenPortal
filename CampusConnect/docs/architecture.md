# Architektur

## Systemüberblick

CampusConnect verwendet eine Drei-Schichten-Architektur: eine Angular-Single-Page-Application als Präsentationsschicht, eine ASP.NET-Core-REST-API als Geschäfts- und Datenzugriffsschicht sowie eine SQLite-Datenbank als aktuelle Persistenzschicht. Jede Schicht kommuniziert ausschließlich mit der benachbarten Schicht.

## Frontend-Architektur

Das Frontend basiert auf **Angular 21** und verwendet ausschließlich eigenständige Komponenten (Standalone Components), sodass NgModules nicht benötigt werden. Die wichtigsten Architekturentscheidungen:

- **Signals** (`signal()`, `computed()`) statt klassischer Properties als Standard-Reaktivitätsmodell.
- **Zoneless-ready**: `provideZonelessChangeDetection()` aktiviert zonenlose Change Detection.
- **Lazy Loading**: Alle Feature-Bereiche (Feed, Mensa, Kalender, Noten, Gruppen, Admin) werden über `loadComponent` in `app.routes.ts` erst bei Bedarf geladen.
- **Functional Guards**: Der Auth-Guard ist als `CanActivateFn`-Funktion implementiert (kein Interface-basiertes Klassen-Guard mehr).
- **Functional Interceptors**: `authTokenInterceptor` und `errorHandlerInterceptor` sind als `HttpInterceptorFn`-Funktionen implementiert und werden über `provideHttpClient(withInterceptors([...]))` registriert.
- **`withComponentInputBinding()`**: Ermöglicht das direkte Binden von Route-Parametern an Component-Inputs.
- **`shared/ui`** enthält wiederverwendbare, rein präsentationale Komponenten (`LoadingSpinner`, `ErrorMessage`).

## Backend-Architektur

Das Backend folgt der **Clean Architecture** mit vier Schichten:

| Schicht | Projekt | Abhängigkeit |
|---|---|---|
| Domain | `CampusConnect.Domain` | *(keine)* |
| Application | `CampusConnect.Application` | Domain |
| Infrastructure | `CampusConnect.Infrastructure` | Application |
| API | `CampusConnect.API` | Application |

Abhängigkeiten zeigen stets nach innen zur Domain-Schicht. Infrastructure und API implementieren Interfaces, die in der Application-Schicht definiert sind.

## Persistenz und Repository-Strategie

Die aktuelle Implementierung persistiert Benutzer, Kurse, Gruppen, Feed-Beiträge, Noten und Prüfungseinträge in SQLite über Entity Framework Core. EF-Migrations verwalten das Datenbankschema; bestehende lokale SQLite-Datenbanken aus der früheren `EnsureCreated`-Initialisierung werden beim Start in die Migration-History übernommen, damit sie ohne Datenverlust weiter migriert werden können. Feed-Kommentare, Reaktionen sowie Gruppeneinstellungen und Gruppenrollen der Mitglieder werden als strukturierte JSON-Spalten gespeichert. Services, die Kurszuordnungen ändern, synchronisieren weiterhin die abgeleiteten Kursgruppen, damit Benutzer-, Kurs- und Gruppenansicht konsistent bleiben.

## Externe APIs

Die SWFR-Mensa-XML-API ist unter `swfr.de/apispeiseplan` verfügbar und erfordert einen API-Schlüssel von SWFR. Um CORS-Probleme zu vermeiden und den Schlüssel geheim zu halten, leitet das Backend alle Anfragen an diesen Dienst weiter, bevor die aufbereiteten Daten an das Angular-Frontend übergeben werden.

Der Stundenplan wird im Backend aus iCal-Kalendern geladen. `Timetable:CalendarUrlTemplate` enthält den Platzhalter `{course}` für den normalisierten Kurscode; `Timetable:CourseAliases` ordnet sichtbare Kurscodes abweichenden Kalenderpostfächern zu. Neue Kurse und Postfachvarianten werden damit über Konfiguration ergänzt, ohne den Endpunkt oder den Parser anzupassen.

## Authentifizierungsablauf

CampusConnect uses JWT-based API authentication and an HttpOnly cookie for browser sessions with a 15-minute sliding idle timeout:

1. The user sends credentials to `POST /api/auth/login`.
2. The backend validates the credentials, issues a signed JWT, and also sets an HttpOnly cookie for the browser session.
3. The Angular frontend keeps the token **only in memory** (not in localStorage or sessionStorage); after a reload, the session is restored from the cookie through `GET /api/auth/me`.
4. API clients can continue to use the `Authorization: Bearer <token>` header. Browsers send the HttpOnly cookie automatically instead.
5. The backend validates authentication on every request and extends the browser session only when there is activity.

Bleibt der Benutzer 15 Minuten inaktiv, beendet das Frontend die lokale Sitzung; das Cookie läuft ebenfalls nach 15 Minuten ohne Aktivität ab.
