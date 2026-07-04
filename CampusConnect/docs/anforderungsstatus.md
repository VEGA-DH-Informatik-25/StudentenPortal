# Anforderungsstatus MVP

Stand: 2026-07-04

Diese Matrix bewertet die Must-have-Kriterien aus [`../../prd-mvp.md`](../../prd-mvp.md) gegen den aktuellen Code-, Test- und Dokumentationsstand. Live-Code, automatisierte Tests und die API-Dokumentation bleiben fuer technische Details massgeblich.

**Kurzfazit:** Der MVP ist erfuellt. Alle Must-have-Bereiche sind implementiert und durch Backend-, Frontend- oder Playwright-Smoke-Tests nachweisbar. Bekannte Einschraenkungen betreffen Betrieb, externe Datenquellen, rechtliche Platzhalter, Build-Warnungen und eine NuGet-Advisory, nicht fehlende MVP-Kernfunktionen.

## Must-have-Matrix

| Prioritaet | Bereich | Status | Nachweis / Kommentar |
|---|---|---|---|
| Must | Nutzeranlage durch Admin | erfuellt | Admin-API und Admin-UI legen Konten mit Rolle, Kurs, Aktivstatus und Initialpasswort an. E-Mail-Domain `@dhbw-loerrach.de` ist aktuelles Anforderungskriterium. |
| Must | Login/Logout und geschuetzte Bereiche | erfuellt | JWT plus HttpOnly-Cookie, aktive Nutzerpruefung, Logout und Inaktivitaets-Timeout sind implementiert und getestet. Oeffentliche Selbstregistrierung existiert nicht. |
| Must | Initialpasswortwechsel / Onboarding | erfuellt | Erstlogin und Admin-Passwort-Reset setzen `MustChangePassword`; Passwortwechsel und Onboarding-Abschluss sind API- und UI-seitig vorhanden. |
| Must | Rollen und Berechtigungen | erfuellt | Globale Rollen und Gruppenrollen sind getrennt. Lecturer verwalten zugewiesene Kursgruppen; Official-Posting erfolgt ueber passende Gruppenrolle oder Management/Admin. |
| Must | Profil und Kurszuordnung | erfuellt | Profil erlaubt Anzeigename, Telefon und Standort. Kurs ist sichtbar und wird administrativ ueber Admin-Endpunkte verwaltet. Selbstservice-Kurswechsel wird abgelehnt. |
| Must | News-Feed | erfuellt | Gruppenbasierter Feed mit Posts, Kommentaren, Reaktionen, Attachments, Moderation und serverseitigen Berechtigungsflags. |
| Must | Gruppen | erfuellt | Course-, Official- und Campus-Gruppen, Join/Request/Invite/Leave, Gruppenrollen, Einstellungen, Mitgliederverwaltung und Moderation sind umgesetzt. |
| Must | Mensa | erfuellt mit externem Betriebsrisiko | Backend kapselt SWFR-API; UI zeigt Wochen-/Tagesansicht und Fehlerzustaende. Verfuegbarkeit haengt von externer Quelle/API-Key ab. |
| Must | Noten-Tracker | erfuellt | Manuelle Noten mit ECTS, Durchschnitt, Simulation und Loeschen sind vorhanden. |
| Must | Stundenplan | erfuellt mit externem Betriebsrisiko | Backend ruft iCal je Profilkurs oder explizitem Kurs ab; UI bietet Tages-, Wochen- und Listenansicht. |
| Must | Kontaktbuch | erfuellt | Suche und Favoriten zeigen Name, E-Mail, Kurs, Studiengang, Telefon und Standort. Profilnotizen werden nicht geliefert oder durchsucht. |
| Must | Admin-Bereich | erfuellt | Admin kann Nutzer erstellen, bearbeiten, Status/Rolle/Kurs aendern, Passwort zuruecksetzen und Kurse anlegen/listen. |
| Must | Onboarding-Feed / Guided Start | erfuellt | Onboarding-Seite zeigt Willkommen, Passwortwechsel, Tour-Schritte und Gruppenvorschlaege; danach startet die Guided Tour im Feed. Eine Gruppen-Erklaerung kann beim ersten Gruppenaufruf starten. |
| Must | Laptop/iPad-Zuschnitt | erfuellt | Playwright-Smoke-Tests laufen auf Desktop Chrome, iPad Portrait und iPad Landscape und pruefen zentrale Flows ohne horizontalen Overflow. |

## Could-have- und Zusatzumfang

