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
| POST | `/api/admin/users` | Benutzer mit Initialpasswort, Rolle und Kurs anlegen | Ja, Admin |
| PUT | `/api/admin/users/{id}` | Name, E-Mail, Rolle und Kurs eines Benutzers aktualisieren | Ja, Admin |
| PATCH | `/api/admin/users/{id}/role` | Rolle eines Benutzers ändern | Ja, Admin |
| PATCH | `/api/admin/users/{id}/course` | Kurszuordnung eines Benutzers ändern | Ja, Admin |
| PATCH | `/api/admin/users/{id}/status` | Benutzer aktiv oder inaktiv setzen | Ja, Admin |
| DELETE | `/api/admin/users/{id}` | Benutzer löschen | Ja, Admin |
| GET | `/api/feed` | Paginierten News-Feed mit Gruppenkontext abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag in einer Gruppe erstellen | Ja |
| POST | `/api/feed/{id}/approve` | Ausstehenden Beitrag veröffentlichen (Gruppenverwaltung) | Ja |
| DELETE | `/api/feed/{id}` | Eigenen oder moderierbaren Beitrag löschen | Ja |
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
| POST | `/api/groups` | Gruppe mit Typ `Campus`, `Official` oder `Course` erstellen (Kursgruppen benötigen `courseCode`, offizielle Gruppen `officialCategory`); optional `joinRule` (`Open`, `RequestRequired`, `InviteOnly`) | Ja |
| GET | `/api/groups/{id}/settings` | Bearbeitbare Gruppendetails inklusive Mitglieder, offener Beitrittsanfragen und Einladungen abrufen | Ja |
| PUT | `/api/groups/{id}/settings` | Gruppeneinstellungen wie Kommentare, Freigabe, Sichtbarkeit und Beitrittsregel ändern | Ja |
| DELETE | `/api/groups/{id}` | Gruppe samt Beiträgen löschen (Besitzer/Admin; Kursgruppen nur Admin) | Ja |
| GET | `/api/groups/{id}/pending-posts` | Ausstehende Beiträge für die Moderation abrufen | Ja |
| GET | `/api/groups/{id}/candidates` | Personen für die Aufnahme suchen (`query` optional, ohne Treffer für Kursgruppen) | Ja |
| POST | `/api/groups/{id}/members` | Eine oder mehrere Personen als Mitglieder hinzufügen (`userIds`) | Ja |
| POST | `/api/groups/{id}/members/course` | Alle aktuellen Mitglieder eines Kurses einmalig als Mitglieder übernehmen (`courseCode`) | Ja |
| DELETE | `/api/groups/{id}/members/{userId}` | Ein Mitglied aus der Gruppe entfernen (nicht den Besitzer) | Ja |
| PUT | `/api/groups/{id}/members/{userId}/role` | Gruppenrolle eines Mitglieds setzen (`Member` oder `Moderator`) | Ja |
| POST | `/api/groups/{id}/join` | Beitreten (`Open`) oder Beitritt anfragen (`RequestRequired`); offene Einladungen werden direkt angenommen | Ja |
| POST | `/api/groups/{id}/leave` | Eigene Mitgliedschaft beenden; Besitzer senden bei weiteren Mitgliedern `newOwnerUserId`, alleinige Besitzer lÃ¶schen die Gruppe beim Verlassen | Ja |
| POST | `/api/groups/{id}/requests/{userId}/approve` | Offene Beitrittsanfrage freigeben (Verwaltungsrecht) | Ja |
| POST | `/api/groups/{id}/requests/{userId}/reject` | Offene Beitrittsanfrage ablehnen (Verwaltungsrecht) | Ja |
| POST | `/api/groups/{id}/invitations` | Personen einladen (`userIds`, Verwaltungsrecht) | Ja |
| DELETE | `/api/groups/{id}/invitations/{userId}` | Einladung zurückziehen (Verwaltungsrecht) | Ja |
| POST | `/api/groups/{id}/invitations/accept` | Eigene Einladung annehmen | Ja |
| POST | `/api/groups/{id}/invitations/decline` | Eigene Einladung ablehnen | Ja |

