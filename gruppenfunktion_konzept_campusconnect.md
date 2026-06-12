# Konzept: Gruppenfunktion in CampusConnect

## 1. Ziel der Gruppenfunktion

Die Gruppenfunktion soll verschiedene Kommunikationsräume innerhalb von CampusConnect ermöglichen. Dabei sollen offizielle Informationen, kursbezogene Kommunikation und freie Campus-Gruppen sauber voneinander getrennt werden.

Die Gruppen sollen so aufgebaut sein, dass sie einfach nutzbar, aber trotzdem gut administrierbar und moderierbar sind.

---

## 2. Grundidee

Es gibt drei Arten von Gruppen:

```text
Gruppen
├── Offizielle Gruppen
│   └── Für offizielle Informationen der Hochschule
│
├── Kursgruppen
│   └── Für einzelne Studiengänge/Kurse, z. B. TIF25A
│
└── Campus-Gruppen
    └── Freie Gruppen für Studierende, Lehrende und Management
```

---

## 3. Gruppentypen

| Gruppentyp | Zweck | Beispiel | Wer darf erstellen? |
|---|---|---|---|
| **Offizielle Gruppe** | Offizielle Kommunikation der Hochschule | Prüfungsamt, Management, IT, Studiengangsleitung | Admin und Management |
| **Kursgruppe** | Kommunikation innerhalb eines konkreten Kurses | TIF25A, WWI24B | Admin und Management |
| **Campus-Gruppe** | Freie Gruppen für Austausch, Lernen, Freizeit oder Organisation | Lerngruppe Mathe, Tennis, Wohnungsbörse | Alle Rollen |

---

## 4. Globale Rollen im System

Diese Rollen existieren unabhängig von einzelnen Gruppen:

| Globale Rolle | Bedeutung |
|---|---|
| **Student** | Normale Studierende |
| **Lehrer** | Dozenten und Lehrpersonen |
| **Management** | Sekretariat, Hochschulverwaltung, organisatorische Rollen |
| **Admin** | Systemweite Administration |

Wichtig: Die globale Rolle ist nicht dasselbe wie die Rolle innerhalb einer Gruppe.

Beispiel:

```text
Max ist global ein Student.
In seiner selbst erstellten Campus-Gruppe ist Max aber Gruppenbesitzer.
```

---

## 5. Erstellrechte nach Gruppentyp

```text
Darf Gruppe erstellen?

                        Offiziell    Kursgruppe    Campus-Gruppe
Student                     Nein         Nein            Ja
Lehrer                      Nein         Nein            Ja
Management                   Ja           Ja             Ja
Admin                        Ja           Ja             Ja
```

| Rolle | Offizielle Gruppe erstellen | Kursgruppe erstellen | Campus-Gruppe erstellen |
|---|---:|---:|---:|
| Student | Nein | Nein | Ja |
| Lehrer | Nein | Nein | Ja |
| Management | Ja | Ja | Ja |
| Admin | Ja | Ja | Ja |

---

## 6. Erstellprozess einer Gruppe

Beim Erstellen einer Gruppe werden je nach Gruppentyp unterschiedliche Felder benötigt.

### 6.1 Allgemeine Felder für alle Gruppen

| Feld | Beschreibung |
|---|---|
| **Gruppenname** | Name der Gruppe |
| **Beschreibung** | Kurze Erklärung, wofür die Gruppe gedacht ist |
| **Gruppentyp** | Offiziell, Kurs oder Campus |
| **Sichtbarkeit** | Gibt an, ob die Gruppe in der Suche sichtbar ist |
| **Beitrittsregel** | Offen, Anfrage erforderlich oder nur Einladung |
| **Post-Regel** | Gibt an, ob normale Mitglieder Beiträge erstellen dürfen |
| **Kommentar-Regel** | Gibt an, ob Kommentare grundsätzlich erlaubt sind |
| **Moderationsregel** | Gibt an, ob Beiträge vor Veröffentlichung freigegeben werden müssen |

