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

Die aktuelle Implementierung persistiert Benutzer, Kurse, Gruppen, Feed-Beiträge, Noten und Prüfungseinträge in SQLite über Entity Framework Core. EF-Migrations verwalten das Datenbankschema; bestehende lokale SQLite-Datenbanken aus der früheren `EnsureCreated`-Initialisierung werden beim Start in die Migration-History übernommen, damit sie ohne Datenverlust weiter migriert werden können. Feed-Kommentare, Reaktionen sowie Gruppeneinstellungen und Mitgliedsrechte werden als strukturierte JSON-Spalten gespeichert. Services, die Kurszuordnungen ändern, synchronisieren weiterhin die abgeleiteten Kursgruppen, damit Benutzer-, Kurs- und Gruppenansicht konsistent bleiben.

## Externe APIs

Die SWFR-Mensa-XML-API ist unter `swfr.de/apispeiseplan` verfügbar und erfordert einen API-Schlüssel von SWFR. Um CORS-Probleme zu vermeiden und den Schlüssel geheim zu halten, leitet das Backend alle Anfragen an diesen Dienst weiter, bevor die aufbereiteten Daten an das Angular-Frontend übergeben werden.

## Authentifizierungsablauf

CampusConnect verwendet JWT-basierte API-Authentifizierung und für Browser-Sitzungen ein HttpOnly-Cookie mit 15 Minuten gleitender Inaktivitätszeit:

1. Der Benutzer sendet seine Anmeldedaten an `POST /api/auth/login`.
2. Das Backend prüft die Anmeldedaten, stellt ein signiertes JWT aus und setzt zusätzlich ein HttpOnly-Cookie für die Browser-Sitzung.
3. Das Angular-Frontend speichert das Token **ausschließlich im Arbeitsspeicher** (nicht in localStorage oder sessionStorage); nach einem Reload wird die Sitzung über `GET /api/auth/me` aus dem Cookie wiederhergestellt.
4. API-Clients können weiterhin den Header `Authorization: Bearer <token>` verwenden. Der Browser sendet stattdessen das HttpOnly-Cookie automatisch mit.
5. Das Backend prüft die jeweilige Anmeldung bei jeder Anfrage und verlängert die Browser-Sitzung nur bei Aktivität.

Bleibt der Benutzer 15 Minuten inaktiv, beendet das Frontend die lokale Sitzung; das Cookie läuft ebenfalls nach 15 Minuten ohne Aktivität ab.