> **Hinweis:** Externe API-Clients authentifizieren sich weiterhin mit folgendem HTTP-Header:
> ```
> Authorization: Bearer <token>
> ```
> Das Token wird über `POST /api/auth/login` bezogen. Browser-Sitzungen nutzen zusätzlich ein HttpOnly-Cookie, das bei Logout oder nach 15 Minuten Inaktivität ungültig wird.

## Nutzer, Kurse und Gruppen

Kurse sind die Quelle für akademische Profilattribute. Ein Kurs besteht aus `code` (z. B. `TIF25A`), `studyProgram`, `semester`, `isActive` und `createdAt`. Registrierung und Profiländerung senden nur den Kurscode; Studiengang und Semester werden serverseitig aus dem Kurskatalog übernommen. Admins können neue aktive Kurse anlegen und Benutzer in der Benutzerverwaltung einem anderen Kurs zuordnen.

Jeder Benutzer hat genau einen Kurscode im Profil. Für jeden aktiven Kurs existiert eine Kursgruppe mit identischem `courseCode`. Die Mitgliedschaft dieser Kursgruppen wird aus den Benutzerprofilen abgeleitet und automatisch synchronisiert; manuelle Mitgliederänderungen in den Gruppeneinstellungen sind deshalb für Kursgruppen gesperrt. Offizielle Gruppen und Campusgruppen verwalten ihre Mitglieder manuell. Ein ganzer Kurs kann über `POST /api/groups/{id}/members/course` einmalig als Momentaufnahme in eine Nicht-Kursgruppe übernommen werden; spätere Kursänderungen wirken sich dann nicht mehr automatisch auf diese Gruppe aus.

## Stundenplan

`GET /api/timetable` verwendet ohne `course`-Query den Kurscode des angemeldeten Profils. Dadurch können Clients den eigenen Stundenplan kursneutral abrufen. Wird `course` gesetzt, kann derselbe Endpunkt jeden Kurs aus dem Kurskatalog oder einen manuell eingegebenen Kurscode laden. `days` steuert die Länge des geladenen Zeitfensters; mit `from` kann ein Startdatum im Format `yyyy-MM-dd` gesetzt werden, damit Kalenderansichten auch vergangene Wochen gezielt nachladen können. Ohne `from` startet das Zeitfenster am Montag der aktuellen Woche. Die externe iCal-URL und optionale Kurs-Aliase werden über `Timetable:CalendarUrlTemplate`, `Timetable:MaxLookaheadDays` und `Timetable:CourseAliases` konfiguriert, damit neue Kurse oder abweichende Kalenderpostfächer ohne Codeänderung ergänzt werden können.

## Noten und Studienplan

Der Notenbereich liest den Studienplan nicht aus einer manuell gepflegten Modulliste, sondern löst den Kurs des angemeldeten Nutzers gegen die öffentlichen DHBW-Studienplan-Indexseiten auf. Für Lörrach werden die dort verlinkten Modulhandbuch-PDFs geladen und serverseitig geparst. `GET /api/grades/plan` liefert die Module, ECTS, Studienjahr, Prüfungsform und den Erfassungsstatus für den aktuellen Kurs zurück. Wenn für einen Kurs kein eindeutiger Plan gefunden wird, antwortet der Endpunkt mit `404 Not Found` und einer `{ error = ... }`-Meldung.

`POST /api/grades` akzeptiert bevorzugt `moduleCode` aus diesem Plan und `value`; Modulname und ECTS werden dann serverseitig aus dem Studienplan übernommen. Für Kurse ohne gefundenen Plan bleibt die manuelle Eingabe mit `moduleName`, `ects` und `value` möglich. Eine `moduleCode`, die nicht im Kursplan des angemeldeten Nutzers vorkommt, wird abgelehnt.

## Gruppen und Feed