### 6.2 Zusätzliche Felder für Kursgruppen

| Feld | Beschreibung | Beispiel |
|---|---|---|
| **Zugeordneter Kurs** | Kurs, dem die Gruppe gehört | TIF25A |

### 6.3 Zusätzliche Felder für offizielle Gruppen

| Feld | Beschreibung | Beispiel |
|---|---|---|
| **Offizielle Kategorie** | Fachliche Einordnung der Gruppe | Prüfungsamt, Management, IT |

---

## 7. Gruppeneinstellungen

Für alle Gruppen gibt es folgende zentrale Einstellungen:

| Einstellung | Bedeutung | Empfehlung |
|---|---|---|
| **Mitglieder dürfen posten** | Normale Gruppenmitglieder dürfen eigene Beiträge erstellen | Bei Campus- und Kursgruppen meistens aktiv |
| **Kommentare grundsätzlich erlauben** | Beiträge in dieser Gruppe können kommentiert werden | Bei Kurs- und Campus-Gruppen meistens aktiv |
| **Beiträge benötigen Freigabe** | Beiträge erscheinen erst nach Freigabe durch Moderator/Besitzer | Bei offiziellen Gruppen sinnvoll |
| **Gruppe ist in der Suche sichtbar** | Eingeloggte Nutzer können die Gruppe finden | Je nach Gruppe aktiv oder inaktiv |
| **Beitrittsregel** | Legt fest, wie Nutzer Mitglied werden | Offen, Anfrage oder Einladung |

---

## 8. Empfohlene Standardeinstellungen je Gruppentyp

| Einstellung | Offizielle Gruppe | Kursgruppe | Campus-Gruppe |
|---|---:|---:|---:|
| In Suche sichtbar | Ja | Optional | Ja |
| Beitritt offen | Nein | Optional | Ja/Optional |
| Beitritt per Anfrage | Optional | Ja | Optional |
| Nur Einladung | Optional | Optional | Optional |
| Mitglieder dürfen posten | Nein | Ja | Ja |
| Kommentare erlauben | Nein/Optional | Ja | Ja |
| Beiträge benötigen Freigabe | Ja/Optional | Optional | Optional |

---

## 9. Kommentare: Gruppenebene und Beitragsebene

Kommentare sollten zweistufig geregelt werden.

### 9.1 Gruppenebene

Die Gruppe legt fest, ob Kommentare grundsätzlich erlaubt sind.

```text
Kommentare in Gruppe erlaubt?
├── Nein
│   └── Kein Beitrag in dieser Gruppe kann kommentiert werden
│
└── Ja
    └── Pro Beitrag kann entschieden werden, ob Kommentare aktiv sind
```

### 9.2 Beitragsebene

Wenn Kommentare in der Gruppe grundsätzlich erlaubt sind, kann beim Erstellen eines Beitrags entschieden werden:

```text
Kommentare für diesen Beitrag erlauben: Ja / Nein
```

### 9.3 Regel

Ein einzelner Beitrag darf Kommentare nur erlauben, wenn die Gruppe Kommentare grundsätzlich erlaubt.

| Gruppeneinstellung | Beitragseinstellung möglich? |
|---|---|
| Kommentare grundsätzlich deaktiviert | Nein, Kommentare bleiben aus |
| Kommentare grundsätzlich aktiviert | Ja, pro Beitrag an- oder ausschaltbar |

---

## 10. Rollen innerhalb einer Gruppe

Neben den globalen Systemrollen gibt es Rollen innerhalb einzelner Gruppen.

Empfohlene Gruppenrollen:

| Gruppenrolle | Bedeutung |
|---|---|
| **Besitzer** | Verantwortliche Person der Gruppe mit voller Kontrolle |
| **Moderator** | Unterstützt bei Verwaltung, Moderation und Freigaben |
| **Mitglied** | Normales Gruppenmitglied |

Optional für später:

| Gruppenrolle | Bedeutung |
|---|---|
| **Leser/Gast** | Kann Inhalte sehen, aber nicht aktiv teilnehmen |

