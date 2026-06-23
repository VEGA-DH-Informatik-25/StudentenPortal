# CampusConnect – Werbevideo

20‑Sekunden‑Werbespot für **CampusConnect** (DHBW Lörrach), 1920×1080.

**Fertiges Video:** [`CampusConnect-Werbevideo.mp4`](CampusConnect-Werbevideo.mp4)

## Aufbau

Intro (Laptop + Tablet + Hook) → News‑Feed → Mensa → Stundenplan → Noten → Lerngruppen → Call‑to‑Action.

Jede Feature‑Szene zeigt **echte Screenshots der laufenden App** in einem Geräte‑Rahmen:
- Listen‑Seiten (Feed, Stundenplan, Gruppen) **scrollen** gleichmäßig.
- Kennzahl‑/Karten‑Seiten (Mensa, Noten) **zoomen** gezielt auf das Wichtige.

## Dateien

| Datei | Zweck |
|---|---|
| `CampusConnect-Werbevideo.mp4` | das fertige Video |
| `scene.html` | animierte Feature‑Szenen (Scroll/Zoom + Text), datengetrieben über `?slide=` |
| `intro-scene.html` | Intro‑Szene mit Laptop + Tablet + Hook‑Text |
| `cta-anim.html` | animierte Call‑to‑Action‑Endkarte |
| `tools/record.js` | nimmt jede Szene per Playwright als Video auf |
| `build.sh` | fügt die Segmente per ffmpeg (Überblendungen/Swipes) zusammen |
| `make.sh` | kompletter Rebuild (record → schneiden → assemblieren) |
| `app-screenshots/` | echte Screenshots der App (Quelle für die Szenen) |

## Neu bauen

Voraussetzungen: Node.js, ffmpeg, Python 3.

```bash
cd tools && npm install playwright && npx playwright install chromium && cd ..
bash make.sh
```

Die App selbst muss dafür **nicht** laufen – die Szenen nutzen die statischen Screenshots in `app-screenshots/`.
Neue Screenshots entstehen aus der laufenden App (siehe `../CampusConnect/README.md`, Demo‑Login in `../CampusConnect/docs/demo-data.md`).
