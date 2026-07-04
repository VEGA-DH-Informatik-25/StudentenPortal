# CampusConnect – Präsentation für den Projektsponsor

## Präsentationsformat
- **Gesamtdauer:** 15 Minuten
- **Teamgröße:** 4 Personen
- **Teammitglieder:** Julius Allgaier, Simon Wörner, Jakob Wußler, Theo Pfaff

---

## 1: Titel und Einstieg
**Redner:** Julius Allgaier

### Inhalt
```
CampusConnect
Das zentrale Studierendenportal der DHBW Lörrach

Ein Projekt der DHBW Informatik – Mai bis Juli 2026
```
---

## 2: Das Problem – Ausgangssituation 
**Redner:** Julius Allgaier

### Inhalt – Stichpunkte
```
Herausforderung im Studienalltag der DHBW Lörrach:

❌ Informationen verteilen sich chaotisch
   • WhatsApp-Gruppen (inoffiziell, unstrukturiert)
   • E-Mail-Verteiler (unübersichtlich, leicht übersehen)
   • Physische Aushänge (nicht jeder sieht sie)

❌ Keine Anlaufstelle für zentrale Informationen
   • Prüfungstermine: handschriftliche Notizen
   • Mensa-Plan: Umständliches Nachschlagen
   • Stundenplan: Verschiedene Quellen

❌ Lerngruppen entstehen zufällig
   • Keine gezielte Vernetzung nach Fachbereichen
   • Erstsemester: kein strukturierter Einstieg
   • Noten und ECTS: manuell in eigenen Tabellen

❌ Folgen
   • Informationsverlust und Missverständnisse
   • Erstsemester werden nicht optimal betreut
   • Zeitaufwand für Studis höher als nötig
```
---

## 3: Ziel und Anforderungen
**Redner:** Julius Allgaier

### Inhalt – Stichpunkte
```
Was CampusConnect löst:

✓ Eine zentrale Plattform für alle studiumsrelevanten Infos
✓ Einheitlicher News-Feed für Ankündigungen und Nachrichten
✓ Automatischer Mensa-Speiseplan – täglich aktuell
✓ Persönlicher Prüfungskalender
✓ Noten- und ECTS-Tracker
✓ Gruppen für Kurse, Campus und offizielle Informationen
✓ Strukturierter Onboarding-Prozess für Erstsemester
✓ Kontaktbuch und Campusgruppen für Vernetzung

→ Ziel: Ein Portal statt zehn Chaos-Kanäle
```
---

## 4: Die Lösung – Kern-Features
**Redner:** Simon Wörner

### Inhalt – Stichpunkte
```
Zentrale Funktionen von CampusConnect:

📰 NEWS-FEED
   • Zentrale Ankündigungen von Lehrbeauftragten & Admins
   • Beiträge mit Kommentaren und Emoji-Reaktionen
   • Kontext: Beiträge werden in Gruppen organisiert

👥 GRUPPEN
   • Course-, Official- und Campus-Gruppen
   • Join-, Request-, Invite- und Leave-Workflows
   • Moderation und Einstellungen pro Gruppe

🍽️ MENSA-SPEISEPLAN
   • Tages- und Wochenansicht
   • Backendgekapselte SWFR-Integration
   • Freundliche Fehlerzustände bei externer Störung

📅 PRÜFUNGSKALENDER + NOTEN-TRACKER
   • Persönliche Prüfungstermine, manuell gepflegt
   • Noteneinträge mit ECTS-Berechnung
   • Notendurchschnitt auf einen Blick

👥 GRUPPEN & KONTAKTE
   • Kurs-, Campus- und offizielle Gruppen
   • Beitrittsanfragen & Gruppenverwaltung
   • Kontaktbuch für Campus-Kontakte

🔐 AUTHENTIFIZIERUNG
   • Konten werden durch Admins erstellt
   • Sichere, moderne JWT-basierte Auth
```
---

## 5: Praktischer Mehrwert für den Sponsor
**Redner:** Theo Pfaff

### Inhalt – Stichpunkte
```
Konkrete Vorteile für die DHBW Lörrach:

🎯 Bessere Studienergebnisse
   → Lerngruppen entstehen strukturiert
   → Erstsemester haben einen guten Start
   → Weniger verlorene Informationen

⏱️ Weniger Kommunikationschaos
   → Offizielle Infos erreichen alle gleichzeitig
   → Keine Mehrfach-Ankündigungen über verschiedene Kanäle
   → Reduzierte Last auf WhatsApp und E-Mail

📊 Bessere Kontrolle für die Verwaltung
   → Zentrale Stelle für offizielle Meldungen
   → Admin-Tools zur Nutzerverwaltung
   → Transparenz über aktive Gruppen und Kurse

🤝 Stärkere Hochschulgemeinde
   → Bessere Vernetzung der Studierenden
   → Authentische Communities entstehen auf der Plattform
   → Hochschulkultur wird aktiver und sichtbar

💡 Modernes Image
   → DHBW Lörrach zeigt: Wir kümmern uns um digitale Infrastruktur
   → Innovative Lösung, die Studierenden begeistert
```
---


## 6: Technisches Überblick – Warum das funktioniert 
**Redner:** Theo Pfaff