---

## 11. Berechtigungsmatrix innerhalb einer Gruppe

| Berechtigung | Besitzer | Moderator | Mitglied |
|---|---:|---:|---:|
| Beiträge lesen | Ja | Ja | Ja |
| Beiträge erstellen | Ja | Ja | Abhängig von Gruppeneinstellung |
| Eigene Beiträge bearbeiten | Ja | Ja | Ja |
| Eigene Beiträge löschen | Ja | Ja | Ja |
| Fremde Beiträge löschen | Ja | Ja | Nein |
| Kommentare moderieren | Ja | Ja | Nein |
| Beiträge freigeben | Ja | Ja | Nein |
| Mitglieder einladen | Ja | Ja | Nein |
| Beitrittsanfragen verwalten | Ja | Ja | Nein |
| Mitglieder entfernen | Ja | Ja | Nein |
| Moderator ernennen | Ja | Nein | Nein |
| Gruppeneinstellungen ändern | Ja | Optional eingeschränkt | Nein |
| Gruppe löschen | Ja | Nein | Nein |

---

## 12. Verhältnis globale Rolle zu Gruppenrolle

Die globale Rolle bestimmt, was ein Nutzer systemweit darf.

Die Gruppenrolle bestimmt, was ein Nutzer innerhalb einer bestimmten Gruppe darf.

```text
Nutzer
├── Globale Rolle
│   ├── Student
│   ├── Lehrer
│   ├── Management
│   └── Admin
│
└── Gruppenrolle pro Gruppe
    ├── Besitzer
    ├── Moderator
    └── Mitglied
```

Beispiele:

| Nutzer | Globale Rolle | Gruppe | Gruppenrolle |
|---|---|---|---|
| Max | Student | Lerngruppe Mathe | Besitzer |
| Frau Mueller | Management | Exam office info | Moderator |
| Herr Schmidt | Lehrer | TIF25A | Moderator |
| Admin | Admin | Alle Gruppen | Systemweiter Zugriff |

---

## 13. Admin-Sonderrechte

Admins sollten systemweit Zugriff auf Gruppenverwaltung haben, auch wenn sie nicht explizit Mitglied jeder Gruppe sind.

Admin darf:

- alle Gruppen sehen
- alle Gruppen bearbeiten
- Gruppen löschen
- Mitglieder verwalten
- Rollen korrigieren
- problematische Inhalte entfernen
- offizielle Gruppen und Kursgruppen erstellen

Trotzdem sollte im UI klar getrennt werden zwischen:

```text
Systemweiter Admin-Zugriff
!=
Eigentliche Gruppenrolle
```

Ein Admin muss also nicht automatisch als sichtbarer Gruppenbesitzer erscheinen.

---

## 14. Empfohlenes MVP

Für die erste Version sollten folgende Funktionen umgesetzt werden:

### Gruppentypen

- Offizielle Gruppe
- Kursgruppe
- Campus-Gruppe

### Globale Erstellrechte

- Offizielle Gruppen: Admin und Management
- Kursgruppen: Admin und Management
- Campus-Gruppen: alle Rollen

### Gruppeneinstellungen

- Gruppe in Suche sichtbar
- Beitrittsregel: offen, Anfrage erforderlich oder nur Einladung
- Mitglieder dürfen posten
- Kommentare grundsätzlich erlauben
- Beiträge benötigen Freigabe

### Gruppenrollen

- Besitzer
- Moderator
- Mitglied

### Beiträge

- Beiträge erstellen
- Beiträge anzeigen
- Beiträge löschen
- Beiträge freigeben, falls Freigabe aktiviert ist
- Kommentare pro Beitrag aktivieren/deaktivieren, wenn Kommentare in der Gruppe grundsätzlich erlaubt sind

---

## 15. Vereinfachtes Gesamtbild

