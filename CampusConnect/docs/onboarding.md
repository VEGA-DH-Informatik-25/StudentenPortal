# Onboarding-Konzept für CampusConnect

> **Status:** Produktkonzept / Zielbild. Der aktuelle Code besitzt noch keinen vollständigen Guided-Tour-Flow und kein persistiertes Feld `onboarding_completed`. Dieses Dokument beschreibt das gewünschte Verhalten für eine spätere Umsetzung, nicht den aktuellen API- oder Datenbankstand.

## Ziel des Onboardings

Das Onboarding soll neue Studierende schnell und verständlich in CampusConnect einführen. Neue Nutzer sollen innerhalb von etwa fünf Minuten die wichtigsten Funktionen verstehen und die App sinnvoll nutzen können.

Es soll nicht überladen wirken: Nur notwendige Schritte sind verpflichtend, alles Weitere kann übersprungen werden.

## Überblick

Das Onboarding besteht aus drei Phasen:

1. Pflicht-Setup: Begrüßung, Initialpasswort ändern und Campus-Daten laden.
2. Guided Tour: kurze, interaktive Erklärungen der Hauptbereiche.
3. Community-Einstieg: optionale Gruppen-Vorschläge und Beitritt.

## Phase 1: Pflicht-Setup

### Schritt 1: Begrüßung

Nach dem ersten Login erscheint ein Willkommensscreen mit einem kurzen Hinweis auf die zentralen Funktionen von CampusConnect und dem Button `Weiter`.

> Willkommen bei CampusConnect. Hier findest du wichtige Infos, deinen Stundenplan, Mensa-Angebote, Noten und Gruppen an einem Ort.

### Schritt 2: Initialpasswort ändern

Nach der Begrüßung muss der Nutzer sein Initialpasswort ändern. Dieser sicherheitsrelevante Schritt ist verpflichtend und darf nicht übersprungen werden.

Der Screen enthält die Felder `Aktuelles Passwort`, `Neues Passwort` und `Neues Passwort wiederholen`. Das neue Passwort muss mindestens acht Zeichen sowie jeweils einen Großbuchstaben, Kleinbuchstaben, eine Zahl und ein Sonderzeichen enthalten.

Die Anforderungen werden sichtbar dargestellt und erfüllte Regeln optisch markiert. Der Button `Passwort ändern` wird erst aktiv, wenn alle Regeln erfüllt sind. Verständliche Fehlermeldungen behandeln nicht übereinstimmende Passwörter, fehlende Zeichenklassen und ein falsches aktuelles Passwort.

### Schritt 3: Campus-Daten laden

Nach der erfolgreichen Passwortänderung lädt CampusConnect im Hintergrund erste relevante Daten:

- Stundenplan
- aktuelle Mensa-Menüs
- offizielle News
- relevante Gruppen-Vorschläge

Währenddessen erscheint eine kurze Ladeanimation, zum Beispiel mit dem Text `Wir richten CampusConnect für dich ein …`. Nach erfolgreichem Abschluss bestätigt eine Erfolgsmeldung die Einrichtung.

Fehler einzelner Datenquellen blockieren das Onboarding nicht. Beispielsweise wird ein nicht verfügbarer Stundenplan freundlich erklärt und später erneut geladen.

## Phase 2: Guided Tour

Die Guided Tour ist optional und kann übersprungen werden. Jeder Schritt verwendet einen Tooltip mit höchstens einem Satz, einem Pfeil auf das passende Element sowie den Aktionen `Weiter` und `Tour überspringen`. Der passende Bereich wird jeweils automatisch fokussiert.

| Bereich | Tooltip |
| --- | --- |
| News | Hier findest du offizielle Informationen und wichtige Ankündigungen deiner Hochschule. |
| Mensa | Hier siehst du aktuelle Speisepläne und Mensa-Angebote. |
| Stundenplan | Hier findest du deine Vorlesungen und Termine übersichtlich dargestellt. |
| Noten | Hier findest du deine Prüfungsergebnisse und deinen aktuellen Durchschnitt. |
| Gruppen | Hier kannst du Gruppen für Kurse, Campus-Themen und offizielle Informationen finden. |

Nach Abschluss oder Überspringen führt die Tour sinnvoll zum Dashboard weiter.

## Phase 3: Community-Einstieg

Der Community-Einstieg ist optional. Es werden höchstens drei passende Gruppen vorgeschlagen, basierend auf Studiengang, Kurs, Hochschule sowie offiziellen und Campus-Gruppen.

Jede Gruppenkarte zeigt Name, Typ, Kurzbeschreibung, Mitgliederzahl und die Aktion `Beitreten`. Nutzer können einzelne Gruppen wählen oder den Schritt mit `Auswahl überspringen` beziehungsweise `Weiter zum Dashboard` überspringen.

Für einen Nutzer aus `TIF25A` könnten beispielsweise die Kursgruppe, eine Informatik-Gruppe der DHBW Lörrach und eine allgemeine CampusConnect-Gruppe vorgeschlagen werden. Eine Gruppen-Chat-Funktion gehört nicht zum Produktumfang.

## Nach dem Onboarding

Nach Abschluss oder Überspringen der optionalen Schritte landet der Nutzer auf dem Dashboard mit aktuellen News, nächster Vorlesung, Mensa-Auszug und Hinweisen zu offenen Aktionen.

Neue Nutzer erhalten für 14 Tage ein Badge `Neu hier` im Profil mit dem Zusatz `Seit X Tagen dabei`. Das Badge kann aus dem Erstellungsdatum des Nutzerkontos berechnet werden und muss nicht dauerhaft gespeichert sein.

Zusätzlich soll im Feed eine prominente Welcome-News erscheinen, die CampusConnect begrüßt und auf Stundenplan, Gruppen, Orientierungstermine und Tipps der Fachschaft hinweist.

## Technische Leitplanken für eine spätere Umsetzung

Die Passwortänderung und das eigentliche Onboarding bleiben getrennt:

```text
must_change_password: boolean
onboarding_completed: boolean
onboarding_completed_at: datetime
```

`must_change_password` ist sicherheitsrelevant und verpflichtend. Guided Tour und Gruppen-Vorschläge bleiben optional. Ein zukünftiges persistiertes Onboarding-Design benötigt ein abgestimmtes Datenmodell, API-Verträge, eine EF-Migration, Tests sowie Aktualisierungen an `api.md` und `architecture.md`.

Nicht erreichbare externe Datenquellen dürfen das Onboarding nie blockieren. Mensa, Stundenplan, News und Gruppen-Vorschläge werden bei Fehlern mit einer freundlichen Meldung übersprungen und später erneut versucht.

## Akzeptanzkriterien für ein MVP

- Neue Nutzer sehen nach dem ersten Login einen Willkommensscreen.
- Das Initialpasswort muss nach den beschriebenen Regeln geändert werden und ist nicht überspringbar.
- Nach dem Passwortwechsel werden Campus-Daten geladen; einzelne Ladefehler blockieren nicht.
- Die Tour erklärt News, Mensa, Stundenplan, Noten und Gruppen; sie ist überspringbar.
- Es werden höchstens drei Gruppen vorgeschlagen; Beitritt und Überspringen sind möglich.
- Nach Abschluss erscheint das Dashboard, die Willkommensmeldung und für 14 Tage das Badge `Neu hier`.

Nicht Bestandteil des MVP sind persistenter Fortschritt pro Einzelschritt, komplexe Empfehlungslogik, personalisierte Welcome-News, detaillierte Analytics und Chat.