Der Feed ist gruppenbasiert. Jeder Beitrag enthält ein `group`-Objekt mit Name, Typ (`Course`, `Official`, `Campus`), Zielgruppe, Kürzel, Akzentfarbe, Besitzer-ID, Anzahl der Mitglieder, der Gruppenrolle `groupRole` (`Owner`, `Moderator`, `Member` oder `None`) und den daraus abgeleiteten Fähigkeits-Flags `isAssigned`, `canManage`, `canEditSettings`, `canManageMembers`, `canAppointModerator`, `canPost`, `canInteract`, `canJoin`, `canRequestJoin`, `hasPendingJoinRequest`, `hasPendingInvitation`, `isSystemAdminAccess`, `isCourseManaged` sowie den Einstellungen. Zusätzlich enthält ein Beitrag `canDelete`, `canComment`, `comments` und `reactions`. Neue Beiträge können optional mit `groupId` erstellt werden; ohne `groupId` wird die Kursgruppe des angemeldeten Nutzers verwendet, sofern ein Kurs im Profil hinterlegt ist.

Feed-Antworten enthalten nur veröffentlichte Beiträge aus Gruppen, für deren Beiträge der Nutzer leseberechtigt ist: Admins sehen alle veröffentlichten Beiträge, zugewiesene Mitglieder sehen die veröffentlichten Beiträge ihrer Gruppen. Private Gruppen erscheinen nur für Admins und zugewiesene Mitglieder; öffentliche Gruppen erscheinen zusätzlich als Entdecken-Kandidaten, geben ihre Beiträge aber erst nach Beitritt oder Zuweisung frei.

Wer posten darf, ergibt sich aus der Gruppenrolle: Besitzer und Moderatoren dürfen immer posten, einfache Mitglieder nur, wenn `allowStudentPosts` aktiv ist (`canPost`). Ist `requiresApproval` aktiv, starten Beiträge einfacher Mitglieder mit Status `Pending`; Beiträge von Besitzern, Moderatoren, berechtigten Kurslehrenden und Admins werden direkt als `Published` gespeichert. Berechtigte Gruppenverwalter rufen die Warteschlange über `GET /api/groups/{id}/pending-posts` ab und veröffentlichen Beiträge über `POST /api/feed/{id}/approve`. Ablehnen löscht den ausstehenden Beitrag über `DELETE /api/feed/{id}`.

`POST /api/feed` akzeptiert neben `content` und `groupId` das optionale Feld `allowComments`. Kommentare sind nur möglich, wenn sowohl `group.settings.allowComments` als auch `post.allowComments` aktiv sind. Besitzer, Moderatoren, berechtigte Kurslehrende und Admins dürfen fremde Beiträge und Kommentare innerhalb ihrer verwalteten Gruppen entfernen.

Emoji-Reaktionen sind als Toggle modelliert: sendet derselbe Nutzer dasselbe Emoji erneut, wird die Reaktion entfernt. Es gibt keine feste Emoji-Liste; akzeptiert werden gültige Emoji-Zeichen oder Emoji-Sequenzen, nicht freier Text.

Gruppeneinstellungen enthalten aktuell:

| Feld | Bedeutung |
|---|---|
| `allowStudentPosts` | Studierende dürfen in der Gruppe Beiträge veröffentlichen |
| `allowComments` | Beiträge der Gruppe sind kommentierbar |
| `requiresApproval` | Neue Beiträge benötigen Moderation/Freigabe |
| `isDiscoverable` | Gruppe ist öffentlich und kann unter Entdecken gefunden werden; `false` macht sie privat |
| `joinRule` | Beitrittsregel: `Open` (sofort beitreten), `RequestRequired` (Beitritt per Anfrage) oder `InviteOnly` (nur per Einladung) |

Zusätzlich tragen Gruppen das Feld `officialCategory` (fachliche Einordnung offizieller Gruppen, z. B. `Prüfungsamt`). Jedes Gruppen-Objekt liefert außerdem `canRequestJoin`, `hasPendingJoinRequest`, `hasPendingInvitation` und `pendingJoinRequestCount` für die Beitritts- und Einladungslogik.

