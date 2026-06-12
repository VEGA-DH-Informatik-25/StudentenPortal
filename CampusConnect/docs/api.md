# API Reference

## Swagger And OpenAPI

In the development environment, the API exposes an interactive Swagger UI at `/swagger`. The OpenAPI document used by Swagger is available at `/swagger/v1/swagger.json`; the ASP.NET Core generated OpenAPI endpoint remains available at `/openapi/v1.json`.

Protected endpoints can be tested in Swagger through **Authorize** with the JWT from `POST /api/auth/login`. The token is sent as a bearer token.

## Implemented Endpoints

| Methode | Endpunkt | Beschreibung | Authentifizierung |
|---|---|---|---|
| POST | `/api/auth/register` | Registrierung mit Hochschul-E-Mail-Adresse | Nein |
| POST | `/api/auth/login` | Anmeldung und JWT-Empfang | Nein |
| POST | `/api/auth/logout` | Browser-Sitzung beenden und Auth-Cookie entfernen | Nein |
| GET | `/api/auth/me` | Aktuelles Benutzerprofil abrufen | Ja |
| PUT | `/api/auth/me` | Anzeigename, Kurs und optionale Kontaktdetails des eigenen Profils aktualisieren | Ja |
| GET | `/api/courses` | Aktive Kursauswahl für Registrierung und Profil abrufen | Nein |
| GET | `/api/contacts` | Kontaktbuch nach Name, E-Mail, Kurs, Studiengang oder Profildetails durchsuchen (`query` optional, `limit` optional) | Ja |
| GET | `/api/admin/courses` | Kurse in der Administration auflisten | Ja, Admin |
| POST | `/api/admin/courses` | Neuen Kurs mit Code, Studiengang und Semester anlegen | Ja, Admin |
| GET | `/api/admin/users` | Benutzer in der Administration auflisten | Ja, Admin |
| PATCH | `/api/admin/users/{id}/role` | Rolle eines Benutzers ändern | Ja, Admin |
| PATCH | `/api/admin/users/{id}/course` | Kurszuordnung eines Benutzers ändern | Ja, Admin |
| DELETE | `/api/admin/users/{id}` | Benutzer löschen | Ja, Admin |
| GET | `/api/feed` | Paginierten News-Feed mit Gruppenkontext abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag in einer Gruppe erstellen | Ja |
| DELETE | `/api/feed/{id}` | Eigenen Beitrag löschen | Ja |
| POST | `/api/feed/{id}/comments` | Kommentar unter einem Beitrag erstellen | Ja |
| DELETE | `/api/feed/{postId}/comments/{commentId}` | Eigenen Kommentar löschen | Ja |
| POST | `/api/feed/{id}/reactions` | Emoji-Reaktion an einem Beitrag umschalten | Ja |
| GET | `/api/mensa` | Mensa-Speiseplan für die aktuelle Woche abrufen | Ja |
| GET | `/api/calendar` | Prüfungskalender-Einträge abrufen | Ja |
| POST | `/api/calendar` | Persönlichen Prüfungseintrag hinzufügen | Ja |
| DELETE | `/api/calendar/{id}` | Eigenen Prüfungseintrag löschen | Ja |
| GET | `/api/grades` | Noteneinträge abrufen | Ja |
| GET | `/api/grades/plan` | Aus dem zugeordneten Kurs abgeleiteten DHBW-Studienplan mit Modulen und Prüfungsformen abrufen | Ja |
| POST | `/api/grades` | Noteneintrag hinzufügen | Ja |
| DELETE | `/api/grades/{id}` | Eigenen Noteneintrag löschen | Ja |
| GET | `/api/timetable` | Stundenplan für den Profilkurs oder einen explizit gewählten Kurs abrufen (`course` optional, `days` optional) | Ja |
| GET | `/api/groups` | Kursgruppen, offizielle Gruppen und Campusgruppen auflisten | Ja |
| POST | `/api/groups` | Gruppe mit Typ `Social`, `Official` oder `Course` erstellen (Kursgruppen benötigen `courseCode`) | Ja |
| GET | `/api/groups/{id}/settings` | Bearbeitbare Gruppendetails inklusive zuweisbarer Konten abrufen | Ja |
| PUT | `/api/groups/{id}/settings` | Gruppeneinstellungen wie Kommentare, Freigabe und Schreibrechte ändern | Ja |
| PUT | `/api/groups/{id}/assignments` | Konten einer bearbeitbaren Gruppe zuweisen | Ja |
| PUT | `/api/groups/{id}/member-permissions` | Berechtigungen (`ReadOnly`, `ReadWrite`, `Manage`) zugewiesener Gruppenmitglieder setzen | Ja |
| POST | `/api/groups/{id}/join` | Einer öffentlichen Campusgruppe beitreten | Ja |