### Inhalt – Stichpunkte (Optional – Tiefe für Sponsor, wenn gewünscht)
```
Kurz gefasst – Die technische Grundlage:

🖥️ Technologie-Stack
   Frontend:  Angular 21 (responsive, lädt schnell)
   Backend:   ASP.NET Core 10 (sicher, performant)
   Datenbank: SQLite (wartbar, portierbar)
   Auth:      JWT + HttpOnly-Cookies (sicher gegen XSS)

🔐 Sicherheit ist eingebaut
   • E-Mails: Nur @dhbw-loerrach.de
   • Tokens: Im Memory, nicht im Browser-Storage
   • API: Rollenbasierter Zugriff (Admin, Lehrbeauftragte, Studierende)
   • CORS & SWFR-Proxy: Externe APIs sicher integriert

🚀 Wartbarkeit & Skalierbarkeit
   • Saubere, aufgeteilte Architektur
   • Jedes Feature ist isoliert testbar
   • Dokumentation ist fortlaufend aktuell

⚡ Performance
   • Lazy Loading im Frontend
   • Caching wo sinnvoll (Speiseplan, Kursinformationen)
```
---

## 7: Risiken & wie wir sie mindern

**Redner:** Theo Pfaff

### Inhalt – Stichpunkte
```
Mögliche Herausforderungen – und unsere Strategie:

⚠️ Abhängigkeiten (SWFR-API, Stundenplan-URLs)
   → Fallback: Lokale Testdaten, manueller Upload
   → Früh anfragen, nicht erst spät

⚠️ Scope-Creep
   → Strikter MVP-Fokus
   → Optionale Features kommen in "Future Work"

⚠️ Zeitmangel
   → Agile Sprints mit regelmäßigen Reviews
   → Priorisierung statt Perfektionismus

⚠️ Sicherheit & Datenschutz
   → Von Anfang an beachtet
   → Regelmäßige Security-Reviews
```
---
## 8: Warum wir geeignet sind – Team & Qualifikation
**Redner:** Theo Pfaff

### Inhalt – Stichpunkte
```
Warum CampusConnect in den richtigen Händen ist:

👥 Unser Team – 4 erfahrene Informatiker
   • 2. Semester – bereits umfangreiche Projekte umgesetzt
   • Verschiedene Spezialisierungen (Frontend, Backend, QA/DevOps)
   • Starke Teamkommunikation und gemeinsame Grundsätze

🔧 Technische Qualifikation
   • Frontend: Angular 21 (modern, skalierbar, responsive)
   • Backend: ASP.NET Core (enterprise-grade, sicher)
   • Datenbank: SQLite (einfach, zuverlässig, wartbar)
   • Authentifizierung: JWT + HttpOnly-Cookies (sichere Best Practices)

📋 Professionelle Workflows
   • Code Reviews, Tests, CI/CD
   • Klar definierte Rollen und Verantwortungen

⏰ Realistische Umsetzung
   • MVP-Fokus: Wir bauen, was notwendig ist, nicht mehr
```
---

## 9: Was wir vom Sponsor brauchen
**Redner:** Jakob Wußler

### Inhalt – Stichpunkte
```
Das bitten wir vom Sponsor:

✅ 1. Feedback zur Vision
   • Sind die Prioritäten richtig gesetzt?
   • Gibt es kritische Features, die wir vergessen haben?

✅ 2. Zugang zu Informationen
   • Stundenplan-Datenquellen (iCal URLs pro Kurs)
   • Kursstruktur und Kurscodes
   • Kontakt zur SWFR für Mensa-API-Schlüssel

✅ 3. Test-Nutzende
   • Kleine Gruppe von Studierenden für Beta-Testing
   • Dozenten und Admins, die früh Feedback geben

✅ 4. Freigaben und Entscheidungen
   • Go/No-Go zur Deployment
   • Datenschutz-Freigabe (Datenschutzerklärung, Impressum)
   • Entscheidung: Hosting (DHBW-Server oder Cloud)

✅ 5. Nach dem MVP
   • Perspektive für produktiven Betrieb klären
   • Wer macht Updates & Support?

✅ 6. Moderation von Gruppenbeiträgen
```
---

## 10: Fazit – Warum CampusConnect jetzt
**Redner:** Jakob Wußler

### Inhalt – Stichpunkte
```
Zusammenfassung:

🎯 DAS PROBLEM
   ✗ Chaotische Informationsverbreitung
   ✗ Unstrukturierte Lerngruppen
   ✗ Manuelle Prozesse (Noten, Termine, Speiseplan)

💡 UNSERE LÖSUNG
   ✓ Zentrale Plattform – ein Ort für alles
   ✓ Modern, sicher, professionell

👥 WARUM WIR
   ✓ Erfahrenes Team mit klaren Rollen
   ✓ Best Practices & solide Architektur
   ✓ Realistischer Projektplan

📊 NÄCHSTE SCHRITTE
   ✓ Feedback vom Sponsor
   ✓ Zugang zu notwendigen Informationen
```
---

## 11: Kontakt und offene Fragen
**Redner:** Jakob Wußler

### Inhalt
```
Kontakt – Fragen sind willkommen!

👥 Team:
   Projektleitung / Full-Stack:  Person 1
   Backend-Entwicklung:          Person 3
   Frontend-Entwicklung:         Person 2
   QA / DevOps:                  Person 4

📚 Dokumentation & Links
   GitHub Repository:  github.com/VEGA-DH-Informatik-25/StudentenPortal
   Dokumentation:       /CampusConnect/docs/README.md
   Projektbeschreibung: /CampusConnect/docs/product/projektbeschreibung.md
   Architektur-Doku:    /CampusConnect/docs/product/architecture.md
   API-Doku:            /CampusConnect/docs/product/api.md

❓ Fragen?