Global roles are separate from group roles: `Student`, `Lecturer`, `Management`, and `Admin` describe system-wide permissions; `Owner`, `Moderator`, and `Member` describe a user's role inside a specific group. Students and lecturers can discover public groups, read posts from assigned groups, comment and react in their groups, join public campus groups through `POST /api/groups/{id}/join`, leave manually managed groups through `POST /api/groups/{id}/leave`, and create their own campus groups. The global `Management` role can create campus groups, official groups, and course groups like `Admin`; course groups require a `courseCode` and continue to be synchronized when user-course assignments change. The creator of a group is its `Owner`. Owners and moderators open group settings, search candidates through `GET /api/groups/{id}/candidates`, add members through `POST /api/groups/{id}/members`, add an entire course as a one-time snapshot through `POST /api/groups/{id}/members/course`, remove members through `DELETE /api/groups/{id}/members/{userId}`, and set member roles through `PUT /api/groups/{id}/members/{userId}/role`. If an owner leaves while other members remain, the request must include `newOwnerUserId`; if the owner is the only member, leaving deletes the group and its posts. Only the owner (or a system admin) can appoint moderators or edit group settings. Admins can manage all group settings and members; lecturers can manage the course groups they are assigned to. `GET /api/groups/{id}/settings`, `PUT /api/groups/{id}/settings`, the member endpoints, and the role endpoint return `403 Forbidden` for unauthorized users; for course groups, the candidate, member, course, and leave endpoints reject manual changes so course membership stays consistent.

### Gruppenrollen (Besitzer / Moderator / Mitglied)

Zusätzlich zur globalen Rolle hat jeder Nutzer pro Gruppe eine eigene Gruppenrolle. Sie ist die primäre Berechtigungsquelle und wird in `groupRole` jedes Gruppen-Objekts sowie pro Mitglied in `GET /api/groups/{id}/settings` ausgegeben:

| Gruppenrolle | Ableitung | Bedeutung |
|---|---|---|
| `Owner` (Besitzer) | `ownerUserId == userId` | Volle Kontrolle: Einstellungen, Mitglieder, Moderatoren ernennen, Gruppe löschen |
| `Moderator` | als Mitglied mit Rolle `Moderator` geführt | Beiträge, Mitgliederverwaltung (keine Einstellungen, keine Moderatorernennung) |
| `Member` (Mitglied) | als Mitglied geführt | Lesen, kommentieren, reagieren; posten nur bei `allowStudentPosts` |
| `None` | nicht Mitglied | Keine Gruppenrolle |

Die konkreten Fähigkeiten werden serverseitig aus Gruppenrolle und Gruppeneinstellungen abgeleitet und als Flags am Gruppen-Objekt geliefert (`canEditSettings`, `canManageMembers`, `canAppointModerator`, `canPost`, `canInteract`). Nur der Besitzer (oder ein systemweiter Admin) darf Moderatoren ernennen, also über `PUT /api/groups/{id}/members/{userId}/role` die Rolle `Moderator` vergeben. Versucht ein Moderator das, antwortet die API mit `403 Forbidden` und der Meldung `You are not allowed to manage this group.`. Das Gruppen-Objekt liefert hierfür `canAppointModerator`.

Der systemweite Admin-Zugriff ist von der eigentlichen Gruppenrolle getrennt: Ein Admin kann jede Gruppe verwalten (`canManage = true`), erscheint dabei aber nicht als Besitzer. Ist der Admin nicht selbst Mitglied, gilt `groupRole = None` und `isSystemAdminAccess = true`, sodass das UI klar zwischen Admin-Zugriff und Gruppenrolle unterscheiden kann. `canDelete` wird ebenfalls serverseitig berechnet: Besitzer dürfen eigene Campus- und offizielle Gruppen löschen, Admins zusätzlich Kursgruppen. Beim Löschen werden alle Beiträge der Gruppe entfernt; eine Kursgruppe kann durch die Kurssynchronisierung später neu entstehen.