> **Hinweis:** Externe API-Clients authentifizieren sich weiterhin mit folgendem HTTP-Header:
> ```
> Authorization: Bearer <token>
> ```
> Das Token wird über `POST /api/auth/login` bezogen. Browser-Sitzungen nutzen zusätzlich ein HttpOnly-Cookie, das bei Logout oder nach 15 Minuten Inaktivität ungültig wird.

## Nutzer, Kurse und Gruppen

Kurse sind die Quelle für akademische Profilattribute. Ein Kurs besteht aus `code` (z. B. `TIF25A`), `studyProgram`, `semester`, `isActive` und `createdAt`. Registrierung und Profiländerung senden nur den Kurscode; Studiengang und Semester werden serverseitig aus dem Kurskatalog übernommen. Admins können neue aktive Kurse anlegen und Benutzer in der Benutzerverwaltung einem anderen Kurs zuordnen.

Jeder Benutzer hat genau einen Kurscode im Profil. Für jeden aktiven Kurs existiert eine Kursgruppe mit identischem `courseCode`. Die Zuweisungen dieser Kursgruppen werden aus den Benutzerprofilen abgeleitet; manuelle Kontenzuweisungen in den Gruppeneinstellungen sind deshalb für Kursgruppen gesperrt. Offizielle Gruppen und Campusgruppen behalten ihre manuelle Kontenzuweisung.

## Stundenplan

`GET /api/timetable` verwendet ohne `course`-Query den Kurscode des angemeldeten Profils. Dadurch können Clients den eigenen Stundenplan kursneutral abrufen. Wird `course` gesetzt, kann derselbe Endpunkt jeden Kurs aus dem Kurskatalog oder einen manuell eingegebenen Kurscode laden. `days` steuert die Länge des geladenen Zeitfensters; mit `from` kann ein Startdatum im Format `yyyy-MM-dd` gesetzt werden, damit Kalenderansichten auch vergangene Wochen gezielt nachladen können. Ohne `from` startet das Zeitfenster am Montag der aktuellen Woche. Die externe iCal-URL und optionale Kurs-Aliase werden über `Timetable:CalendarUrlTemplate`, `Timetable:MaxLookaheadDays` und `Timetable:CourseAliases` konfiguriert, damit neue Kurse oder abweichende Kalenderpostfächer ohne Codeänderung ergänzt werden können.

## Noten und Studienplan

Der Notenbereich liest den Studienplan nicht aus einer manuell gepflegten Modulliste, sondern löst den Kurs des angemeldeten Nutzers gegen die öffentlichen DHBW-Studienplan-Indexseiten auf. Für Lörrach werden die dort verlinkten Modulhandbuch-PDFs geladen und serverseitig geparst. `GET /api/grades/plan` liefert die Module, ECTS, Studienjahr, Prüfungsform und den Erfassungsstatus für den aktuellen Kurs zurück. Wenn für einen Kurs kein eindeutiger Plan gefunden wird, antwortet der Endpunkt mit `404 Not Found` und einer `{ error = ... }`-Meldung.

`POST /api/grades` akzeptiert bevorzugt `moduleCode` aus diesem Plan und `value`; Modulname und ECTS werden dann serverseitig aus dem Studienplan übernommen. Für Kurse ohne gefundenen Plan bleibt die manuelle Eingabe mit `moduleName`, `ects` und `value` möglich. Eine `moduleCode`, die nicht im Kursplan des angemeldeten Nutzers vorkommt, wird abgelehnt.

## Gruppen und Feed

Der Feed ist gruppenbasiert. Jeder Beitrag enthält ein `group`-Objekt mit Name, Typ (`Course`, `Official`, `Social`), Zielgruppe, Kürzel, Akzentfarbe, Besitzer-ID, Anzahl zugewiesener Konten, den Berechtigungsflags `canManage`, `isAssigned`, `canPost`, `canJoin`, der aktuellen Mitgliedsberechtigung `memberPermission` (`ReadOnly`, `ReadWrite` oder `Manage`), der Gruppenrolle `groupRole` (`Owner`, `Moderator`, `Member` oder `None`), `isSystemAdminAccess`, `canAppointModerator` und Einstellungen. Zusätzlich enthält ein Beitrag `canDelete`, `canComment`, `comments` und `reactions`. Neue Beiträge können optional mit `groupId` erstellt werden; ohne `groupId` wird die Kursgruppe des angemeldeten Nutzers verwendet, sofern ein Kurs im Profil hinterlegt ist.

