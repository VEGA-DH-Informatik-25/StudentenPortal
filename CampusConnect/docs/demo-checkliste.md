# Demo-Checkliste

Stand: 2026-06-24

Diese Checkliste bereitet eine reproduzierbare Vorführung der zentralen CampusConnect-Funktionen vor. Sie nutzt lokale Demo-Daten und vermeidet echte personenbezogene Daten.

## Vorbereitung

1. Backend-Secrets setzen:

```powershell
cd CampusConnect/backend
dotnet user-secrets set "Jwt:Secret" "<at-least-32-character-secret>" --project .\CampusConnect.API\CampusConnect.API.csproj
```

2. API starten:

```powershell
cd CampusConnect/backend
dotnet run --project .\CampusConnect.API\CampusConnect.API.csproj
```

3. Frontend starten:

```powershell
cd CampusConnect/frontend
npm start
```

4. Browser öffnen:

- Frontend: `http://localhost:4200`
- Swagger: `http://localhost:5135/swagger`

## Demo-Accounts

Alle Demo-Accounts verwenden lokal standardmäßig `CampusDemo2026!`. Die Accounts sind ausschließlich für lokale Demos gedacht.

| Rolle | Account | Demo-Zweck |
|---|---|---|
| Admin | `demo.admin@dhbw-loerrach.de` | Benutzer, Kurse, Admin-Bereich |
| Lecturer | `demo.technik@dhbw-loerrach.de` | Kurs-/Technikgruppen, Feed |
| Lecturer | `demo.wirtschaft@dhbw-loerrach.de` | Wirtschaftsgruppen, Feed |
| Student | `lena.tif25a@dhbw-loerrach.de` | Hauptdemo für Studierendenfluss |
| Student | `noah.wwi25a@dhbw-loerrach.de` | Gruppen- und Kursvergleich |
| Student | `mia.wdb25a@dhbw-loerrach.de` | Kontakt-/Gruppenvergleich |

## Kernflow für die Vorführung

| Schritt | Aktion | Erwarteter Nachweis |
|---|---|---|
| 1 | Mit `lena.tif25a@dhbw-loerrach.de` anmelden | Feed öffnet sich, Nutzerprofil ist in der Navbar sichtbar |
| 2 | Feed zeigen | Gruppenbasierte Beiträge, Schnellzugriffe, Tagesplan |
| 3 | Mensa öffnen | Wochen-/Tagesansicht oder freundlicher Fehler bei externer API-Störung |
| 4 | Stundenplan öffnen | Kursbezogene Vorlesungsansicht oder Kursauswahl |
| 5 | Noten öffnen | Bestehende Demo-Noten, Durchschnitt und Eingabeformular |
| 6 | Gruppen öffnen | Offizielle, Kurs-, Campus- und Entdecken-Ansicht |
| 7 | Einer öffentlichen Gruppe beitreten oder Gruppen-Detail öffnen | Join/Request/Leave- und Gruppenbeitragslogik sichtbar |
| 8 | Kontakte öffnen | Suchkarte, Suche ab drei Zeichen, Kontaktkarten |
| 9 | Profil öffnen | Anzeigename, Kurs und Kontaktdetails bearbeitbar |
| 10 | Logout | Sitzung endet, Login wird wieder erreichbar |

## Admin-Flow

| Schritt | Aktion | Erwarteter Nachweis |
|---|---|---|
| 1 | Mit `demo.admin@dhbw-loerrach.de` anmelden | Admin-Link ist sichtbar |
| 2 | Admin-Bereich öffnen | Übersicht mit Kennzahlen |
| 3 | Benutzer-Tab öffnen | Benutzerliste, Filter, Bearbeitungsdialog |
| 4 | Kurs-Tab öffnen | Kursliste und Kursanlage |
| 5 | Neuen Demo-Nutzer nur bei Bedarf anlegen | Initialpasswort wird vergeben; keine echten Daten verwenden |

## Rechtliche Seiten

Die folgenden Seiten sind ohne Login erreichbar und müssen in der Demo als Platzhalter erklärt werden:

- `/legal/impressum`
- `/legal/datenschutz`
- `/legal/nutzungsordnung`

## Automatisierter Smoke-Check

Der schnelle technische Demo-Check läuft im Frontend:

```powershell
cd CampusConnect/frontend
npm run e2e
```

Der Smoke-Test startet API und Frontend mit isolierter E2E-SQLite-Datenbank. Er ersetzt keine fachliche Live-Demo, deckt aber Login, Navigation, Admin-Zugriff und öffentliche Rechtsseiten ab.

## Fallbacks für die Vorführung

- Wenn externe Mensa-/Stundenplan-/Studienplanquellen nicht verfügbar sind, den freundlichen Fehlerzustand zeigen und auf Backend-Kapselung verweisen.
- Wenn eine lokale Datenbank unerwartete alte Demo-Daten enthält, für die Demo eine frische Datenbank verwenden oder die Playwright-E2E-Datenbank als technisches Referenzszenario nutzen.
- Falls Browser-Demo nicht möglich ist, Screenshots und Werbevideo aus [`media/werbevideo.md`](media/werbevideo.md) verwenden.
