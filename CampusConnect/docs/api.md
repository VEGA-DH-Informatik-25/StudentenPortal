# API-Referenz

## Geplante Endpunkte

| Methode | Endpunkt | Beschreibung | Authentifizierung |
|---|---|---|---|
| POST | `/api/auth/register` | Registrierung mit Hochschul-E-Mail-Adresse | Nein |
| POST | `/api/auth/login` | Anmeldung und JWT-Empfang | Nein |
| GET | `/api/auth/me` | Aktuelles Benutzerprofil abrufen | Ja |
| PUT | `/api/auth/me` | Eigenes Benutzerprofil aktualisieren | Ja |
| GET | `/api/courses` | Aktive Kursauswahl für Registrierung und Profil abrufen | Nein |
| GET | `/api/admin/courses` | Kurse in der Administration auflisten | Ja, Admin |
| POST | `/api/admin/courses` | Neuen Kurs mit Code, Studiengang und Semester anlegen | Ja, Admin |
| PATCH | `/api/admin/users/{id}/course` | Kurszuordnung eines Benutzers ändern | Ja, Admin |
| GET | `/api/feed` | Paginierten News-Feed mit Gruppenkontext abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag in einer Gruppe erstellen | Ja |
| DELETE | `/api/feed/{id}` | Eigenen Beitrag löschen | Ja |
| POST | `/api/feed/{id}/comments` | Kommentar unter einem Beitrag erstellen | Ja |
| DELETE | `/api/feed/{postId}/comments/{commentId}` | Eigenen Kommentar löschen | Ja |
| POST | `/api/feed/{id}/reactions` | Emoji-Reaktion an einem Beitrag umschalten | Ja |
| GET | `/api/mensa` | Mensa-Speiseplan für die aktuelle Woche abrufen | Ja |
| GET | `/api/calendar` | Prüfungskalender-Einträge abrufen | Ja |
| POST | `/api/calendar` | Persönlichen Prüfungseintrag hinzufügen | Ja |
| GET | `/api/grades` | Noteneinträge abrufen | Ja |
| POST | `/api/grades` | Noteneintrag hinzufügen | Ja |
| DELETE | `/api/grades/{id}` | Eigenen Noteneintrag löschen | Ja |
| GET | `/api/groups` | Kursgruppen, offizielle Gruppen und Campusgruppen auflisten | Ja |
| POST | `/api/groups` | Eigene Campusgruppe erstellen | Ja |
| GET | `/api/groups/{id}/settings` | Bearbeitbare Gruppendetails inklusive zuweisbarer Konten abrufen | Ja |
| PUT | `/api/groups/{id}/settings` | Gruppeneinstellungen wie Kommentare, Freigabe und Schreibrechte ändern | Ja |
| PUT | `/api/groups/{id}/assignments` | Konten einer bearbeitbaren Gruppe zuweisen | Ja |
| PUT | `/api/groups/{id}/member-permissions` | Schreibrechte zugewiesener Gruppenmitglieder setzen | Ja |
| POST | `/api/groups/{id}/join` | Einer öffentlichen Campusgruppe beitreten | Ja |
| GET | `/api/contacts` | Alle Adressbuch-Einträge abrufen | Ja |
| GET | `/api/contacts/{id}` | Einzelnen Kontakt abrufen | Ja |
| POST | `/api/contacts` | Neuen Kontakt anlegen (Admin) | Ja |
| PUT | `/api/contacts/{id}` | Kontakt aktualisieren (Admin) | Ja |
| DELETE | `/api/contacts/{id}` | Kontakt löschen (Admin) | Ja |

> **Hinweis:** Alle authentifizierungspflichtigen Endpunkte erwarten folgenden HTTP-Header:
> ```
> Authorization: Bearer <token>
> ```
> Das Token wird über `POST /api/auth/login` bezogen und muss bei jeder Anfrage an eine geschützte Ressource mitgesendet werden.

## Nutzer, Kurse und Gruppen

Kurse sind die Quelle für akademische Profilattribute. Ein Kurs besteht aus `code` (z. B. `TIF25A`), `studyProgram`, `semester`, `isActive` und `createdAt`. Registrierung und Profiländerung senden nur den Kurscode; Studiengang und Semester werden serverseitig aus dem Kurskatalog übernommen. Admins können neue aktive Kurse anlegen und Benutzer in der Benutzerverwaltung einem anderen Kurs zuordnen.

