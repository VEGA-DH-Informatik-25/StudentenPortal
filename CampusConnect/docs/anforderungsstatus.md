# Anforderungsstatus

Stand: 2026-06-24

Dieses Dokument ist die aktuelle Statusmatrix zur geschützten MVP-PRD. Es ändert keine Anforderungen in [`../../prd-mvp.md`](../../prd-mvp.md), sondern ordnet die aktuelle Implementierung, Priorisierung und Nachweise ein.

## Statuslegende

| Status | Bedeutung |
|---|---|
| erfüllt | Im Live-Code vorhanden und durch Tests, Build oder Demo-Daten nachweisbar |
| teilweise | Implementierung ist vorhanden, aber fachlich/rechtlich/operativ noch nicht vollständig abgabereif |
| Mockup/Nachweis | Nicht vollständig als Produktfeature umgesetzt, aber durch Screenshot, Video, Konzept oder Platzhalter dokumentiert |
| offen | Noch nicht umgesetzt |
| nicht im Scope | Bewusst außerhalb des MVP |

## Must-have-Matrix

| Priorität | Anforderung | Status | Evidenz / Einschränkung |
|---|---|---|---|
| Must | Nutzeranlage durch Admin | erfüllt | Admin-API und Admin-UI, Tests in `AdminUsersServiceTests`, `AdminUsersApiTests` |
| Must | Login/Logout und geschützte Bereiche | erfüllt | Auth-Service, Guards, Cookie/JWT-Fluss, `ApiAuthorizationTests`, `auth.spec.ts` |
| Must | Onboarding und Initialpasswortwechsel | teilweise | API und UI vorhanden; Guided-Tour-/Gruppenvorschlagslogik ist implementiert, finale UX-Abnahme offen |
| Must | Rollen und Berechtigungen | erfüllt | Rollen `Student`, `Lecturer`, `Management`, `Admin`; Gruppenrollen und API-Tests vorhanden |
| Must | Profil und Kurszuordnung | erfüllt | Profilseite, Kursliste, Admin-Kursverwaltung, Profiltests |
| Must | Gruppenbasierter News-Feed | erfüllt | Feed mit Gruppen, Kommentaren, Reaktionen, Moderation, API-/Frontend-Tests |
| Must | Gruppen | erfüllt | Gruppenübersicht, Details, Einstellungen, Join/Request/Invite/Leave, Moderation und Tests |
| Must | Mensa-Speiseplan | erfüllt | Backend-Proxy und Frontend-Seite; externe Verfügbarkeit bleibt abhängig von SWFR-Konfiguration |
| Must | Noten-Tracker | erfüllt | Manuelle Noten, Durchschnitt, Was-waere-wenn-Rechner, Service- und UI-Tests |
| Must | Stundenplan | erfüllt | Backend-iCal-Service, Kursauswahl, Wochen-/Tagesansicht und Tests |
| Must | Kontaktbuch | erfüllt | Suche und Favoriten-UI; Datenschutzfreigabe für reale Nutzung bleibt offen |
| Must | Admin-Bereich | erfüllt | Benutzer-/Kursverwaltung, Admin-Guard, API- und UI-Tests |
| Must | Onboarding-Feed / Guided Start | teilweise | Onboarding-Seite und Guided-Tour-Service vorhanden; detaillierte fachliche Endabnahme ausstehend |
| Must | Laptop- und iPad-Anpassung | teilweise | Responsive Layouts vorhanden; visuelle iPad-Browserabnahme als manueller QA-Schritt offen |

## Could-have- und Zusatzumfang

| Priorität | Anforderung | Status | Evidenz / Einschränkung |
|---|---|---|---|
| Could | Handy-Anpassung | teilweise | Responsive Layouts vorhanden; keine eigene mobile Abnahme vollständig dokumentiert |
| Could | Darkmode | erfüllt | Theme-Service, Navbar-Einstellung und Tests |
| Could | Prüfungskalender | erfüllt | Kalenderroute, API, persönliche Einträge und Tests |
| Zusatz | Rechtliche Seiten | Mockup/Nachweis | `/legal/impressum`, `/legal/datenschutz`, `/legal/nutzungsordnung` als prüfpflichtige Platzhalter |
| Zusatz | Werbe-/Mockup-Nachweise | erfüllt | [`media/werbevideo.md`](media/werbevideo.md), `../../werbevideo/app-screenshots/` |
| Zusatz | E2E-Smoke-Tests | erfüllt | Playwright-Konfiguration und `npm run e2e` |

## Offene Lücken und bekannte Einschränkungen

- Rechtliche Texte sind Platzhalter. Impressum, Datenschutzerklärung und Nutzungsordnung brauchen finale Verantwortliche, Hosting-/Datenschutzangaben und Freigabe.
- Docker Compose ist weiterhin nicht produktionsreif und darf nur als Platzhalter erwähnt werden.
- Echte SSO-Integration, produktives Hosting, Monitoring, Backups und Betriebshandbuch sind nicht implementiert.
- Kontaktbuch und Gruppenverwaltung können personenbezogene Daten sichtbar machen; für reale Nutzung ist eine Datenschutz-/Berechtigungsfreigabe nötig.
- Externe Mensa- und Stundenplanquellen können in Demos ausfallen; Demo und Tests müssen mit Fallback-Erklärung vorbereitet werden.
- Der Frontend-Produktionsbuild ist erfolgreich, meldet aber SCSS-Budget-Warnungen für mehrere Komponenten.

## Mockup- und Nachweisartefakte

- Statische Screenshots: `../../werbevideo/app-screenshots/`
- Werbevideo: `../../werbevideo/CampusConnect-Werbevideo.mp4`
- Video-Dokumentation: [`media/werbevideo.md`](media/werbevideo.md)
- Konzepte, die nicht automatisch Implementierungsstand sind: [`concepts/`](concepts/), [`onboarding.md`](onboarding.md)

