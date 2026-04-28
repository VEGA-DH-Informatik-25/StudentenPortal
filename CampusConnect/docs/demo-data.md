# Development Demo Data

The API seeds development-only demo data when `ASPNETCORE_ENVIRONMENT=Development` and `DemoData:Enabled` is `true`.

The data is intended for local UI testing only. It is not loaded in the test host or production startup path.

## Sources Used

- `https://dhbw-loerrach.de/studieren/studienangebote` lists DHBW Loerrach Bachelor study offers across Wirtschaft, Technik, and Gesundheit. Demo course cohorts use those study program names with illustrative cohort codes such as `TIF25A` and `WDB25A`.
- `https://dhbw-loerrach.de/studierende` lists student hub topics such as Allgemeine Studienberatung, Bibliothek, Campus App, Webmail, Hochschulaktivitaeten, Moodle, Mensa, Pruefungsamt, Studienbeitraege, Studierendenwerk, Termine, Wohnungssuche, and Zusatzprogramme. Demo groups are based on those hub needs.

## Mock Accounts

All seeded accounts use the password from `DemoData:Password`; the default local password is `CampusDemo2026!`.

| Email | Role | Purpose |
| --- | --- | --- |
| `demo.admin@dhbw-loerrach.de` | Admin | Admin UI, course catalog, user/course assignments |
| `demo.technik@dhbw-loerrach.de` | Lecturer | Technical course/group management |
| `demo.wirtschaft@dhbw-loerrach.de` | Lecturer | Business course/group management |
| `lena.tif25a@dhbw-loerrach.de` | Student | Informatik course flow |
| `noah.wwi25a@dhbw-loerrach.de` | Student | Wirtschaftsinformatik course flow |
| `mia.wdb25a@dhbw-loerrach.de` | Student | Digital Business course flow |
| `jonas.tmb25a@dhbw-loerrach.de` | Student | Maschinenbau course flow |
| `sara.wgm24a@dhbw-loerrach.de` | Student | Gesundheitsmanagement course flow |
| `emil.gig25a@dhbw-loerrach.de` | Student | Gesundheitsversorgung course flow |

## Seeded In-Memory Areas

The development seeder fills the existing in-memory repositories for groups, feed posts with comments and reactions, personal grades, and personal exam entries each time the API starts. Users and courses are stored in SQLite so they are available to the admin screens and auth flow.

Disable local demo data with:

```powershell
dotnet user-secrets set "DemoData:Enabled" "false" --project .\CampusConnect.API\CampusConnect.API.csproj
```