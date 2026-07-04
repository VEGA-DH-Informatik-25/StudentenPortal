# CampusConnect

CampusConnect ist ein webbasiertes Studierendenportal fuer die DHBW Loerrach. Die Anwendung buendelt Authentifizierung, News-Feed, Gruppen, Mensa, Stundenplan, Pruefungskalender, Noten, Kontakte, Profilverwaltung und Administration in einer Angular-/ASP.NET-Core-Web-App.

**Uebergabestatus:** Der MVP ist nach aktuellem Code- und Teststand vom 2026-07-04 fachlich erfuellt. Bekannte Restpunkte sind externe Datenquellen, Platzhalter-Rechtstexte, Docker als Platzhalter, SCSS-Budgetwarnungen und eine NuGet-Sicherheitswarnung fuer `Microsoft.OpenApi`.

## Schnellorientierung

| Thema | Einstieg |
|---|---|
| Aktuelle MVP-Erfuellung | [`CampusConnect/docs/anforderungsstatus.md`](CampusConnect/docs/anforderungsstatus.md) |
| Lokales Setup und Demoablauf | [`CampusConnect/docs/product/setup.md`](CampusConnect/docs/product/setup.md) |
| Produkt- und Projektkontext | [`CampusConnect/docs/product/projektbeschreibung.md`](CampusConnect/docs/product/projektbeschreibung.md) |
| Architektur | [`CampusConnect/docs/product/architecture.md`](CampusConnect/docs/product/architecture.md) |
| API-Oberflaeche | [`CampusConnect/docs/product/api.md`](CampusConnect/docs/product/api.md) |
| Tests und QA | [`CampusConnect/docs/product/testing.md`](CampusConnect/docs/product/testing.md) |
| Testfallkatalog | [`CampusConnect/docs/product/testfallkatalog.md`](CampusConnect/docs/product/testfallkatalog.md) |
| Demo-Daten | [`CampusConnect/docs/information/demo-data.md`](CampusConnect/docs/information/demo-data.md) |
| Agenten-/Beitragsregeln | [`AGENTS.md`](AGENTS.md) |
| Geschuetzte MVP-PRD | [`prd-mvp.md`](prd-mvp.md) |

## Repository-Aufbau

```text
.
|-- AGENTS.md
|-- README.md
|-- prd-mvp.md
`-- CampusConnect/
    |-- backend/
    |   |-- CampusConnect.slnx
    |   |-- CampusConnect.API/
    |   |-- CampusConnect.Application/
    |   |-- CampusConnect.Domain/
    |   |-- CampusConnect.Infrastructure/
    |   |-- CampusConnect.API.Tests/
    |   `-- CampusConnect.Application.Tests/
    |-- frontend/
    |   |-- angular.json
    |   |-- package.json
    |   `-- src/app/
    `-- docs/
        |-- README.md
        |-- anforderungsstatus.md
        |-- product/
        |-- information/
        |-- concepts/
        |-- media/
        `-- team/
```

## MVP-Status

Alle Must-have-Bereiche aus der PRD sind im aktuellen Projektstand umgesetzt:

- Admin-Nutzeranlage mit Initialpasswort, Rollen, Kurszuordnung und Aktivstatus.
- Login/Logout mit JWT fuer API-Clients und HttpOnly-Cookie fuer Browser-Sessions.
- Verpflichtender Initialpasswortwechsel und Onboarding mit anschliessender Guided Tour.
- Rollen- und Gruppenberechtigungen fuer `Student`, `Lecturer`, `Management`, `Admin` sowie Owner/Moderator/Member innerhalb von Gruppen.
- Profilbearbeitung fuer Anzeigename, Telefon und Standort; Kurswechsel nur administrativ.
- Gruppenbasierter Feed mit Kommentaren, Reaktionen, Anhaengen, Moderation und Berechtigungsflags.
- Course-, Official- und Campus-Gruppen mit Join/Request/Invite/Leave, Einstellungen und Mitgliedsverwaltung.
- Mensa-Integration ueber das Backend zur SWFR-XML-API.
- Stundenplan-Integration ueber backendseitige iCal-Abrufe.
- Manueller Noten-Tracker mit ECTS, Durchschnitt und Simulation.
- Kontaktbuch mit Suche und Favoriten.
- Admin-Bereich fuer Nutzer- und Kursverwaltung.
- Laptop- und iPad-taugliche Smoke-Abdeckung ueber Playwright.

Der Pruefungskalender und Dark Mode sind als Zusatz-/Could-have-Umfang umgesetzt. Smartphone-Optimierung, produktiver Docker-Betrieb, SSO, offizieller Dualis-Import, Push-Reminder, Chat und native Apps gehoeren nicht zum MVP.

## Lokal Starten

Voraussetzungen: .NET 10 SDK, Node.js/npm passend zu `CampusConnect/frontend/package.json`, lokale Secrets fuer JWT und optional Mensa.

Backend-Secrets setzen:

```powershell
cd CampusConnect/backend
dotnet user-secrets set "Jwt:Secret" "<at-least-32-character-secret>" --project .\CampusConnect.API\CampusConnect.API.csproj
dotnet user-secrets set "Mensa:ApiKey" "<swfr-mensa-api-key>" --project .\CampusConnect.API\CampusConnect.API.csproj
```

Backend starten:

```powershell
cd CampusConnect/backend
dotnet run --project .\CampusConnect.API\CampusConnect.API.csproj
```

Frontend installieren und starten:

```powershell
cd CampusConnect/frontend
npm install
npm start
```

Lokale URLs:

- Frontend: `http://localhost:4200`
- API: `http://localhost:5135`
- Swagger: `http://localhost:5135/swagger`

