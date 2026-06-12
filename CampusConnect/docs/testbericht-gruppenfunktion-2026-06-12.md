# Testbericht: Gruppenfunktion CampusConnect

## 1. Testumgebung

- Datum: 2026-06-12
- Frontend-URL: `http://localhost:4200` (HTTP 200)
- Backend-URL: `http://localhost:5135/swagger` (HTTP 200)
- Verwendete Demo-Accounts:
  - `demo.admin@dhbw-loerrach.de` / `CampusDemo2026!` / Admin
  - `demo.technik@dhbw-loerrach.de` / `CampusDemo2026!` / Lecturer
  - `demo.wirtschaft@dhbw-loerrach.de` / `CampusDemo2026!` / Lecturer
  - `lena.tif25a@dhbw-loerrach.de` / `CampusDemo2026!` / Student
  - `noah.wwi25a@dhbw-loerrach.de` / `CampusDemo2026!` / Student
  - `mia.wdb25a@dhbw-loerrach.de` / `CampusDemo2026!` / Student
- Getestete Rollen: Student, Lecturer, Admin
- Nicht getestet: Verwaltung/Management, weil in `CampusConnect/docs/demo-data.md` kein Verwaltungs-Demo-Account dokumentiert ist.
- Browser: Codex In-App-Browser sollte verwendet werden, konnte in dieser Sitzung aber wegen eines lokalen Sandbox-Startfehlers nicht initialisiert werden. Die UI wurde daher anhand der laufenden lokalen App, der Angular-Routen/Templates/Services und echter API-Requests gegen Backend/Frontend geprüft. Browser-Konsole und visuelle Klickprüfung konnten nicht zuverlässig getestet werden.
- Datenzustand: Die lokale SQLite-Datenbank war nicht komplett frisch; es waren neben Seed-Daten auch lokale Gruppen vorhanden. Alle während dieses Tests angelegten `QA...`-Gruppen wurden am Ende wieder gelöscht.

## 2. Kurzfazit

Die Gruppenfunktion ist technisch schon gut nutzbar: Gruppenübersicht, Typen, Rollenflags, Einstellungen, offene Beitritte, Beitrittsanfragen, Einladungen, Moderatorrechte, Beitragsfreigabe und Kommentarregeln werden serverseitig weitgehend korrekt durchgesetzt.

Kritisch sind vor allem Konzeptabweichungen und Lücken: Lecturer dürfen Kursgruppen erstellen, obwohl die Testvorgabe Kursgruppen nur Admins erlaubt. Außerdem gibt es keinen auffindbaren "Gruppe verlassen"-Flow. Mehrere verbotene Aktionen werden zwar blockiert, antworten aber mit `400 Bad Request` statt semantisch sauber mit `403 Forbidden`.

Aus Nutzersicht wirkt die Funktion für eine Demo geeignet. Für echte Nutzung fehlen noch visuelle End-to-End-Prüfung, sauberere Statuscodes, ein Leave-Flow und eine Entscheidung, ob Lecturer wirklich Kursgruppen erstellen dürfen.

## 3. Getestete Bereiche

| Bereich | Ergebnis | Bemerkung |
|---|---|---|
| Gruppenübersicht | funktioniert | Route `/groups`, Menüeintrag und API `/api/groups` vorhanden; Rollen sehen unterschiedliche Gruppenmengen. |
| Gruppenerstellung | teilweise | Campus für alle getesteten Rollen, Official für Admin, Course für Admin und Lecturer. Lecturer-Course weicht von Testvorgabe ab. |
| Berechtigungen | teilweise | Server blockt verbotene Aktionen, aber einige Permission-Fehler kommen als `400` statt `403`. |
| Gruppenrollen | funktioniert | Owner, Moderator, Member werden gesetzt und wirken plausibel. Moderator kann verwalten, aber keine Moderatoren ernennen und nicht löschen. |
| Beiträge | funktioniert | Member-Posting, Sperre, Pending-Status und Freigabe geprüft. |
| Kommentare | funktioniert | Gruppenweite Kommentarregel und Post-Kommentarregel wirken; Nichtmitglieder werden geblockt. |
| Beitritt/Mitgliedschaft | teilweise | Offen, Anfrage und Einladung funktionieren. Gruppe verlassen ist nicht umgesetzt/im UI nicht auffindbar. |
| UI/UX | mittel | Struktur, Texte und States sind vorhanden; visuelle Browserprüfung konnte nicht abgeschlossen werden. |

