# Architektur

## Systemüberblick

CampusConnect besteht aus einer Angular-Single-Page-Application und einem ASP.NET-Core-Backend in Clean-Architecture-Aufteilung. Das Backend trennt Domain, Application, Infrastructure und API. SQLite ist die aktuelle Persistenz. Die API ist HTTP-Grenze und Composition-Root und referenziert deshalb Application sowie Infrastructure.

## Frontend-Architektur

Das Frontend basiert auf **Angular 21** und verwendet ausschließlich eigenständige Komponenten (Standalone Components), sodass NgModules nicht benötigt werden. Die wichtigsten Architekturentscheidungen:

- **Signals** (`signal()`, `computed()`) statt klassischer Properties als Standard-Reaktivitätsmodell.
- **Zoneless-ready**: `provideZonelessChangeDetection()` aktiviert zonenlose Change Detection.
- **Lazy Loading**: Alle Feature-Bereiche (Feed, Mensa, Kalender, Noten, Gruppen, Admin) werden über `loadComponent` in `app.routes.ts` erst bei Bedarf geladen.
- **Functional Guards**: Der Auth-Guard ist als `CanActivateFn`-Funktion implementiert (kein Interface-basiertes Klassen-Guard mehr).
- **Functional Interceptors**: `authTokenInterceptor` und `errorHandlerInterceptor` sind als `HttpInterceptorFn`-Funktionen implementiert und werden über `provideHttpClient(withInterceptors([...]))` registriert.
- **`withComponentInputBinding()`**: Ermöglicht das direkte Binden von Route-Parametern an Component-Inputs.
- **`shared/ui`** enthält wiederverwendbare, rein präsentationale Komponenten, beispielsweise `ProfileHoverCard`.

### Internationalisierung

Das Frontend besitzt eine eigene, signalbasierte Englisch-/Deutsch-Übersetzungsschicht unter `src/app/core/i18n/`:

- `translations.ts` definiert die zulässigen Übersetzungsschlüssel und beide Sprachwerte.
- Der standalone `TranslatePipe` wird für übersetzte Template-Texte importiert.
- Der `I18n`-Service übersetzt Texte in TypeScript und liefert mit `locale()` die Locale für `Intl`-Formatierung.
- Der `I18n`-Service bildet bekannte Backend-Fehlertexte mit `readError()` auf Übersetzungsschlüssel ab. Unbekannte API-Details werden nicht roh im UI angezeigt, sondern über den lokalisierten Fallback der jeweiligen Komponente.
- Die Sprachauswahl wird als nicht sensible UI-Präferenz unter `campusconnect.language` in `localStorage` gespeichert.
- Die Startsprache folgt einer gespeicherten Auswahl oder fällt auf Deutsch zurück.
- Die Sprache wird im Zahnrad-Menü der Navbar über Buttons gewählt; `document.documentElement.lang` folgt der aktiven Auswahl.
- `app.config.ts` registriert deutsche und englische Locale-Daten und verwendet für Angulars statisches `LOCALE_ID` `de-DE`. Dynamisch lokalisierte Datums- und Zahlenformate verwenden weiterhin `I18n.locale()`.

Neue nutzerseitige Texte werden nicht direkt in Templates oder Komponenten geschrieben, sondern als englischer und deutscher Schlüssel ergänzt.

### Darstellung und Theme

Das Frontend besitzt einen globalen `Theme`-Service unter `src/app/core/services/theme.ts`:

- Unterstützte Präferenzen sind `system`, `light` und `dark`.
- Die Auswahl wird als nicht sensible UI-Präferenz unter `campusconnect.theme` in `localStorage` gespeichert.
- `system` ist der Default und folgt `prefers-color-scheme`, bis der Benutzer explizit Hell oder Dunkel auswählt.
- Der Service setzt `document.documentElement.dataset.theme` auf `light` oder `dark` und synchronisiert `color-scheme`.
- Sichtbare Farben werden über globale Tokens in `styles.scss` gesteuert, damit Feature-Seiten, Popover, Statuschips, Modals und Kalender-/Stundenplanflächen in beiden Darstellungen konsistent bleiben.