## Demo-Uebergabe

In Development seedet die API Demo-Daten, wenn `DemoData:Enabled` aktiv ist. Das Standardpasswort ist lokal `CampusDemo2026!`.

| Rolle | Account | Zweck |
|---|---|---|
| Admin | `demo.admin@dhbw-loerrach.de` | Nutzer, Kurse, Admin-Bereich |
| Lecturer | `demo.technik@dhbw-loerrach.de` | Kurs-/Technikgruppen, Feed |
| Lecturer | `demo.wirtschaft@dhbw-loerrach.de` | Wirtschaftsgruppen, Feed |
| Student | `lena.tif25a@dhbw-loerrach.de` | Hauptdemo fuer Studierendenfluss |
| Student | `noah.wwi25a@dhbw-loerrach.de` | Gruppen- und Kursvergleich |
| Student | `mia.wdb25a@dhbw-loerrach.de` | Kontakt- und Gruppenvergleich |

Empfohlener Demoablauf: Login als Student, Feed, Mensa, Stundenplan, Kalender, Noten, Gruppen, Kontakte, Profil, Logout; danach Login als Admin und Nutzer-/Kursverwaltung zeigen. Die genaue Checkliste steht in [`CampusConnect/docs/product/setup.md`](CampusConnect/docs/product/setup.md).

## Letzte Verifikation

Lokaler Stand vom 2026-07-04:

| Befehl | Ergebnis |
|---|---|
| `dotnet test .\CampusConnect.slnx --no-restore` | 152 Tests bestanden: 107 Application, 45 API; Warnung `NU1903` fuer `Microsoft.OpenApi 2.4.1` |
| `npm.cmd test -- --watch=false` | 35 Testdateien / 166 Tests bestanden |
| `npm.cmd run build` | Produktionsbuild erfolgreich; SCSS-Budgetwarnungen fuer Navbar, Feed, Timetable, Admin und Group Settings |
| `npm.cmd run e2e` | 9 Playwright-Smoke-Tests bestanden auf Desktop, iPad Portrait und iPad Landscape |

Hinweis fuer Codex-/Sandbox-Laeufe: Angular-Test, Build und E2E koennen im eingeschraenkten Sandbox-Dateisystem an Pfadauflösung scheitern. Ausserhalb der Sandbox liefen sie erfolgreich.

## Bekannte Uebergabepunkte

- Externe Mensa- und Stundenplandaten koennen in Live-Demos ausfallen; die App zeigt Fehlerzustaende, aber die Datenquelle bleibt ein Betriebsrisiko.
- Rechtliche Seiten unter `/legal/impressum`, `/legal/datenschutz` und `/legal/nutzungsordnung` sind Projektplatzhalter und brauchen finale rechtliche Freigabe.
- `CampusConnect/docker-compose.yml` ist weiterhin nicht produktionsreif.
- Keine echten Secrets, Tokens, produktiven Daten oder realen personenbezogenen Daten committen.
- Die lokale SQLite-Demo-Datenbank kann personenbezogene Demo- oder Testdaten enthalten; vor Commits bewusst pruefen.
- Die SCSS-Budgetwarnungen blockieren den Build nicht, sind aber ein sinnvoller Nachlaufpunkt.
- Die NuGet-Advisory `GHSA-v5pm-xwqc-g5wc` fuer `Microsoft.OpenApi 2.4.1` sollte vor einer echten Abgabe/Deployment-Entscheidung bewertet werden.

## Dokumentationsregel

Live-Code und Konfiguration sind massgeblich, wenn alte Konzepttexte widersprechen. Bei API-, Architektur-, Setup- oder Testaenderungen die Dokumente unter `CampusConnect/docs/product/` im selben Change aktualisieren. Konzeptdokumente unter `CampusConnect/docs/concepts/` sind Planungsunterlagen und nicht automatisch implementierter Produktumfang.

## Lizenz

CampusConnect steht unter der [MIT License](LICENSE).