## 4. Was funktioniert bereits?

- Frontend und Backend laufen auf den erwarteten lokalen Ports.
- `/groups` ist als Route vorhanden und im Navigationsmenü verlinkt.
- Die Gruppenübersicht unterscheidet Official, Course und Campus über Tabs, Typ-Labels und Gruppenmetadaten.
- Bestehende Gruppen werden rollenabhängig geliefert:
  - Admin: 31 Gruppen, alle verwaltbar.
  - Lecturer `demo.technik`: 13 Gruppen, 2 verwaltbar, 5 direkt beitretbar.
  - Student `lena`: 12 Gruppen, 3 verwaltbar, 3 direkt beitretbar.
  - Student `noah`: 12 Gruppen, 1 verwaltbar, 3 direkt beitretbar, 1 anfragbar.
  - Student `mia`: 12 Gruppen, 0 verwaltbar, 4 direkt beitretbar.
- Nicht authentifizierte Requests auf `/api/groups` werden mit `401` blockiert.
- Admin kann Official-, Course- und Campus-Gruppen erstellen.
- Student kann Campus-Gruppen erstellen.
- Student kann Official- und Course-Gruppen nicht erstellen.
- Lecturer kann Campus-Gruppen erstellen.
- Lecturer kann Official-Gruppen nicht erstellen.
- Offene Gruppen erlauben direkten Beitritt.
- Gruppen mit `RequestRequired` zeigen für Nichtmitglieder `canRequestJoin`; Antrag wird gespeichert und Owner sieht ihn in Settings.
- Owner kann Beitrittsanfragen annehmen.
- Invite-only-Gruppen blocken direkten Beitritt; nach Einladung kann der Nutzer die Einladung annehmen.
- Owner kann Mitglieder hinzufügen, entfernen und Moderatorrollen vergeben.
- Moderator kann Mitglieder verwalten und entfernen.
- Moderator kann keine weiteren Moderatoren ernennen.
- Moderator kann die Gruppe nicht löschen.
- Admin kann Gruppen systemweit verwalten, auch ohne Gruppenmitglied zu sein.
- Mitglieder können keine Settings öffnen oder ändern.
- Normale Mitglieder werden serverseitig am Posten gehindert, wenn `allowStudentPosts=false`.
- Beiträge von normalen Mitgliedern werden bei `requiresApproval=true` als `Pending` gespeichert und sind im normalen Feed nicht sichtbar.
- Owner sieht Pending-Beiträge und kann sie freigeben.
- Nach Freigabe ist der Beitrag im Feed sichtbar.
- Kommentare funktionieren, wenn Gruppen- und Post-Regel Kommentare erlauben.
- Kommentare werden blockiert, wenn Gruppenkommentare deaktiviert sind.
- Nichtmitglieder können nicht kommentieren.
- Owner/Manager können fremde Kommentare löschen.
- Einstellungen bleiben nach erneutem Laden der Settings über API gespeichert.

## 5. Was funktioniert noch nicht?

### Problem 1: In-App-Browser konnte nicht initialisiert werden

- Beschreibung: Die angeforderte Browserprüfung konnte nicht vollständig als echte Klickprüfung im In-App-Browser durchgeführt werden.
- Schritte zum Reproduzieren: Browser-Skill initialisieren.
- Erwartetes Verhalten: In-App-Browser öffnet lokale App und erlaubt DOM-/Klickprüfung.
- Tatsächliches Verhalten: Lokaler Sandbox-Fehler beim Browser-Start.
- Rolle/Account: Nicht rollenabhängig.
- Schweregrad: mittel

