# Produktanforderungsdokument (PRD) – CampusConnect MVP

Stand: 2026-05-20

Dieses Dokument bündelt die verbindlichen funktionalen Anforderungen für den Prototyp von CampusConnect. Die Priorisierung folgt MoSCoW (`Must-have`, `Should-have`, `Could-have`, `Won't-have`) und bildet den aktuellen Repository-Stand sowie die erkennbaren nächsten Ausbauschritte ab.

## 1. Produktvision

CampusConnect ist ein zentrales, webbasiertes Studierendenportal für die DHBW Lörrach. Studierende, Lehrende und Verwaltung finden dort studienrelevante Informationen, Gruppenkommunikation und Alltagsservices wie Mensa, Stundenplan, Noten und Kontakte an einem Ort.

## 2. Zielgruppe

| Zielgruppe | Bedarf |
|---|---|
| Studierende | Schneller Überblick über relevante Informationen, Kurse, Gruppen, Mensa, Stundenplan, Noten und Kontakte |
| Lehrende | Kommunikation mit Kursen und offiziellen Gruppen ohne Medienbruch |
| Verwaltung/Admins | Pflege von Kursen, Rollen, Nutzern und offiziellen Informationen |

## 3. Problem und Nutzen

Aktuell verteilen sich Informationen über Chats, E-Mails, Aushänge und separate Portale. CampusConnect reduziert diese Fragmentierung durch eine gemeinsame Oberfläche, klare Navigation und serverseitig angebundene Datenquellen.

## 4. Abnahmekriterium für den MVP

Der MVP gilt als abnahmebereit, wenn alle `Must-have`-Anforderungen aus Abschnitt 5 entweder implementiert oder als bewusst akzeptierte MVP-Lücke mit Verantwortlichkeit dokumentiert sind. Für die Meilenstein-Vorstellung muss insbesondere erkennbar sein:

- Seitenstruktur, Navigation und zentrale UI-Komponenten der Webanwendung.
- Funktionsumfang mit klarer MoSCoW-Priorisierung.
- Aktueller Projektstand, Rollenverteilung und erledigte Aufgaben.

## 5. Funktionale Anforderungen nach MoSCoW

### 5.1 Must-have