Feed-Antworten enthalten nur Beiträge aus Gruppen, für deren Beiträge der Nutzer leseberechtigt ist: Admins sehen alle Beiträge, zugewiesene Mitglieder sehen die Beiträge ihrer Gruppen. Private Gruppen erscheinen nur für Admins und zugewiesene Mitglieder; öffentliche Gruppen erscheinen zusätzlich als Entdecken-Kandidaten, geben ihre Beiträge aber erst nach Beitritt oder Zuweisung frei. Beiträge, Kommentare und Reaktionen können nur von Admins oder Gruppenmitgliedern mit `ReadWrite` oder `Manage` erstellt werden. Mitglieder mit `ReadOnly` dürfen Gruppen und Beiträge lesen, aber nicht posten, kommentieren oder reagieren. Für Studierende muss bei Beiträgen zusätzlich `allowStudentPosts` aktiv sein, Kommentare respektieren zusätzlich `allowComments`.

Emoji-Reaktionen sind als Toggle modelliert: sendet derselbe Nutzer dasselbe Emoji erneut, wird die Reaktion entfernt. Es gibt keine feste Emoji-Liste; akzeptiert werden gültige Emoji-Zeichen oder Emoji-Sequenzen, nicht freier Text.

Gruppeneinstellungen enthalten aktuell:

| Feld | Bedeutung |
|---|---|
| `allowStudentPosts` | Studierende dürfen in der Gruppe Beiträge veröffentlichen |
| `allowComments` | Beiträge der Gruppe sind kommentierbar |
| `requiresApproval` | Neue Beiträge benötigen Moderation/Freigabe |
| `isDiscoverable` | Gruppe ist öffentlich und kann unter Entdecken gefunden werden; `false` macht sie privat |

Global roles are separate from group roles: `Student`, `Lecturer`, `Management`, and `Admin` describe system-wide permissions; `ReadOnly`, `ReadWrite`, and `Manage` describe permissions inside a specific group. Students and lecturers can discover public groups, read posts from assigned groups, post in assigned and enabled groups with `ReadWrite` or `Manage`, join public campus groups through `POST /api/groups/{id}/join`, and create their own campus groups. The global `Management` role can create campus groups, official groups, and course groups like `Admin`; course groups require a `courseCode` and continue to be synchronized when user-course assignments change. The creator of a group can open its settings, assign accounts, and set assigned accounts to `ReadOnly`, `ReadWrite`, or `Manage` through `PUT /api/groups/{id}/member-permissions`. `Manage` additionally allows editing group settings and member administration. Admins can edit all group settings; lecturers can manage course groups when they are assigned to that course group. `GET /api/groups/{id}/settings`, `PUT /api/groups/{id}/settings`, `PUT /api/groups/{id}/assignments`, and `PUT /api/groups/{id}/member-permissions` return `403 Forbidden` for unauthorized users; for course groups, `PUT /api/groups/{id}/assignments` rejects manual assignments so course membership stays consistent.

### Gruppenrollen (Besitzer / Moderator / Mitglied)

Zusätzlich zur globalen Rolle hat jeder Nutzer pro Gruppe eine eigene Gruppenrolle. Sie wird aus Besitz und Mitgliedsberechtigung abgeleitet und in `groupRole` jedes Gruppen-Objekts sowie pro Konto in `GET /api/groups/{id}/settings` ausgegeben:

| Gruppenrolle | Ableitung | Bedeutung |
|---|---|---|
| `Owner` (Besitzer) | `ownerUserId == userId` | Volle Kontrolle über die Gruppe |
| `Moderator` | zugewiesen mit `Manage` | Moderation, Freigaben, Mitgliederverwaltung |
| `Member` (Mitglied) | zugewiesen mit `ReadOnly`/`ReadWrite` | Normales Gruppenmitglied |
| `None` | nicht zugewiesen | Keine Gruppenrolle |

Nur der Besitzer (oder ein systemweiter Admin) darf weitere Moderatoren ernennen, also über `PUT /api/groups/{id}/member-permissions` die Berechtigung `Manage` vergeben. Versucht ein Moderator das, antwortet die API mit `400 Bad Request` und der Meldung `Only the group owner can appoint moderators.`. Das Gruppen-Objekt liefert hierfür `canAppointModerator`.

Der systemweite Admin-Zugriff ist von der eigentlichen Gruppenrolle getrennt: Ein Admin kann jede Gruppe verwalten (`canManage = true`), erscheint dabei aber nicht als Besitzer. Ist der Admin nicht selbst Mitglied, gilt `groupRole = None` und `isSystemAdminAccess = true`, sodass das UI klar zwischen Admin-Zugriff und Gruppenrolle unterscheiden kann.