### Problem 2: Verwaltung/Management nicht testbar

- Beschreibung: In `demo-data.md` ist kein Verwaltungs- oder Management-Account dokumentiert.
- Schritte zum Reproduzieren: Demo-Accounts in `CampusConnect/docs/demo-data.md` prüfen.
- Erwartetes Verhalten: Für die Rolle Verwaltung existiert ein Demo-Login.
- Tatsächliches Verhalten: Nur Admin, Lecturer und Student sind dokumentiert.
- Rolle/Account: Verwaltung/Management.
- Schweregrad: mittel

### Problem 3: Gruppe verlassen nicht umgesetzt/im UI nicht auffindbar

- Beschreibung: Es gibt Endpoints zum Entfernen von Mitgliedern durch Manager, aber keinen erkennbaren Self-Service-Flow zum Verlassen einer Gruppe.
- Schritte zum Reproduzieren: Mitglied einer Campus-Gruppe werden und Detail-/Settings-Flow prüfen.
- Erwartetes Verhalten: Mitglied kann Gruppe verlassen; Owner-Leave ist geregelt oder verständlich blockiert.
- Tatsächliches Verhalten: Kein Leave-Button, kein Self-Leave-Endpoint auffindbar.
- Rolle/Account: Student/Mitglied.
- Schweregrad: hoch

### Problem 4: Einige Permission-Fehler verwenden `400` statt `403`

- Beschreibung: Verbotene Aktionen werden häufig technisch blockiert, aber mit `400 Bad Request` beantwortet.
- Schritte zum Reproduzieren: Student erstellt Official/Course-Gruppe; Mitglied postet bei `allowStudentPosts=false`; Nichtmitglied kommentiert.
- Erwartetes Verhalten: `403 Forbidden` bei fehlenden Rechten.
- Tatsächliches Verhalten: `400` mit Fehlermeldung, z. B. `This global role cannot create this group type.` oder `Permission denied.`
- Rolle/Account: Student, Nichtmitglied.
- Schweregrad: mittel

## 6. Was funktioniert anders als erwartet?

- Lecturer dürfen Kursgruppen erstellen. Die Testvorgabe erwartet Kursgruppen nur für Admins. UI und Backend sind dabei konsistent miteinander: Beide erlauben Lecturer den Course-Typ.
- Management dürfte laut Code Official- und Course-Gruppen erstellen, konnte aber mangels Demo-Account nicht verifiziert werden. Die Testvorgabe erlaubt Official/Course nur Admin.
- Admins verwalten Gruppen systemweit auch ohne Gruppenrolle. Das ist technisch sauber als `isSystemAdminAccess=true` gekennzeichnet, kann aber für Nutzer erklärungsbedürftig sein.
- Bei Course-Gruppen ist `canDelete` für Lecturer-Owner `false`; Admin kann sie löschen. Das ist plausibel wegen Kurssynchronisierung, sollte im Produktkonzept klar benannt sein.

## 7. Berechtigungstest