Jeder Benutzer hat genau einen Kurscode im Profil. Für jeden aktiven Kurs existiert eine Kursgruppe mit identischem `courseCode`. Die Zuweisungen dieser Kursgruppen werden aus den Benutzerprofilen abgeleitet; manuelle Kontenzuweisungen in den Gruppeneinstellungen sind deshalb für Kursgruppen gesperrt. Offizielle Gruppen und Campusgruppen behalten ihre manuelle Kontenzuweisung.

## Gruppen und Feed

Der Feed ist gruppenbasiert. Jeder Beitrag enthält ein `group`-Objekt mit Name, Typ (`Course`, `Official`, `Social`), Zielgruppe, Kürzel, Akzentfarbe, Besitzer-ID, Anzahl zugewiesener Konten, den Berechtigungsflags `canManage`, `isAssigned`, `canPost`, `canJoin`, der aktuellen Mitgliedsberechtigung `memberPermission` (`ReadOnly` oder `ReadWrite`) und Einstellungen. Zusätzlich enthält ein Beitrag `canDelete`, `canComment`, `comments` und `reactions`. Neue Beiträge können optional mit `groupId` erstellt werden; ohne `groupId` wird die Kursgruppe des angemeldeten Nutzers verwendet, sofern ein Kurs im Profil hinterlegt ist.

Feed-Antworten enthalten nur Beiträge aus Gruppen, für deren Beiträge der Nutzer leseberechtigt ist: Admins sehen alle Beiträge, zugewiesene Mitglieder sehen die Beiträge ihrer Gruppen. Private Gruppen erscheinen nur für Admins und zugewiesene Mitglieder; öffentliche Gruppen erscheinen zusätzlich als Entdecken-Kandidaten, geben ihre Beiträge aber erst nach Beitritt oder Zuweisung frei. Beiträge, Kommentare und Reaktionen können nur von Admins oder Gruppenmitgliedern mit `ReadWrite` erstellt werden. Mitglieder mit `ReadOnly` dürfen Gruppen und Beiträge lesen, aber nicht posten, kommentieren oder reagieren. Für Studierende muss bei Beiträgen zusätzlich `allowStudentPosts` aktiv sein, Kommentare respektieren zusätzlich `allowComments`.

Emoji-Reaktionen sind als Toggle modelliert: sendet derselbe Nutzer dasselbe Emoji erneut, wird die Reaktion entfernt. Es gibt keine feste Emoji-Liste; akzeptiert werden gültige Emoji-Zeichen oder Emoji-Sequenzen, nicht freier Text.

Gruppeneinstellungen enthalten aktuell:

| Feld | Bedeutung |
|---|---|
| `allowStudentPosts` | Studierende dürfen in der Gruppe Beiträge veröffentlichen |
| `allowComments` | Beiträge der Gruppe sind kommentierbar |
| `requiresApproval` | Neue Beiträge benötigen Moderation/Freigabe |
| `isDiscoverable` | Gruppe ist öffentlich und kann unter Entdecken gefunden werden; `false` macht sie privat |

Studierende können öffentliche Gruppen entdecken, Beiträge zugewiesener Gruppen lesen, in zugewiesenen und freigegebenen Gruppen mit `ReadWrite` posten, öffentlichen Campusgruppen über `POST /api/groups/{id}/join` beitreten und eigene Campusgruppen erstellen. Die Erstellerin oder der Ersteller einer Campusgruppe kann deren Einstellungen öffnen, Konten zuweisen und über `PUT /api/groups/{id}/member-permissions` zugewiesene Konten auf `ReadOnly` oder `ReadWrite` setzen. Admins können alle Gruppeneinstellungen ändern; Lehrende können Kursgruppen verwalten, wenn sie der Kursgruppe zugewiesen sind. `GET /api/groups/{id}/settings`, `PUT /api/groups/{id}/settings`, `PUT /api/groups/{id}/assignments` und `PUT /api/groups/{id}/member-permissions` liefern für nicht berechtigte Nutzer `403 Forbidden`; bei Kursgruppen lehnt `PUT /api/groups/{id}/assignments` manuelle Zuweisungen ab, damit die Kursmitgliedschaft konsistent bleibt.
