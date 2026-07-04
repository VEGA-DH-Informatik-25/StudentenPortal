# MVP-Must-have-Kritik

Stand: 2026-07-04

Dieses Dokument fasst eine statische Prüfung der Must-have-Kriterien aus [`../../prd-mvp.md`](../../prd-mvp.md) gegen den aktuellen Live-Code, die vorhandenen Tests und die Projektdokumentation zusammen. Es ersetzt die geschützte PRD nicht. Wenn eine hier genannte Abweichung fachlich gewollt ist, sollte entweder die PRD bewusst angepasst oder die Implementierung nachgezogen werden.

Automatisierte Tests wurden für diese Prüfung nicht erneut ausgeführt; bewertet wurden Code, Routen, Services, Tests und Dokumentationsnachweise.

## Kurzfazit

Die Definition of Done aus der PRD ist aktuell nicht vollständig nachweisbar. Mehrere Kernbereiche sind umgesetzt, aber es gibt harte PRD-Abweichungen bei Admin-Passwort-Reset, E-Mail-Domainfreiheit, selbst pflegbarer Kurszuordnung, Kontaktbuch-Datenumfang, Profilnotiz und Teilen des Onboardings.

Die vorhandene Statusmatrix [`anforderungsstatus.md`](anforderungsstatus.md) ist dadurch an mehreren Stellen zu optimistisch. Besonders Einträge mit Status "erfüllt" sollten nach diesem Audit neu bewertet werden.

## Harte Abweichungen

| Priorität | PRD-Kriterium | Befund | Evidenz |
|---|---|---|---|
| Hoch | Nutzeranlage ohne Domain-Restriktion | Admin-Nutzeranlage ist implementiert, aber nicht ohne Domain-Restriktion. Die PRD verlangt ausdrücklich "keine Domain-Restriktion"; der Service akzeptiert nur `@dhbw-loerrach.de`. | PRD Zeile 32; `AdminUsersService.ValidateEmail` in `backend/CampusConnect.Application/Features/Admin/AdminUsersService.cs` Zeilen 229-235 |
| Hoch | Admin kann Passwort zurücksetzen | Kein Admin-Reset-Endpunkt und keine Service-Methode für Passwort-Reset gefunden. Der Admin-Bereich unterstützt Erstellen, Rolle/Kurs/Status ändern und Löschen, aber nicht "Passwort zurücksetzen". | PRD Zeile 43; `AdminController` Zeilen 26, 74, 84, 98; `AdminUsersService` enthält Commands für Rolle/Kurs/Status/Löschen, aber keinen Reset-Command |
| Hoch | Onboarding nach Admin-Reset | Weil der Admin-Passwort-Reset fehlt, ist auch der PRD-Fall "oder nach Admin-Reset" nicht erfüllt. | PRD Zeile 34; fehlender Reset-Pfad siehe vorheriger Befund |
| Hoch | Profil: Nutzer kann Kurs pflegen und Kursliste im UI auswählen | Profilseite zeigt den Kurs nur readonly an; Backend weist Kursänderungen über `PUT /api/auth/me` zurück. Eine Kursliste ist im Profil-UI nicht auswählbar. | PRD Zeile 36; `profile-page.html` Zeilen 53-54; `AuthService.CourseChangeNotAllowedError` und Prüfung in `AuthService.cs` Zeilen 20 und 84-85 |
| Hoch | Kontaktbuch: sichtbare Kontaktdaten inkl. Telefon und Standort, keine Profilnotiz | Kontakt-Suchergebnisse zeigen Name, Rolle, Kurs, Studiengang und E-Mail, aber nicht Telefon und Standort. Gleichzeitig gibt die API `ProfileNote` zurück und sucht sogar darin; die Profilnotiz wird außerdem in Hover-Cards angezeigt. Das widerspricht sowohl dem Kontaktbuch-Kriterium "keine Profilnotiz" als auch dem Nicht-Ziel "Profilnotiz-Funktion". | PRD Zeilen 42 und 64; `contact-result-card.html` Zeilen 27-37; `ContactsService.cs` Zeilen 14, 47, 57; `profile-hover-card.html` Zeilen 46-47 |
| Mittel/Hoch | Rollen: Lecturer kann in Official posten | Lecturer-Rolle existiert und Kursgruppenrechte sind weitgehend umgesetzt. Ein explizites globales Recht "Lecturer kann in Official posten" ist aber nicht erkennbar: `CanPost` erlaubt Admin, Owner/Moderator, Mitglieder je nach Gruppeneinstellung oder Course-Lecturer nur in Kursgruppen. In Official-Gruppen hängt Lecturer-Posting damit von Gruppenrolle/Einstellung ab, nicht von der globalen Lecturer-Rolle. | PRD Zeile 35; `GroupsService.cs` Zeilen 731-740 und 787-788 |
| Mittel/Hoch | Onboarding-Feed / Guided Start gemäß `onboarding.md` | Grundroute, Passwortwechsel und Guided-Tour-Service sind vorhanden, aber der implementierte Flow ist fachlich nicht vollständig: Nach Passwortwechsel lädt `loadCampusData` nur Gruppen, startet die globale Tour und navigiert direkt nach `/feed`; die Onboarding-Schritte `tour` und `groups` der Seite werden im normalen Passwortwechselpfad nicht fortgesetzt. Die Spezifikation verlangt außerdem Campus-Daten, Gruppenvorschläge, Dashboard/Welcome-Nachricht und Badge-Verhalten. | PRD Zeile 44; `docs/onboarding.md`; `onboarding-page.ts` Zeilen 111-120; `guided-tour.ts` Zeilen 59-61 |
| Mittel | Initialpasswortwechsel verpflichtend | Frontend-Guard leitet nicht abgeschlossene Nutzer zur Onboarding-Route. Backendseitig wird bei geschützten Fachendpunkten aber nur geprüft, ob der Nutzer aktiv ist; ein Nutzer mit gültigem Token und `MustChangePassword = true` kann geschützte API-Endpunkte direkt aufrufen. | PRD Zeile 34; `AuthenticatedUserValidator.cs` prüft nur Existenz/Aktivstatus; `AuthService.CompleteOnboardingAsync` blockiert nur Onboarding-Abschluss vor Passwortwechsel |
| Mittel | Zentrale Demo-/Abnahmenachweise | Mehrere Dokumente verlinken `CampusConnect/docs/demo-checkliste.md`, die Datei ist im aktuellen Workspace aber nicht vorhanden. Dadurch ist die fachliche Demo-Abnahme nicht zentral nachvollziehbar. | `docs/README.md`, `docs/project-overview.md`, `docs/abgabe-und-uebergabe.md`, `docs/qa-nachweis.md`, Root-`README.md` verweisen auf `demo-checkliste.md`; `rg --files CampusConnect/docs` findet nur `demo-data.md` als Demo-Datei |