| Rolle | Aktion | Erwartet | Tatsächlich | Ergebnis |
|---|---|---|---|---|
| Ohne Login | Gruppen abrufen | verboten | `401` | funktioniert |
| Student | Campus-Gruppe erstellen | erlaubt | `200`, Gruppe erstellt | funktioniert |
| Student | Offizielle Gruppe erstellen | verboten | `400`, blockiert | funktioniert, Statuscode verbesserbar |
| Student | Kursgruppe erstellen | verboten | `400`, blockiert | funktioniert, Statuscode verbesserbar |
| Lecturer | Campus-Gruppe erstellen | erlaubt | `200`, Gruppe erstellt | funktioniert |
| Lecturer | Offizielle Gruppe erstellen | verboten | `400`, blockiert | funktioniert, Statuscode verbesserbar |
| Lecturer | Kursgruppe erstellen | laut Vorgabe verboten | `200`, Gruppe erstellt | anders als erwartet |
| Admin | Campus-Gruppe erstellen | erlaubt | `200`, Gruppe erstellt | funktioniert |
| Admin | Offizielle Gruppe erstellen | erlaubt | `200`, Gruppe erstellt | funktioniert |
| Admin | Kursgruppe erstellen | erlaubt | `200`, Gruppe erstellt | funktioniert |
| Student/Nichtmitglied | RequestRequired-Gruppe beitreten | Anfrage statt Mitgliedschaft | Pending-Anfrage gespeichert | funktioniert |
| Owner | Beitrittsanfrage annehmen | erlaubt | Mitglied wurde hinzugefügt | funktioniert |
| Member | Gruppeneinstellungen öffnen | verboten | `403` | funktioniert |
| Member | Gruppeneinstellungen ändern | verboten | `403` | funktioniert |
| Owner | Member zu Moderator machen | erlaubt | Rolle `Moderator` gesetzt | funktioniert |
| Moderator | Settings-Details öffnen | erlaubt | `200` | funktioniert |
| Moderator | Settings ändern | verboten/eingeschränkt | `canEditSettings=false` | funktioniert |
| Moderator | Mitglied hinzufügen | erlaubt | Mitglied wurde hinzugefügt | funktioniert |
| Moderator | Moderator ernennen | verboten | `400`, blockiert | funktioniert, Statuscode verbesserbar |
| Moderator | Gruppe löschen | verboten | `403` | funktioniert |
| Admin | Fremde Gruppe verwalten | erlaubt | `isSystemAdminAccess=true`, alle Manage-Flags true | funktioniert |
| Member | Posten bei `allowStudentPosts=false` | verboten | `400`, blockiert | funktioniert, Statuscode verbesserbar |
| Member | Posten bei `requiresApproval=true` | Pending | `Pending`, nicht im normalen Feed | funktioniert |
| Owner | Pending-Beitrag freigeben | erlaubt | Beitrag wird `Published` | funktioniert |
| Nichtmitglied | Kommentieren | verboten | `400 Permission denied.` | funktioniert, Statuscode verbesserbar |

## 8. UI-/UX-Einschätzung

Verständlich:

- Gruppentypen sind über Tabs und Labels als Offiziell, Kurse und Campus getrennt.
- Erstellmaske enthält Name, Beschreibung, Gruppentyp, Zielgruppe, Beitrittsregel, Post-Rechte, Kommentare, Freigabe und Sichtbarkeit.
- Kursgruppen zeigen zusätzlich Kurscode.
- Offizielle Gruppen zeigen zusätzlich offizielle Kategorie.
- Die Settings-Seite ist fachlich sinnvoll gegliedert in Regeln, Mitglieder, Moderation, Beitrittsanfragen, Einladungen, Mitglieder hinzufügen und Gefahrenbereich.
- Erfolgs-, Lade- und Fehlertexte sind über Übersetzungen vorhanden.
- Deutsche Übersetzungen existieren für die geprüften Gruppen-Texte.
- Gefährliche Aktion "Gruppe löschen" hat eine Bestätigung im UI.

Verwirrend oder fehlend:

- Kein "Gruppe verlassen"-Flow auffindbar.
- Der Begriff `Campusgruppe` ist gut, aber "Zielgruppe" und "Sichtbarkeit" könnten beim Erstellen mit kurzen Hilfetexten noch klarer werden.
- Fehler aus Backend-Validierung werden teilweise direkt als englische Backend-Texte angezeigt, z. B. `This global role cannot create this group type.` Diese sollten lokalisiert werden.
- Visuelle Mobile-/Konsole-/Netzwerkprüfung konnte wegen Browser-Blocker nicht durchgeführt werden.
- Bei Admin-Zugriff ohne Mitgliedschaft ist der Hinweis vorhanden, aber die Unterscheidung zwischen Systemrechten und Gruppenrolle sollte in Demos bewusst erklärt werden.

