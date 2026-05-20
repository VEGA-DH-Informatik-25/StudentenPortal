# UI-Prototyp und Seitenstruktur

Stand: 2026-05-20

Dieses Dokument beschreibt die aktuell im Repository vorhandene Benutzeroberfläche von CampusConnect. Es ersetzt keine grafischen Mockups, macht aber Seitenstruktur, Navigation und zentrale UI-Komponenten aus Kundensicht nachvollziehbar.

## 1. Prototyp-Umfang

Der Prototyp ist eine Angular-Webanwendung unter `CampusConnect/frontend/`. Die Oberfläche ist über `http://localhost:4200` erreichbar, wenn API und Frontend lokal laufen. API-Aufrufe an `/api` werden über `proxy.conf.json` an `http://localhost:5135` weitergeleitet.

Für eine realistische Präsentation nutzt die API in der Development-Umgebung Demo-Daten aus `DevelopmentDemoDataSeeder`, sofern `DemoData:Enabled` aktiv ist. Dadurch stehen Beispielnutzer, Kurse, Gruppen, Feed-Beiträge, Noten und Prüfungseinträge zur Verfügung.

## 2. Informationsarchitektur

```text
Login / Registrierung
└── Geschützte App-Shell
    ├── Feed
    ├── Mensa
    ├── Stundenplan
    ├── Prüfungen
    ├── Noten
    ├── Gruppen
    │   ├── Gruppenübersicht
    │   ├── Gruppendetail
    │   └── Gruppeneinstellungen
    ├── Kontakte
    ├── Profil
    └── Admin (nur Admin-Rolle)
```

## 3. Navigation

Die Hauptnavigation befindet sich in der oberen Navigationsleiste:

- Logo/Marke `CampusConnect` führt zum Feed.
- Direkte Navigationspunkte: Mensa, Stundenplan, Prüfungen, Noten, Gruppen, Kontakte.
- Der Admin-Link wird nur für Nutzer mit Rolle `Admin` angezeigt.
- Rechts befindet sich ein Profilmenü mit Initialen, Name, Kurskontext, Profildetails, Link zur Profilbearbeitung und Abmelden-Aktion.

Die Datei `layout/sidebar/sidebar.html` enthält aktuell nur einen Platzhalter und ist nicht Teil der produktiven Navigation.

## 4. Seiten aus Nutzersicht

### 4.1 Login und Registrierung

Zweck: Einstieg in die Anwendung.

Aktuelle UI:

- Tab-Umschaltung zwischen `Anmelden` und `Registrieren`.
- Login mit E-Mail und Passwort.
- Registrierung mit E-Mail, Passwort, Anzeigename und Kursauswahl.
- Kursauswahl wird aus dem Backend geladen.
- Fehlermeldungen werden direkt in der Login-Karte angezeigt.

### 4.2 Feed

Zweck: Zentraler Nachrichten- und Gruppenfeed.

Aktuelle UI:

- Anzeige von Beiträgen aus berechtigten Gruppen.
- Beitragserstellung für Nutzer mit Schreibrecht.
- Kommentare und Emoji-Reaktionen.
- Gruppenkontext pro Beitrag.
- Löschaktionen für eigene oder berechtigte Inhalte.

### 4.3 Mensa

Zweck: Mensa-Speiseplan der DHBW Lörrach.

Aktuelle UI:

- Wochen-/Tagesübersicht aus der Backend-Mensa-API.
- Ladezustand und Fehlerzustand.
- Darstellung der Gerichte mit verfügbaren Metadaten aus der SWFR-Quelle.

### 4.4 Stundenplan

Zweck: Kursbezogene Vorlesungsübersicht.

Aktuelle UI:

- Lädt den Stundenplan für den Profilkurs oder einen gewählten Kurs.
- Unterstützt ein Zeitfenster über Backend-Parameter.
- Zeigt Termine aus serverseitig geladenen iCal-Daten.

### 4.5 Prüfungen

Zweck: Persönlicher Prüfungskalender.

Aktuelle UI:

