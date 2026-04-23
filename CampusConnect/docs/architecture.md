# Architektur

## Systemüberblick

CampusConnect verwendet eine Drei-Schichten-Architektur: eine Angular-Single-Page-Application als Präsentationsschicht, eine ASP.NET-Core-REST-API als Geschäfts- und Datenzugriffsschicht sowie eine PostgreSQL-Datenbank als Persistenzschicht. Jede Schicht kommuniziert ausschließlich mit der benachbarten Schicht.

## Frontend-Architektur

Das Frontend basiert auf **Angular 21** und verwendet ausschließlich eigenständige Komponenten (Standalone Components), sodass NgModules nicht benötigt werden. Die wichtigsten Architekturentscheidungen:

- **Signals** (`signal()`, `computed()`) statt klassischer Properties als Standard-Reaktivitätsmodell.
- **Zoneless-ready**: `provideZoneChangeDetection({ eventCoalescing: true })` reduziert überflüssige Change-Detection-Zyklen.
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

## Externe APIs

Die SWFR-Mensa-XML-API ist unter `swfr.de/apispeiseplan` verfügbar und erfordert einen API-Schlüssel von SWFR. Um CORS-Probleme zu vermeiden und den Schlüssel geheim zu halten, leitet das Backend alle Anfragen an diesen Dienst weiter, bevor die aufbereiteten Daten an das Angular-Frontend übergeben werden.

## Authentifizierungsablauf

CampusConnect verwendet zustandslose JWT-basierte Authentifizierung:

1. Der Benutzer sendet seine Anmeldedaten an `POST /api/auth/login`.
2. Das Backend prüft die Anmeldedaten und stellt ein signiertes JWT aus.
3. Das Angular-Frontend speichert das Token **ausschließlich im Arbeitsspeicher** (nicht in localStorage).
4. Jede folgende Anfrage an einen geschützten Endpunkt enthält den Header `Authorization: Bearer <token>`.
5. Das Backend prüft Signatur und Ablaufzeit des Tokens bei jeder Anfrage.