## 9. Sicherheits- und Backend-Einschätzung

- Authentifizierung wird serverseitig durchgesetzt: `/api/groups` ohne Token liefert `401`.
- Gruppenverwaltung wird serverseitig geprüft: Member erhalten `403` für Settings.
- Admin-Rechte werden serverseitig korrekt erkannt und nicht nur im UI entschieden.
- Student kann Official/Course-Gruppen nicht per manipulierter API-Anfrage erstellen.
- Lecturer kann Official-Gruppen nicht per manipulierter API-Anfrage erstellen.
- Lecturer kann Course-Gruppen erstellen; das ist eine Konzeptfrage, keine UI-Lücke, weil Backend und UI es erlauben.
- Beitritt ist serverseitig abgesichert: Invite-only blockt direkten Join und erlaubt Join nach Einladung.
- Pending-Beiträge sind normalen Mitgliedern vor Freigabe verborgen.
- Pending-Moderationsliste ist für normale Mitglieder mit `403` blockiert.
- Kommentare und Posten werden serverseitig gegen Gruppenregeln geprüft.
- Auffällig: mehrere Permission-Probleme laufen über `BadRequest`/`400`. Für API-Konsumenten und Security-Tests wäre `403` eindeutiger.
- Es wurden keine sensiblen Daten jenseits normaler Demo-Profile für die geprüften Gruppen beobachtet. Eine vollständige Browser-Network-Prüfung konnte nicht erfolgen.

## 10. Verbesserungsvorschläge

### Hohe Priorität

- Klären und vereinheitlichen: Dürfen Lecturer Kursgruppen erstellen oder nur Admins? Danach UI, Backend, Tests und Doku angleichen.
- Self-Service-Flow zum Verlassen einer Gruppe ergänzen, inklusive klarer Regel für Owner.
- Permission-Fehler im Feed- und Gruppenbereich konsequent als `403 Forbidden` statt `400 Bad Request` zurückgeben.
- Einen Management/Verwaltung-Demo-Account in `demo-data.md` und Seeder ergänzen.

### Mittlere Priorität

- Backend-Fehlermeldungen lokalisieren oder auf stabile Fehlercodes mappen, damit UI immer deutsche Texte anzeigen kann.
- UI-Tests oder E2E-Flows für Gruppen erstellen: Create, Join, Request, Invite, Moderator, Pending Post, Comments.
- In Settings deutlicher zeigen, welche Rechte Moderator vs Owner hat.
- Bei Course-Gruppen erklären, warum Mitgliedschaft kursverwaltet ist und warum Lecturer sie nicht löschen kann.

### Niedrige Priorität

- Kleine Hilfetexte bei Sichtbarkeit, Beitrittsregel und Beitragsfreigabe ergänzen.
- Leere Zustände und Erfolgsmeldungen visuell im Browser auf Mobilgrößen prüfen.
- Browser-Konsole und Network-Tab in einem funktionierenden Browserlauf nachtesten.

## 11. Gesamtbewertung

- Reifegrad der Gruppenfunktion: mittel
- Für Demo geeignet: ja, mit Hinweis auf die Konzeptabweichung bei Lecturer/Kursgruppen und den fehlenden Leave-Flow
- Für echte Nutzung geeignet: teilweise
- Wichtigste nächste Schritte:
  - Rollenregel für Kurs- und offizielle Gruppen finalisieren.
  - Gruppe-verlassen-Funktion implementieren.
  - Permission-Statuscodes bereinigen.
  - Verwaltung-Demo-Account ergänzen.
  - Visuelle Browser-/E2E-Prüfung nachholen, sobald der In-App-Browser verfügbar ist.
