# MVP-Must-have-Kritik

Stand: 2026-07-04

Dieses Dokument haelt die Pruefung der Must-have-Kriterien aus [`../../prd-mvp.md`](../../prd-mvp.md) gegen den aktuellen Umsetzungsstand fest. Die im Screenshot markierten Punkte wurden fachlich bewertet und entweder implementiert oder bewusst in der PRD auf den gewollten Produktstand angepasst.

## Kurzfazit

Die zuvor harten Abweichungen sind fuer die Abgabe bereinigt:

- Nutzeranlage bleibt bewusst auf `@dhbw-loerrach.de` beschraenkt und ist in der PRD angepasst.
- Kurswechsel bleiben bewusst administrativ und sind in der PRD angepasst.
- Admin-Passwort-Reset ist als API, Service-Regel und Admin-UI umgesetzt.
- Nach Admin-Reset wird der Erstlogin-/Passwortwechselzustand wieder aktiviert.
- Profilnotiz ist aus Auth-/Kontakt-API und sichtbarer UI entfernt; die Datenbankspalte bleibt als ungenutztes Legacy-Feld bestehen.
- Kontaktbuch zeigt Telefon und Standort und durchsucht keine Profilnotizen mehr.
- Lecturer/Official-Posting ist in der PRD an das bestehende Rollenmodell angepasst: Official-Posting erfolgt ueber Gruppenrolle oder Management/Admin.
- Onboarding ist dokumentarisch auf den aktuellen stabilen Flow gebracht.
- Demo-/Setup-Verweise zeigen auf `product/setup.md`.

## Bereinigte Befunde

| Urspruenglicher Befund | Entscheidung / Umsetzung | Aktueller Status |
|---|---|---|
| Nutzeranlage ohne Domain-Restriktion | PRD angepasst: `@dhbw-loerrach.de` ist gewollte Domain-Regel. | erledigt |
| Profil: Nutzer kann Kurs selbst pflegen | PRD angepasst: Nutzer pflegen Anzeigename, Telefon und Standort; Kurs wird durch Admin verwaltet. | erledigt |
| Admin kann Passwort nicht zuruecksetzen | `PATCH /api/admin/users/{id}/password`, Service-Regel, UI-Aktion und Tests ergaenzt. | erledigt |
| Onboarding nach Admin-Reset fehlt | Reset setzt `MustChangePassword = true`, `OnboardingCompleted = false`, `OnboardingCompletedAt = null`. | erledigt |
| Kontaktbuch ohne Telefon/Standort und mit Profilnotiz | Kontakt-DTO/Suche/UI liefern keine Profilnotiz mehr; Karten zeigen Telefon und Standort. | erledigt |
| Lecturer kann nicht global in Official posten | PRD an Rollenmodell angepasst: Official-Posting ueber Gruppenrolle oder Management/Admin. | erledigt |
| Onboarding-Konzept groesser als Implementierung | `concepts/onboarding.md` beschreibt jetzt den stabilen MVP-Flow; groessere Elemente sind optional. | erledigt |
| Fehlender Demo-/Abnahmenachweis | Existierende Setup-Checkliste unter `docs/product/setup.md` ist verlinkt. | erledigt |

## Verbleibende Hinweise Fuer Die Abgabe

| Bereich | Hinweis | Bewertung |
|---|---|---|
| Externe Datenquellen | Mensa und Stundenplan haengen von SWFR/iCal-Verfuegbarkeit und lokaler Konfiguration ab. | Betriebsrisiko, kein Implementierungsblocker |
| Profilnotiz-Legacyfeld | `Users.ProfileNote` bleibt wegen "keine EF-Migration" in der Datenbank und im Domainmodell bestehen. Es wird nicht mehr ueber Auth-/Kontakt-DTOs oder UI angeboten. | bewusst akzeptiert |
| Testnachweis | Die neuen und geaenderten Tests muessen fuer die finale Abgabe lokal/CI laufen. | wird ueber Testlauf nachgewiesen |

## Relevante Nachweise

- [`prd-mvp.md`](../../prd-mvp.md)
- [`anforderungsstatus.md`](anforderungsstatus.md)
- [`product/api.md`](product/api.md)
- [`product/setup.md`](product/setup.md)
- [`product/testing.md`](product/testing.md)
- [`product/testfallkatalog.md`](product/testfallkatalog.md)
