# Mitwirken an CampusConnect

## Branch-Benennung

Präfix gefolgt von einer kurzen Kebab-Case-Beschreibung:

- `feature/` – neue Funktionalität
- `fix/` – Fehlerbehebungen
- `docs/` – Dokumentationsänderungen
- `chore/` – Wartungsaufgaben, Abhängigkeitsaktualisierungen
- `test/` – Tests hinzufügen oder aktualisieren

Beispiele: `feature/mensa-speiseplan`, `fix/auth-abgelaufenes-jwt`, `docs/api-notenendpunkte`

## Commit-Nachrichtenformat

Es gilt die [Conventional Commits](https://www.conventionalcommits.org/)-Spezifikation.

```
<typ>(<bereich>): <kurze Beschreibung>
```

Beispiele:
- `feat(mensa): Wochenspeiseplan-Komponente hinzufügen`
- `fix(auth): abgelaufenes JWT korrekt behandeln`
- `docs(api): Notenendpunkte dokumentieren`

## Pull-Request-Prozess

1. PR gegen den Branch `main` erstellen.
2. PR-Template vollständig ausfüllen.
3. Mindestens ein Review von einem Teammitglied anfordern.
4. Alle Review-Kommentare vor dem Merge adressieren.
5. Nach Freigabe **Squash-Merge** verwenden.

## Tests ausführen

**Frontend:**
```bash
cd frontend
ng test
```

**Backend:**
```bash
cd backend
dotnet test
```
