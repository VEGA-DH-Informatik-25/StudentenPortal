# Abgabe und Übergabe

Stand: 2026-06-24

Dieses Dokument bündelt die Lieferobjekte für Abgabe, Vorführung und Übergabe. Live-Code und Konfiguration bleiben maßgeblich; dieses Dokument verweist auf die jeweils konkreten Nachweise.

## Lieferobjekte

| Nachweis | Status | Evidenz |
|---|---|---|
| README mit Setup und Start | erfüllt | [`../../README.md`](../../README.md), [`project-overview.md`](project-overview.md) |
| Architekturüberblick | erfüllt | [`architecture.md`](architecture.md) |
| Übergabedokumentation | erfüllt | Dieses Dokument, [`project-overview.md`](project-overview.md), [`contributing.md`](contributing.md), [`testing.md`](testing.md) |
| Must-have-Use-Cases demonstrierbar | teilweise | [`demo-checkliste.md`](demo-checkliste.md), Demo-Daten in [`demo-data.md`](demo-data.md), Playwright-Smoke-Tests im Frontend |
| Anforderungen final priorisiert | erfüllt | [`anforderungsstatus.md`](anforderungsstatus.md), geschütztes [`../../prd-mvp.md`](../../prd-mvp.md) |
| Offene Lücken transparent | erfüllt | [`anforderungsstatus.md`](anforderungsstatus.md), [`qa-nachweis.md`](qa-nachweis.md), historische Befunde in [`code-review.md`](code-review.md) |
| Fehlende Teile als Mockups/Screenshots dokumentiert | erfüllt | [`media/werbevideo.md`](media/werbevideo.md), `../../werbevideo/app-screenshots/`, [`anforderungsstatus.md`](anforderungsstatus.md) |
| Qualitätssicherung sichtbar | erfüllt | [`qa-nachweis.md`](qa-nachweis.md), CI in [`../.github/workflows/ci.yml`](../.github/workflows/ci.yml), Tests im Code |
| Demo/Vorführung vorbereitet | erfüllt | [`demo-checkliste.md`](demo-checkliste.md), [`demo-data.md`](demo-data.md), [`media/werbevideo.md`](media/werbevideo.md) |
| Abgabe fachlich vorbereitet | erfüllt | Dieses Dokument und Dokumentationsindex |
| Impressum, Datenschutz, Nutzungsordnung | teilweise | Öffentliche Platzhalterseiten `/legal/impressum`, `/legal/datenschutz`, `/legal/nutzungsordnung`; finale rechtliche Angaben fehlen |
| DHBW-IT-Integration vorbereitet | teilweise | Architektur ist trennbar und konfigurierbar; SSO, Produktivdeployment und produktionsreife Containerisierung sind nicht implementiert |

## Übergabeumfang

- Anwendung: Angular-Frontend unter `../frontend` und ASP.NET-Core-Backend unter `../backend`.
- Daten: SQLite über EF-Core-Migrations; lokale Demo-Daten werden in Development über `DemoData:Enabled=true` geseedet.
- Authentifizierung: Admin-angelegte Konten, JWT für API-Clients, HttpOnly-Cookie für Browser-Sessions.
- Dokumentation: Zentraler Einstieg über [`README.md`](README.md), technische Quellen in `docs/`, geschützte MVP-Anforderungen in [`../../prd-mvp.md`](../../prd-mvp.md).
- Qualitätssicherung: Backend-xUnit, Angular/Vitest, Playwright-Smoke-Tests und GitHub-Actions-CI.

## Übergabe-Checkliste

- Repository ist ohne echte Secrets, echte Zugangsdaten und produktive personenbezogene Daten übergabefähig.
- Lokale Datenbank `backend/CampusConnect.API/campusconnect.db` vor Commit oder Übergabe prüfen, falls sie mitgeliefert werden soll.
- `Jwt:Secret` lokal über User-Secrets oder Umgebungsvariablen setzen; niemals in `appsettings.json` eintragen.
- Demo-Passwort nur für lokale Demonstration verwenden und nicht für geteilte oder produktive Umgebungen einsetzen.
- Rechtliche Platzhalterseiten vor offizieller Nutzung durch echte, freigegebene Angaben ersetzen.
- `docker-compose.yml` bleibt ein Platzhalter und ist nicht als produktionsreife Startumgebung zu präsentieren.

## IT-Readiness

CampusConnect ist für eine spätere IT-Integration vorbereitet, aber nicht produktiv integriert:

- Datenspeicherung ist über `ConnectionStrings:CampusConnect` konfigurierbar.
- Externe DHBW-/SWFR-Quellen werden ausschließlich im Backend gekapselt.
- Rollen und Authentifizierung sind zentral im Backend geprüft.
- SSO ist nicht implementiert; ein späterer SSO-Adapter müsste die bestehende Auth-Schicht ergänzen.
- Deployment ist nicht produktionsfertig; CI validiert Build und Tests, aber keine Release- oder Hosting-Pipeline.
