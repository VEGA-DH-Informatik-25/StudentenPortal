# CampusConnect – Projektbeschreibung

## Projektidee

CampusConnect ist ein webbasiertes Studierendenportal für die DHBW Lörrach. Es bündelt
studiumsrelevante Informationen, soziale Funktionen und Hochschulservices an einem zentralen
Ort und ersetzt die aktuell genutzten Insellösungen aus WhatsApp-Gruppen, E-Mail-Verteilern
und Aushängen.

---

## Zielgruppe

| Gruppe | Beschreibung |
|---|---|
| Studierende (primär) | Alle eingeschriebenen Studierenden der DHBW Lörrach, insbesondere Erstsemester, die einen strukturierten Einstieg benötigen |
| Lehrbeauftragte (sekundär) | Dozierende, die Ankündigungen oder Informationen an Kurse kommunizieren möchten |
| Hochschulverwaltung (sekundär) | Mitarbeitende, die als Administratoren News und offizielle Informationen publizieren |

---

## Problem und Nutzen

### Ausgangsproblem

An der DHBW Lörrach fehlt eine einheitliche digitale Plattform für den Studienalltag.
Informationen verteilen sich unkontrolliert über inoffizielle WhatsApp-Gruppen, E-Mail-Verteiler
und physische Aushänge. Erstsemester finden keinen strukturierten Einstieg in den Studienalltag.
Lerngruppen entstehen rein zufällig. Prüfungstermine und Noten werden manuell in eigenen
Tabellen oder Notizen verwaltet.

### Nutzen durch CampusConnect

- Studierende haben einen einzigen Anlaufpunkt für alle studiumsrelevanten Informationen
- Erstsemester erhalten einen geführten Einstieg über einen Onboarding-Feed
- Lerngruppen können gezielt und nach Modul oder Kurs gebildet werden
- Der persönliche Prüfungskalender und Noten-Tracker reduzieren manuellen Aufwand
- Der Mensa-Speiseplan ist direkt integriert und täglich aktuell

---

## Kernfunktionen

### Authentifizierung und Profil
- Registrierung ausschließlich mit `@student.dhbw-loerrach.de` E-Mail-Adresse
- JWT-basierte Authentifizierung, kein dauerhafter Token im Browser-Speicher
- Profilseite mit Studiengang, Semester und Kurs

### News-Feed
- Zentraler Feed für Ankündigungen, Neuigkeiten und Beiträge
- Beiträge können von Studierenden, Lehrbeauftragten und Admins erstellt werden
- Kommentar- und Reaktionsfunktion

### Schwarzes Brett
- Pinnwand für Angebote, Gesuche und Hinweise (z. B. WG-Suche, Mitfahrgelegenheiten)
- Einträge laufen nach einem definierten Zeitraum automatisch ab

### Lerngruppen-Matching
- Erstellung und Suche von Lerngruppen gefiltert nach Modul, Kurs und Semester
- Beitrittsanfragen und einfache Gruppenverwaltung

### Mensa-Speiseplan
- Tages- und Wochenansicht des aktuellen Speiseplans der Mensa Lörrach
- Datenquelle: SWFR XML-API (`swfr.de/apispeiseplan`, Ort-ID 671)
- Anzeige von Preis, Allergenen und Kategorien (vegetarisch, vegan)

### Prüfungskalender
- Persönlicher Kalender für Prüfungstermine
- Erinnerungsfunktion (Push-Benachrichtigung im Browser)

### Noten-Tracker
- Manuelle Erfassung von Noten und ECTS-Punkten
- Berechnung des aktuellen Notendurchschnitts

### Admin-Bereich
- Verwaltung von Nutzern und Rollen
- Erstellen und Pinnen offizieller Ankündigungen

---

## Technologie-Stack

| Bereich | Technologie |
|---|---|
| Frontend | Angular 21, Standalone Components, Signals, zoneless Change Detection, SCSS |
| Backend | ASP.NET Core 9, Clean Architecture, MediatR, CQRS |
| Datenbank | PostgreSQL, Entity Framework Core 9 |
| Authentifizierung | JWT Bearer, Refresh Token (In-Memory) |
| Externe API | SWFR Mensa XML-API |
| Containerisierung | Docker, docker-compose |
| CI/CD | GitHub Actions |
| Testing | xUnit, Jasmine/Karma, Cypress |

---

## Abgrenzung – Was CampusConnect nicht ist

| Thema | Abgrenzung |
|---|---|
| Mobile App | Keine native iOS- oder Android-App; die Web-App ist responsive, aber kein PWA-Schwerpunkt |
| LMS / Moodle-Ersatz | Keine Lernplattform, kein Upload von Lernmaterialien, keine Kursverwaltung |
| Echtzeit-Chat | Kein privates Messaging oder Live-Chat zwischen Studierenden |
| Notenverwaltung der Hochschule | Keine Anbindung an Dualis oder offizielle Notensysteme; nur manueller persönlicher Tracker |
| Multi-Mandant-Betrieb | Keine Unterstützung anderer Hochschulen; Scope ist ausschließlich DHBW Lörrach |
| Gamification | Keine Punkte- oder Badge-Systeme im MVP |

---

## Projektrahmendaten

| | |
|---|---|
| Projekttitel | CampusConnect |
| Hochschule | DHBW Lörrach |
| Teamgröße | 4 Personen |
| Projektrahmen | Anwendungsprojekt Informatik |
| Meilenstein 1 – Deadline | 27.04.2026 |
| Geplanter Projektstart | Mai 2026 |
| Geplantes Projektende | Juli 2026 |
| Repository | github.com/[organisation]/CampusConnect |