| Prioritaet | Bereich | Status | Nachweis / Kommentar |
|---|---|---|---|
| Could | Dark Mode | erfuellt | Theme-Service mit `system`, `light`, `dark`, Navbar-Steuerung und Tests ist vorhanden. |
| Could | Pruefungskalender | erfuellt | Kalenderroute, API-Endpunkte, persoenliche Eintraege, Demo-Seeding und Tests sind vorhanden. Keine Push-Reminder oder offiziellen Pruefungsamtsimporte. |
| Could | Smartphone-Anpassung | nicht erforderlich | Smartphone war laut PRD optional. Pflichtabdeckung bleibt Laptop/iPad. |
| Zusatz | Rechtliche Seiten | Platzhalter | `/legal/impressum`, `/legal/datenschutz`, `/legal/nutzungsordnung` sind erreichbar, aber nicht rechtlich final freigegeben. |
| Zusatz | Development-Demo-Daten | erfuellt | Seeder legt Demo-Kurse, Demo-Nutzer, Gruppen, Feed, Noten und Kalenderdaten fuer lokale Demos an. |
| Zusatz | Mehrsprachigkeit | erfuellt | Frontend-Texte liegen in Deutsch, Englisch und Franzoesisch vor; Translation-Key-Vollstaendigkeit wird getestet. |

## Abgleich Produkt und Anforderungen

Kleine Abweichungen zwischen aelterem Produkttext, PRD und Live-Code wurden anforderungsseitig eingeordnet:

- Nutzeranlage ist auf `@dhbw-loerrach.de` beschraenkt; die PRD folgt damit der implementierten Domain-Regel.
- Kurswechsel sind administrativ und nicht im Profil selbst pflegbar; die PRD beschreibt die Kurszuordnung jetzt als sichtbar, aber nicht selbst aenderbar.
- Lecturer erhalten Official-Posting nicht pauschal global, sondern ueber Gruppenrolle oder Management/Admin; die PRD ist auf dieses Berechtigungsmodell angepasst.
- Kontaktbuch liefert keine Profilnotizen; die PRD und dieser Status behandeln Profilnotizen als Nicht-Ziel.
- Der Pruefungskalender ist als erfuelltes Could-have/Zusatzfeature bewertet. Nicht im MVP sind weiterhin Push-Reminder und offizielle Pruefungsamts-/Dualis-Integrationen.
- Dashboard-Welcome-News, persistente Gruppenvorschlaege und Badge-Logik sind optionale Onboarding-Ausbaustufen, keine offenen Must-have-Luecken.

## Aktuelle Verifikation

Am 2026-07-04 wurden folgende Befehle lokal ausgefuehrt:

| Befehl | Ergebnis |
|---|---|
| `dotnet test .\CampusConnect.slnx --no-restore` | Bestanden: 107 Application-Tests und 45 API-Tests. Warnung `NU1903` fuer `Microsoft.OpenApi 2.4.1`. |
| `npm.cmd test -- --watch=false` | Bestanden: 35 Testdateien, 166 Tests. |
| `npm.cmd run build` | Bestanden: Produktionsbuild erfolgreich; SCSS-Budgetwarnungen fuer `navbar`, `feed-page`, `timetable-page`, `admin-page`, `group-settings-page`. |
| `npm.cmd run e2e` | Bestanden: 9 Playwright-Smoke-Tests auf Desktop, iPad Portrait und iPad Landscape. |

Hinweis: Angular-Test, Build und E2E scheiterten im eingeschraenkten Sandbox-Dateisystem zunaechst an Pfadauflösung (`Cannot read directory "../../.."`). Ausserhalb der Sandbox liefen dieselben Befehle erfolgreich.

## Bekannte Einschraenkungen

- Externe Mensa- und Stundenplanquellen koennen in Demos ausfallen; Fehlerzustaende sind implementiert, die Verfuegbarkeit bleibt aber ein Betriebsrisiko.
- Rechtliche Seiten sind Platzhalter und benoetigen finale Angaben und Freigaben.
- `docker-compose.yml` ist weiterhin nicht produktionsreif.
- Produktiver Betrieb, SSO, Monitoring, Backups und Deployment-/Betriebshandbuch sind nicht umgesetzt.
- Lokale SQLite-Datenbanken koennen Demo- oder Testdaten enthalten und muessen vor Commits bewusst geprueft werden.
- SCSS-Budgetwarnungen und die `Microsoft.OpenApi`-Advisory sollten vor einer echten Produktions- oder Abgabeentscheidung bewertet werden.
