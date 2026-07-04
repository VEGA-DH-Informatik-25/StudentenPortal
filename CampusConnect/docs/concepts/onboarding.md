# Onboarding-Konzept fuer CampusConnect

> **Status:** Abgabefaehige Beschreibung des aktuellen MVP-Flows. Dieses Dokument beschreibt den stabilen Ist-Stand und grenzt optionale Ausbaustufen bewusst vom Must-have ab.

## Ziel

Das Onboarding fuehrt neue Nutzer nach der Admin-Anlage sicher in CampusConnect ein. Verpflichtend ist der Initialpasswortwechsel. Danach hilft eine kurze Guided Tour, die wichtigsten Bereiche im laufenden Produkt zu finden.

## Aktueller MVP-Flow

1. Admin legt ein Nutzerkonto mit `@dhbw-loerrach.de`-E-Mail, Rolle, Kurs und Initialpasswort an.
2. Beim ersten Login oder nach einem Admin-Passwort-Reset ist `MustChangePassword = true`.
3. Das Frontend fuehrt den Nutzer auf die Onboarding-Route.
4. Der Nutzer sieht eine Willkommensansicht und muss das Initialpasswort aendern.
5. Nach erfolgreichem Passwortwechsel startet die Guided Tour im Feed.
6. Die Tour erklaert die zentralen Navigationspunkte und kann abgeschlossen oder uebersprungen werden.
7. Beim ersten Oeffnen der Gruppenansicht kann eine kurze Gruppen-Erklaerung starten.

Der Admin-Passwort-Reset setzt ein neues Initialpasswort und oeffnet den Erstlogin-Zustand erneut:

```text
MustChangePassword = true
OnboardingCompleted = false
OnboardingCompletedAt = null
```

Rolle, Kurs, Aktivstatus und sonstige Profildaten bleiben dabei unveraendert.

## Verpflichtender Teil

- Nutzer muessen ihr Initialpasswort beim ersten Login oder nach Admin-Reset aendern.
- Das neue Passwort muss mindestens acht Zeichen enthalten und jeweils Grossbuchstaben, Kleinbuchstaben, Zahl und Sonderzeichen abdecken.
- Ein falsches aktuelles Passwort oder ungueltiges neues Passwort wird verstaendlich gemeldet.
- Der Onboarding-Abschluss ist erst nach Passwortwechsel moeglich.

## Guided Tour

Die Tour ist ein leichter Produkthinweis, kein eigener Daten- oder Badge-Prozess. Sie laeuft im normalen App-Shell-Kontext und verweist auf Feed, Mensa, Stundenplan, Noten, Gruppen, Kontakte, Profil und Schnellzugriffe.

Die Gruppen-Erklaerung ist bewusst an den ersten Gruppen-Klick gekoppelt. So bleibt der erste Login kurz und Nutzer sehen die Erklaerung dort, wo sie fachlich relevant ist.

## Nicht Harte MVP-Pflicht

Folgende Punkte bleiben optionale Ausbaustufen und sind fuer die aktuelle Abgabe nicht als Must-have formuliert:

- persistenter Fortschritt fuer einzelne Onboarding-Schritte,
- Dashboard-Welcome-News,
- persistente Gruppenvorschlaege,
- Badge-Logik ausserhalb der vorhandenen einfachen Profilanzeige,
- komplexe Empfehlungslogik,
- Analytics fuer Onboarding-Abbrueche.

## Akzeptanzkriterien Fuer Die Abgabe

- Ein neu angelegter Nutzer wird nach Login zum Passwortwechsel gefuehrt.
- Ein per Admin zurueckgesetzter Nutzer wird beim naechsten Login wieder zum Passwortwechsel gefuehrt.
- Nach Passwortwechsel kann der Nutzer die App normal verwenden.
- Die Guided Tour startet im Feed und kann abgeschlossen werden.
- Die Gruppen-Erklaerung startet erst nach dem Oeffnen der Gruppenansicht.
- Fehler externer Datenquellen blockieren den Passwortwechsel nicht.