| ID | Anforderung | Aktueller Repository-Stand | Akzeptanzkriterien |
|---|---|---|---|
| M1 | Authentifizierung und Sitzung | Implementiert: Registrierung, Login, Logout, `GET /api/auth/me`, JWT Bearer und HttpOnly-Browser-Cookie | Nutzer kann sich mit DHBW-Lörrach-E-Mail registrieren, anmelden und abmelden; geschützte Seiten sind nur authentifiziert erreichbar; Token wird im Frontend nicht dauerhaft gespeichert |
| M2 | Profil und Kurszuordnung | Implementiert: Profilseite, Kursauswahl, Telefon, Standort und Profilnotiz | Nutzer kann Anzeigename, Kurs und optionale Kontaktdaten pflegen; Kursdaten werden aus dem Kurskatalog synchronisiert |
| M3 | Rollen und Zugriffsschutz | Implementiert: `Student`, `Lecturer`, `Admin`; Admin-Guard und Backend-Rollenprüfung | Admin-Bereich ist nur für Admins erreichbar; geschützte API-Endpunkte lehnen unberechtigte Zugriffe ab |
| M4 | Kursverwaltung | Implementiert: Admin kann Kurse anlegen und auflisten | Admin kann Kurscode, Studiengang und Semester erfassen; neue aktive Kurse sind in Registrierung, Profil und Administration auswählbar |
| M5 | Nutzerverwaltung | Teilweise implementiert: Admin kann Nutzer auflisten, Rolle/Kurs ändern und Nutzer löschen | Admin kann bestehende Nutzer verwalten; direkte Nutzeranlage und Passwort-Reset durch Admin sind noch nicht implementiert und bleiben als MVP-Lücke sichtbar |
| M6 | News-Feed | Implementiert: gruppenbasierter Feed mit Beiträgen, Kommentaren und Emoji-Reaktionen | Berechtigte Nutzer sehen Beiträge ihrer Gruppen; Nutzer mit Schreibrecht können Beiträge erstellen, kommentieren, reagieren und eigene Inhalte löschen |
| M7 | Gruppen und Berechtigungen | Implementiert: Kursgruppen, offizielle Gruppen, Campusgruppen, Entdecken/Beitreten, Gruppenverwaltung und Mitgliedsrechte | Nutzer kann Gruppen entdecken und öffentlichen Campusgruppen beitreten; Besitzer/Admins können Einstellungen, Zuweisungen und Rechte verwalten |
| M8 | Mensa-Speiseplan | Implementiert: Backend-Integration der SWFR-XML-API und Frontend-Wochenansicht | Angemeldete Nutzer sehen den Mensa-Speiseplan der Mensa Lörrach; Lade- und Fehlerzustände werden verständlich angezeigt |
| M9 | Stundenplan | Implementiert: Backend lädt iCal-Daten; Frontend zeigt Kursstundenplan | Nutzer kann den Stundenplan für den Profilkurs oder einen ausgewählten Kurs abrufen; Zeitraum ist über Parameter steuerbar |
| M10 | Noten-Tracker | Implementiert: persönliche Noteneinträge, Durchschnitt, optionaler Studienplanabgleich | Nutzer kann Noten erfassen, anzeigen und löschen; Durchschnitt wird aus den gespeicherten Einträgen berechnet |
| M11 | Prüfungskalender | Implementiert: persönliche Prüfungseinträge und Kalenderseite | Nutzer kann Prüfungstermine anzeigen, erfassen und löschen |
| M12 | Kontaktbuch | Implementiert: geschützte Kontaktbuchsuche | Nutzer kann Personen nach Name, E-Mail, Kurs, Studiengang oder Profildetails suchen und Kontaktdaten sehen |
| M13 | Admin-Bereich | Implementiert: kombinierte Benutzer- und Kursadministration | Admin kann zentrale Verwaltungsfunktionen über eine geschützte UI ausführen |
| M14 | Demo- und Präsentationsdaten | Implementiert: Development-Demo-Seed über `DevelopmentDemoDataSeeder` | Lokale Demo enthält Kurse, Rollen, Gruppen, Feed-Beiträge, Noten und Prüfungseinträge für die Präsentation |
| M15 | Dokumentierte API | Implementiert: `docs/api.md` und Swagger/OpenAPI im Development-Modus | Implementierte Endpunkte sind im Repository dokumentiert und über Swagger nachvollziehbar |

### 5.2 Should-have

| ID | Anforderung | Status | Nutzen |
|---|---|---|---|
| S1 | Admin legt Nutzer direkt mit Initialpasswort an | Geplant / nicht implementiert | Unterstützt zentral verwaltete Kurskohorten ohne Self-Service-Registrierung |
| S2 | Passwort-Reset und Erstlogin-Passwortwechsel | Geplant / nicht implementiert | Erhöht Betriebssicherheit bei zentral angelegten Konten |
| S3 | Verwaltungsrolle zusätzlich zu Admin/Lecturer/Student | Geplant / nicht implementiert | Erlaubt feinere Trennung zwischen technischer Administration und Hochschulverwaltung |
| S4 | Onboarding-Flow für neue Nutzer | Konzeption in `docs/onboarding.md`, nicht implementiert | Führt Erstsemester schneller durch Profil, Stundenplan, Mensa und Gruppen |
| S5 | Moderations-/Freigabeprozess für Gruppenbeiträge | Datenmodell vorbereitet (`requiresApproval`), Workflow noch nicht umgesetzt | Ermöglicht kontrollierte Kommunikation in offiziellen Gruppen |
| S6 | Verbesserte UI-Dokumentation mit Screenshots oder Mockup-Export | Dieses Dokumentationspaket beschreibt Struktur; Bildexporte können ergänzt werden | Erleichtert Kundengespräch und Abgleich der Oberfläche |