Sprache und Darstellung werden gemeinsam über das Zahnrad-Menü der Navbar bedient.

## Backend-Architektur

Das Backend folgt der **Clean Architecture** mit vier Schichten:

| Schicht | Projekt | Abhängigkeit |
|---|---|---|
| Domain | `CampusConnect.Domain` | *(keine)* |
| Application | `CampusConnect.Application` | Domain |
| Infrastructure | `CampusConnect.Infrastructure` | Application *(Domain transitiv)* |
| API | `CampusConnect.API` | Application und Infrastructure |

Domain besitzt keine Projektabhängigkeit. Application referenziert Domain. Infrastructure implementiert Persistenz und externe Dienste hinter den in den inneren Schichten definierten Schnittstellen. Die API bindet Application und Infrastructure über Dependency Injection ein; Controller bleiben von Repositories und DbContext getrennt.

## Persistenz und Repository-Strategie

Die aktuelle Implementierung persistiert Benutzer, Kurse, Gruppen, Feed-Beiträge, Noten und Prüfungseinträge in SQLite über Entity Framework Core. EF-Migrations verwalten das Datenbankschema; bestehende lokale SQLite-Datenbanken aus der früheren `EnsureCreated`-Initialisierung werden beim Start in die Migration-History übernommen, damit sie ohne Datenverlust weiter migriert werden können. Feed-Kommentare, Reaktionen sowie Gruppeneinstellungen und Gruppenrollen der Mitglieder werden als strukturierte JSON-Spalten gespeichert. Services, die Kurszuordnungen ändern, synchronisieren weiterhin die abgeleiteten Kursgruppen, damit Benutzer-, Kurs- und Gruppenansicht konsistent bleiben.

## Externe APIs

Die SWFR-Mensa-XML-API ist unter `swfr.de/apispeiseplan` verfügbar und erfordert einen API-Schlüssel von SWFR. Um CORS-Probleme zu vermeiden und den Schlüssel geheim zu halten, leitet das Backend alle Anfragen an diesen Dienst weiter, bevor die aufbereiteten Daten an das Angular-Frontend übergeben werden.

Der Stundenplan wird im Backend aus iCal-Kalendern geladen. `Timetable:CalendarUrlTemplate` enthält den Platzhalter `{course}` für den normalisierten Kurscode; `Timetable:CourseAliases` ordnet sichtbare Kurscodes abweichenden Kalenderpostfächern zu. Neue Kurse und Postfachvarianten werden damit über Konfiguration ergänzt, ohne den Endpunkt oder den Parser anzupassen.

## Authentifizierungsablauf

CampusConnect uses JWT-based API authentication and an HttpOnly cookie for browser sessions with a 15-minute sliding idle timeout:

1. Admins create user accounts through `POST /api/admin/users`; public self-registration is not available.
2. The user sends credentials to `POST /api/auth/login`.
3. The backend rate-limits online login attempts across the normalized account, IP address, and User-Agent based device fingerprint. After 5 failed attempts within 15 minutes, it temporarily blocks further attempts for 1 minute and escalates repeated attempts during the block to 5, 15, and at most 60 minutes. No permanent automatic account lock is written to the database.
4. The backend validates the credentials and the active account state, issues a signed JWT, resets the login failure counters, and also sets an HttpOnly cookie for the browser session.
5. The Angular frontend keeps the token **only in memory** (not in localStorage or sessionStorage); after a reload, the session is restored from the cookie through `GET /api/auth/me`.
6. API clients can continue to use the `Authorization: Bearer <token>` header. Browsers send the HttpOnly cookie automatically instead.
7. The backend validates authentication against the active database user on every protected request and extends the browser session only when there is activity.

Bleibt der Benutzer 15 Minuten inaktiv, beendet das Frontend die lokale Sitzung; das Cookie läuft ebenfalls nach 15 Minuten ohne Aktivität ab.