- Liste persönlicher Prüfungseinträge.
- Formular zum Hinzufügen von Prüfungsterminen.
- Löschen eigener Einträge.

### 4.6 Noten

Zweck: Persönlicher Noten-Tracker.

Aktuelle UI:

- Anzeige gespeicherter Noteneinträge.
- Formular zum Erfassen neuer Noten.
- Berechnung und Anzeige des Durchschnitts.
- Löschen eigener Einträge.
- Backend kann Studienplaninformationen für den Profilkurs bereitstellen.

### 4.7 Gruppen

Zweck: Kurs-, offizielle und Campusgruppen finden und nutzen.

Aktuelle UI:

- Gruppenübersicht mit berechtigten und entdeckbaren Gruppen.
- Beitritt zu öffentlichen Campusgruppen.
- Erstellung eigener Campusgruppen.
- Gruppendetail mit Beitragskontext.
- Einstellungen für berechtigte Nutzer: Posting-Regeln, Kommentare, Sichtbarkeit, Mitglieder und Mitgliedsrechte.

### 4.8 Kontakte

Zweck: Kontaktbuch für Personen im CampusConnect-Kontext.

Aktuelle UI:

- Suche nach Name, E-Mail, Kurs, Studiengang und Profildetails.
- Anzeige von Name, E-Mail, Kurs, Studiengang, Semester und optionalen Kontaktdaten.

### 4.9 Profil

Zweck: Eigenes Nutzerprofil pflegen.

Aktuelle UI:

- Bearbeitung von Anzeigename, Kurs, Telefon, Standort und Profilnotiz.
- Kurswechsel synchronisiert Studiengang und Semester über den Kurskatalog.
- Profilinformationen erscheinen im Navigationsmenü.

### 4.10 Admin

Zweck: Verwaltung von Benutzern und Kursen.

Aktuelle UI:

- Kursverwaltung mit Kurscode, Studiengang und Semester.
- Benutzerliste mit E-Mail, Anzeigename, Studieninformationen, Rolle und Erstellungsdatum.
- Änderung von Rolle und Kurs.
- Löschen von Nutzern.

Aktuelle Lücke:

- Direkte Nutzeranlage und Passwort-Reset durch Admin sind noch nicht implementiert.

## 5. Zentrale UI-Komponenten

| Komponente/Bereich | Funktion |
|---|---|
| App-Shell | Geschützter Rahmen für alle angemeldeten Seiten |
| Navbar | Hauptnavigation, Profilmenü und Logout |
| Feature Pages | Route-level Seiten für Feed, Mensa, Kalender, Stundenplan, Noten, Gruppen, Kontakte, Profil und Admin |
| Shared UI | Wiederverwendbare Komponenten wie Ladeanzeige und Fehlermeldung |
| Guards | Authentifizierungs- und Admin-Zugriffsschutz |
| Interceptors | Auth-Token-/Cookie-Unterstützung und zentrale Fehlerbehandlung |

## 6. Demo-Daten für die Präsentation

Die Development-Demo-Daten enthalten unter anderem:

- Admin-, Lecturer- und Student-Beispielkonten.
- Mehrere DHBW-Kurse.
- Kursgruppen, offizielle Gruppen und Campusgruppen.
- Beispielhafte Feed-Beiträge mit Kommentaren und Reaktionen.
- Persönliche Noten- und Prüfungseinträge.

Die konkreten Konten und das Default-Demo-Passwort sind in `docs/demo-data.md` dokumentiert.

## 7. Bekannte UI-Lücken

| Bereich | Lücke |
|---|---|
| Sidebar | Aktuell nur Platzhalter, Navigation läuft über die Navbar |
| Onboarding | Konzeption vorhanden, aber keine fertige UI |
| Admin | Keine UI für Admin-Nutzeranlage oder Passwort-Reset |
| Beitrag-Freigabe | Einstellung existiert, aber kein vollständiger Moderationsworkflow |
| Grafische Mockups | Repository enthält primär die laufende Webanwendung; separate Figma-/Excalidraw-Dateien sind nicht abgelegt |
