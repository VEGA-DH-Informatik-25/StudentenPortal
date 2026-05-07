# Onboarding für CampusConnect

## Ziel
Neue Studierende in unter 5 Minuten produktiv machen.

---

## Phase 1: Setup (2-3 Min)

### Schritt 1: Begrüßung + Hochschule/Studiengang
- **Screen**: "Willkommen bei CampusConnect"
- Studiengang eingeben
- Button: "Weiter"


### Schritt 2: Auto-Load – First Win
- Stundenplan laden
- Aktuelle Mensa-Menüs zeigen
- **Visuell**: Kurze Loading-Animation mit Erfolgsmeldung
- User sieht sofort: "Die App funktioniert und hat meine Daten"

---

## Phase 2: Guided Tour (1-2 Min)

### 4 Interactive Clicks
1. **News** – "Hier siehst du offizielle Infos & Ankündigungen"
2. **Mensa** – "Aktuelle Speisepläne für deine Uni"
3. **Stundenplan** – "Deine Vorlesungen übersichtlich"
4. **Noten** – "Deine Prüfungsergebnisse & Durchschnitt"

### Design der Tour
- **Tooltip über Element** mit Pfeil
- **Kurzer Text** (max. 1 Satz)
- **"Skip Tour"** Button oben rechts (unauffällig)
- Nach jedem Klick: Automat. zum nächsten Element

---

## Phase 3: Community-Einstieg (Optional)

### Gruppen-Suggestions
- 3 vorgeschlagene Gruppen basierend auf Studiengang
- "Beitreten" Button für jede
- Skip möglich
- Nach Beiritt: Willkommens-Nachricht in Gruppen-Chat

---

## Nach dem Onboarding

### Badge "Neu hier" (2 Wochen)
- Visuell markiert im Profil
- Zeigt: "Seit X Tagen dabei"
- Danach automatisch entfernt

### Welcome-News
- Offizielle Begrüßung von Hochschule
- Fachschafts-Tipps
- Links zu Orientierungsterminen
- Im News-Feed prominent

---

## Technische Anmerkungen

- **Onboarding-Status speichern** im Backend (User-Flag `onboarding_completed`)
- **Auto-Retry**: Falls Hochschul-Daten nicht laden, nicht blockieren – später neu versuchen
