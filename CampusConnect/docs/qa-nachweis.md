# QA-Nachweis

Stand: 2026-06-26

Dieses Dokument macht die Qualitätssicherung für CampusConnect sichtbar. Die aktuellen Kommandoausgaben bleiben maßgeblich; die Zahlen hier dokumentieren die lokale Verifikation vom 2026-06-24.

## Testpyramide

| Ebene | Werkzeug | Zweck | Ort |
|---|---|---|---|
| Application-Tests | xUnit | Geschäftsregeln, Services, Security-Helfer | `backend/CampusConnect.Application.Tests` |
| API-/Integrationstests | xUnit + `WebApplicationFactory` | Controller, Auth, Repository, Migrations, Demo-Seeding, externe Parser | `backend/CampusConnect.API.Tests` |
| Frontend-Unit-/Component-Tests | Angular/Vitest + jsdom | Services, Guards, Interceptors, i18n, Feature-Komponenten | `frontend/src/app/**/*.spec.ts` |
| E2E-Smoke-Tests | Playwright | Login, Kernnavigation, Admin-Zugriff, öffentliche Rechtsseiten | `frontend/e2e` |
| Build-Gates | Angular build, .NET build | Compile-, Template- und Bundle-Validierung | CI und lokale Kommandos |

## Lokale Kommandos

Backend:

```powershell
cd CampusConnect/backend
dotnet test .\CampusConnect.slnx
```

Frontend-Unit-Tests:

```powershell
cd CampusConnect/frontend
npm test -- --watch=false
```

Frontend-Produktionsbuild:

```powershell
cd CampusConnect/frontend
npm run build
```

E2E-Smoke-Tests:

```powershell
cd CampusConnect/frontend
npm run e2e
```

## Verifikation vom 2026-06-24

| Kommando | Ergebnis |
|---|---|
| `dotnet test .\CampusConnect.slnx` | bestanden, 147 Tests gesamt: 102 Application, 45 API |
| `npm test -- --watch=false` | bestanden, 33 Testdateien und 134 Tests |
| `npm run build` | bestanden mit SCSS-Budget-Warnungen |

Bekannte Build-Warnungen:

- `group-settings-page.scss` überschreitet 8 kB um 249 Bytes.
- `navbar.scss` überschreitet 8 kB um 721 Bytes.
- `admin-page.scss` überschreitet 8 kB um 257 Bytes.
- `timetable-page.scss` überschreitet 8 kB um 329 Bytes.

## CI-Gates

Die GitHub-Actions-CI führt aus:

- Backend restore/build/test.
- Frontend `npm ci`, Unit-Tests und Produktionsbuild.
- Playwright-Smoke-Tests als separater E2E-Job mit isolierter Datenbank.

Unit/API-Tests bleiben schnelle Pflicht-Gates. E2E-Smoke prüft nur Kernwege und soll stabil bleiben; detaillierte fachliche Abnahme erfolgt über die Demo-Checkliste.

## Manuelle QA-Checkliste

- Login/Logout mit Student, Lecturer und Admin prüfen.
- Admin-Nutzeranlage mit Initialpasswort prüfen.
- Onboarding-Flow mit neuem Nutzer prüfen.
- Feed: Beitrag, Kommentar, Reaktion und Freigabe für moderierte Gruppe prüfen.
- Gruppen: Join, Request, Invitation, Leave und Owner-Transfer prüfen.
- Mensa: erfolgreiche Datenanzeige und Fehlerzustand prüfen.
- Stundenplan: Kursauswahl, Tages-/Wochenansicht und leere Zustände prüfen.
- Noten: manuelle Note, Durchschnitt und Löschen prüfen.
- Kontakte: Suche ab drei Zeichen und Favoriten-UI prüfen.
- Profil: Kurs und Kontaktdetails ändern.
- Rechtliche Seiten ohne Login und aus der Navbar öffnen.
- Desktop/Laptop und iPad- oder vergleichbare Tablet-Breite visuell prüfen; Smartphone-/Handy-Breiten sind kein verpflichtender QA-Schritt.

## Bekannte QA-Lücken

- Keine vollständige visuelle Regression-Suite.
- Keine Coverage-Schwellen in CI.
- Keine produktive Deployment-/Monitoring-Prüfung.
- Keine rechtliche Endabnahme der Platzhalterseiten.
- Datenschutz- und Rollenmodell für reale Kontaktbuchnutzung muss vor Produktivbetrieb geprüft werden.
