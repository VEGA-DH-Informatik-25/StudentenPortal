# API-Referenz

## Geplante Endpunkte

| Methode | Endpunkt | Beschreibung | Authentifizierung |
|---|---|---|---|
| POST | `/api/auth/register` | Registrierung mit Hochschul-E-Mail-Adresse | Nein |
| POST | `/api/auth/login` | Anmeldung und JWT-Empfang | Nein |
| GET | `/api/auth/me` | Aktuelles Benutzerprofil abrufen | Ja |
| PUT | `/api/auth/me` | Eigenes Benutzerprofil aktualisieren | Ja |
| GET | `/api/feed` | Paginierten News-Feed abrufen | Ja |
| POST | `/api/feed` | Neuen Beitrag erstellen | Ja |
| DELETE | `/api/feed/{id}` | Eigenen Beitrag löschen | Ja |
| GET | `/api/mensa` | Mensa-Speiseplan für die aktuelle Woche abrufen | Ja |
| GET | `/api/calendar` | Prüfungskalender-Einträge abrufen | Ja |
| POST | `/api/calendar` | Persönlichen Prüfungseintrag hinzufügen | Ja |
| GET | `/api/grades` | Noteneinträge abrufen | Ja |
| POST | `/api/grades` | Noteneintrag hinzufügen | Ja |
| GET | `/api/groups` | Alle Lerngruppen auflisten | Ja |
| POST | `/api/groups` | Lerngruppe erstellen | Ja |
| POST | `/api/groups/{id}/join` | Lerngruppe beitreten | Ja |
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