## Teilweise oder mit Nachweisrisiko erfüllt

| PRD-Kriterium | Einschätzung | Kommentar |
|---|---|---|
| Auth Login/Logout und geschützte Bereiche | Weitgehend erfüllt | Login/Logout, Cookie/JWT und in-memory Token sind umgesetzt. Restrisiko ist die fehlende Backend-Gating-Regel für Nutzer mit offenem Initialpasswortwechsel. |
| News-Feed gruppenbasiert | Weitgehend erfüllt | Feed, Gruppenbezug, Kommentare, Reaktionen, Löschrechte und Moderation sind implementiert und getestet. |
| Gruppen | Weitgehend erfüllt | Gruppentypen, Erstellrechte, Rollen, Einstellungen, Join/Request/Invite/Leave und Moderation sind im Code und in Tests sichtbar. Siehe auch `docs/concepts/groups.md`, Abschnitt "Umsetzungsstand des MVP". |
| Mensa-Speiseplan | Weitgehend erfüllt | Backend-Proxy nutzt Location-ID 677 als Default, Frontend zeigt Wochen-/Tagesansicht und Fehler-/Leerzustände. Externe Verfügbarkeit und API-Key-Konfiguration bleiben operative Risiken. |
| Noten-Tracker | Erfüllt nach statischer Prüfung | Manuelle Noten mit ECTS, Durchschnitt und Löschen sind in Backend/Frontend vorhanden. |
| Stundenplan | Erfüllt nach statischer Prüfung | Backend kann Profilkurs oder expliziten Kurs laden; Frontend bietet Kursauswahl und Tages-/Wochen-/Listenansicht. |
| Admin-Zugriff nur Admin | Weitgehend erfüllt | AdminController ist mit `Authorize(Roles = "Admin")` geschützt und Frontend hat `adminGuard`. Der fehlende Passwort-Reset bleibt die fachliche Lücke des Admin-Bereichs. |
| Laptop und iPad | Teilweise nachgewiesen | Playwright konfiguriert Desktop Chrome, iPad Portrait und iPad Landscape und prüft horizontalen Overflow. Die Doku nennt weiterhin eine offene manuelle visuelle Tablet-Abnahme. |

## Zusätzliche PRD-Widersprüche

- Die PRD nennt "Profilnotiz-Funktion" als Nicht-Ziel, der aktuelle Code enthält jedoch `profileNote` im Profilformular, Auth-Modell, Kontakt-DTO und Hover-Card.
- Die PRD nennt "Prüfungskalender im UI" als Nicht-Ziel, gleichzeitig ist eine Kalenderroute vorhanden. Das kann als bewusstes Could-have gelten, sollte aber in der finalen MVP-Abnahme sauber als Zusatzumfang und nicht als Must-have verkauft werden.
- Die Statusmatrix markiert "Profil und Kurszuordnung", "Kontaktbuch" und "Admin-Bereich" als erfüllt, obwohl die oben genannten PRD-Kriterien nicht vollständig erfüllt sind.

## Empfohlene nächste Schritte

1. Fachentscheidung treffen: Soll die PRD gelten oder die aktuelle Produktentscheidung? Besonders bei E-Mail-Domain, Kurs-Selbständerung und Profilnotiz muss eine Seite angepasst werden.
2. Admin-Passwort-Reset implementieren: API-Endpunkt, Service-Command, UI-Aktion, Tests und `MustChangePassword = true` nach Reset.
3. Onboarding härten: Backend-Gating für Nutzer mit offenem Initialpasswortwechsel oder bewusst dokumentierte Ausnahme; Guided-Start-Flow so korrigieren, dass Tour, Gruppenvorschläge und Abschluss nachvollziehbar durchlaufen werden.
4. Kontaktbuch an PRD anpassen: Telefon und Standort sichtbar machen; Profilnotiz aus Kontakt-API/Suche/UI entfernen oder PRD ändern.
5. Lecturer-Official-Posting explizit entscheiden und testen.
6. `demo-checkliste.md` wiederherstellen oder alle Links auf ein existierendes Abnahmedokument umstellen.
7. Nach Änderungen die Statusmatrix, API-Doku, Testfallkatalog und QA-Nachweise aktualisieren und anschließend Backend-, Frontend- und Playwright-Suites ausführen.
