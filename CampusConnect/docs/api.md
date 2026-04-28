# API-Referenz

## Geplante Endpunkte

| Methode | Endpunkt | Beschreibung | Authentifizierung |
|---|---|---|---|
| POST | `/api/auth/register` | Registrierung mit Hochschul-E-Mail-Adresse | Nein |
| POST | `/api/auth/login` | Anmeldung und JWT-Empfang | Nein |
| GET | `/api/auth/me` | Aktuelles Benutzerprofil abrufen | Ja |
| PUT | `/api/auth/me` | Eigenes Benutzerprofil aktualisieren | Ja |
| GET | `/api/feed` | Paginierten News-Feed mit Gruppenkontext abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag in einer Gruppe erstellen | Ja |
| DELETE | `/api/feed/{id}` | Eigenen Beitrag löschen | Ja |
| GET | `/api/mensa` | Mensa-Speiseplan für die aktuelle Woche abrufen | Ja |
| GET | `/api/calendar` | Prüfungskalender-Einträge abrufen | Ja |
| POST | `/api/calendar` | Persönlichen Prüfungseintrag hinzufügen | Ja |
| GET | `/api/grades` | Noteneinträge abrufen | Ja |
| POST | `/api/grades` | Noteneintrag hinzufügen | Ja |
| GET | `/api/groups` | Kursgruppen, offizielle Gruppen und Campusgruppen auflisten | Ja |
| POST | `/api/groups` | Eigene Campusgruppe erstellen | Ja |
| GET | `/api/groups/{id}/settings` | Bearbeitbare Gruppendetails inklusive zuweisbarer Konten abrufen | Ja |
| PUT | `/api/groups/{id}/settings` | Gruppeneinstellungen wie Kommentare, Freigabe und Schreibrechte ändern | Ja |
| PUT | `/api/groups/{id}/assignments` | Konten einer bearbeitbaren Gruppe zuweisen | Ja |
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

## Gruppen und Feed

Der Feed ist gruppenbasiert. Jeder Beitrag enthält ein `group`-Objekt mit Name, Typ (`Course`, `Official`, `Social`), Zielgruppe, Kürzel, Akzentfarbe, Besitzer-ID, Anzahl zugewiesener Konten, Bearbeitungsflag und Einstellungen. Neue Beiträge können optional mit `groupId` erstellt werden; ohne `groupId` wird die Kursgruppe des angemeldeten Nutzers verwendet, sofern ein Kurs im Profil hinterlegt ist.

Gruppeneinstellungen enthalten aktuell:

| Feld | Bedeutung |
|---|---|
| `allowStudentPosts` | Studierende dürfen in der Gruppe Beiträge veröffentlichen |
| `allowComments` | Beiträge der Gruppe sind kommentierbar |
| `requiresApproval` | Neue Beiträge benötigen Moderation/Freigabe |
| `isDiscoverable` | Gruppe ist im Gruppenverzeichnis sichtbar |

Studierende können Gruppen lesen, in freigegebenen Gruppen posten und eigene Campusgruppen erstellen. Die Erstellerin oder der Ersteller einer Campusgruppe kann deren Einstellungen öffnen und Konten zuweisen. Admins können alle Gruppeneinstellungen ändern; Lehrende können Kursgruppen verwalten. `GET /api/groups/{id}/settings`, `PUT /api/groups/{id}/settings` und `PUT /api/groups/{id}/assignments` liefern für nicht berechtigte Nutzer `403 Forbidden`.