### 5.3 Could-have

| ID | Anforderung | Status | Nutzen |
|---|---|---|---|
| C1 | Erinnerungen oder Push-Benachrichtigungen für Prüfungen | Nicht implementiert | Nutzer wird aktiv an Termine erinnert |
| C2 | Schwarzes Brett / Marktplatz | Nicht implementiert | Ergänzt Angebote, Gesuche und Hinweise außerhalb des offiziellen Feeds |
| C3 | PDF-Export für Stundenplan oder Notenübersicht | Nicht implementiert | Hilft bei Offline-Nutzung und Weitergabe |
| C4 | Erweiterte Benachrichtigungszentrale | Nicht implementiert | Bündelt neue Kommentare, Gruppenaktivitäten und offizielle Hinweise |
| C5 | Verbesserte mobile Detailansichten | Teilweise durch responsive Styles abgedeckt | Optimiert Nutzung zwischen Vorlesungen |

### 5.4 Won't-have für den MVP

| ID | Nicht-Ziel | Begründung |
|---|---|---|
| W1 | Native iOS-/Android-App | Der MVP ist eine responsive Webanwendung |
| W2 | LMS- oder Moodle-Ersatz | CampusConnect verwaltet keine Lernmaterialien und ersetzt keine Lehrplattform |
| W3 | Privater Echtzeit-Chat | Gruppenkommunikation läuft über Feed, Kommentare und Reaktionen |
| W4 | Offizielle Dualis-Notenintegration | Noten bleiben persönliche Einträge; keine Anbindung an offizielle Notensysteme |
| W5 | Multi-Hochschul-/Multi-Mandantenbetrieb | Scope bleibt DHBW Lörrach |
| W6 | Gamification | Kein Kernnutzen für den MVP |

## 6. Seitenstruktur und Navigation

Die aktuelle Webanwendung ist über eine geschützte Shell erreichbar. Nach Login führt die Navigation zu:

- Feed
- Mensa
- Stundenplan
- Prüfungen
- Noten
- Gruppen
- Kontakte
- Profilmenü
- Admin-Bereich für Admins

Details zur Benutzeroberfläche stehen in `docs/ui-prototype.md`.

## 7. Aktueller Projektstand

Der aktuelle Stand, Rollenverteilung, erledigte Aufgaben und bekannte Lücken sind in `docs/project-status.md` dokumentiert.

## 8. Meilensteine

| Meilenstein | Datum | Ergebnis |
|---|---|---|
| Kick-off | 2026-05-11 | Projektidee, Zielgruppe und Grundarchitektur festgelegt |
| Konzeptabschluss | 2026-05-17 | Architektur-, API-, Markt- und Demo-Dokumentation vorhanden |
| Scope-Freeze | 2026-05-20 | Must-haves und MVP-Lücken in diesem PRD festgehalten |
| Code-Freeze | 2026-06-23 | Geplanter Abschluss der MVP-Implementierung |
| Generalprobe | 2026-06-26 | Präsentationsdurchlauf mit Demo-Daten |
| Pitch-Termin | 2026-06-29 | Abgabe/Präsentation |

## 9. Offene fachliche Entscheidungen

| Thema | Entscheidung nötig |
|---|---|
| Registrierung vs. zentrale Nutzeranlage | Aktuelle Implementierung erlaubt Self-Service-Registrierung; ein zentraler Admin-Anlageprozess ist als Should-have dokumentiert |
| Zusätzliche Verwaltungsrolle | Aktuell gibt es `Student`, `Lecturer`, `Admin`; eine eigene Verwaltungsrolle muss fachlich definiert werden |
| Prüfungskalender im MVP | Implementiert, aber in älteren Scope-Dokumenten teilweise als Nicht-Ziel beschrieben; für die aktuelle Webanwendung wird er als Must-have dokumentiert |