```text
CampusConnect Gruppenfunktion

├── Gruppentypen
│   ├── Offiziell
│   │   ├── Nur Admin erstellt
│   │   ├── Offizielle Informationen
│   │   └── Häufig eingeschränkte Kommentare/Postrechte
│   │
│   ├── Kursgruppe
│   │   ├── Nur Admin erstellt
│   │   ├── An Kurs gebunden
│   │   └── Kommunikation innerhalb einer Klasse
│   │
│   └── Campus-Gruppe
│       ├── Von allen Rollen erstellbar
│       ├── Freie Themen
│       └── Community-Charakter
│
├── Gruppeneinstellungen
│   ├── Sichtbarkeit
│   ├── Beitrittsregel
│   ├── Mitglieder dürfen posten
│   ├── Kommentare erlauben
│   └── Beiträge benötigen Freigabe
│
├── Gruppenrollen
│   ├── Besitzer
│   ├── Moderator
│   └── Mitglied
│
└── Inhalte
    ├── Beiträge
    ├── Kommentare
    ├── Reaktionen optional
    └── Freigabeprozess optional
```

---

## 16. Offene Entscheidungen

Diese Punkte sollten vor der Umsetzung noch entschieden werden:

1. Sollen Kursgruppen automatisch mit den Studierenden eines Kurses befüllt werden?
2. Dürfen Lehrende automatisch Moderator in Kursgruppen sein?
3. Darf eine Campus-Gruppe später in eine offizielle Gruppe umgewandelt werden?
4. Sollen offizielle Gruppen immer sichtbar sein?
5. Sollen Beiträge nach Veröffentlichung noch bearbeitet werden dürfen?
6. Soll es eine Meldemöglichkeit für problematische Beiträge geben?
7. Soll es Gruppenbilder oder Icons geben?
8. Sollen archivierte Gruppen möglich sein?

---

## 17. Kurzfazit

Das Konzept ist sinnvoll, wenn Gruppentypen, globale Rollen und Gruppenrollen klar getrennt werden.

Die wichtigste Struktur ist:

```text
Globale Rolle entscheidet, wer Gruppen erstellen und systemweit verwalten darf.
Gruppenrolle entscheidet, was ein Nutzer innerhalb einer konkreten Gruppe darf.
Gruppeneinstellungen entscheiden, wie offen oder moderiert eine Gruppe funktioniert.
```

Damit bleibt das System flexibel genug für offizielle Hochschulkommunikation, Kursorganisation und freie Campus-Community-Gruppen.

---

## 18. Umsetzungsstand des MVP

Der beschriebene MVP ist umgesetzt. Dazu gehören Gruppentypen und Erstellrechte, Gruppenrollen, zentrale Gruppeneinstellungen, Mitgliederverwaltung, Beitrittsanfragen, Einladungen und die Trennung von globalem Admin-Zugriff und sichtbarer Gruppenrolle.

Für moderierte Gruppen gilt:

- Beiträge normaler Mitglieder erhalten bei aktivierter Freigabe zunächst den Status `Pending`.
- Besitzer, Moderatoren, berechtigte Kurslehrende und Admins veröffentlichen direkt.
- Ausstehende Beiträge erscheinen in der Moderationskarte der Gruppeneinstellungen und können dort freigegeben oder endgültig abgelehnt werden.
- Der normale Feed zeigt ausschließlich veröffentlichte Beiträge.

Kommentare werden zweistufig gesteuert. Die Gruppe muss Kommentare grundsätzlich erlauben; zusätzlich entscheidet der Autor beim Erstellen, ob der einzelne Beitrag kommentierbar ist.

Besitzer und Moderatoren dürfen Inhalte ihrer Gruppe moderieren. Besitzer dürfen eigene Campus- und offizielle Gruppen löschen. Kursgruppen dürfen ausschließlich von Admins gelöscht werden und können durch die automatische Kurssynchronisierung später erneut entstehen. Beim Löschen einer Gruppe werden ihre Beiträge ebenfalls gelöscht.

Weiterhin nicht Bestandteil des MVP sind Beitragsbearbeitung, Meldungen, Gruppenbilder, Archivierung und die Umwandlung eines Gruppentyps.
