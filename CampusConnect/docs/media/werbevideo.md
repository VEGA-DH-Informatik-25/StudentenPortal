# CampusConnect – Werbevideo

20‑Sekunden‑Werbespot für **CampusConnect** (DHBW Lörrach), 1920×1080.

**Fertiges Video:** [`CampusConnect-Werbevideo.mp4`](werbevideo/CampusConnect-Werbevideo.mp4)

## Aufbau

Intro (Laptop + Tablet + Hook) → News‑Feed → Mensa → Stundenplan → Noten → Gruppen → Call‑to‑Action.

Jede Feature‑Szene zeigt **echte Screenshots der laufenden App** in einem Geräte‑Rahmen:
- Listen‑Seiten (Feed, Stundenplan, Gruppen) **scrollen** gleichmäßig.
- Kennzahl‑/Karten‑Seiten (Mensa, Noten) **zoomen** gezielt auf das Wichtige.

## Dateien

| Datei | Zweck |
|---|---|
| `werbevideo/CampusConnect-Werbevideo.mp4` | das fertige Video |
| `werbevideo/scene.html` | animierte Feature‑Szenen (Scroll/Zoom + Text), datengetrieben über `?slide=` |
| `werbevideo/intro-scene.html` | Intro‑Szene mit Laptop + Tablet + Hook‑Text |
| `werbevideo/cta-anim.html` | animierte Call‑to‑Action‑Endkarte |
| `werbevideo/tools/record.js` | nimmt jede Szene per Playwright als Video auf |
| `werbevideo/build.sh` | fügt die Segmente per ffmpeg (Überblendungen/Swipes) zusammen |
| `werbevideo/make.sh` | kompletter Rebuild (record → schneiden → assemblieren) |
| `werbevideo/app-screenshots/` | echte Screenshots der App (Quelle für die Szenen) |

## Neu bauen

Voraussetzungen: Node.js, ffmpeg, Python 3.

```bash
cd CampusConnect/docs/media/werbevideo/tools
npm install playwright
npx playwright install chromium
cd ..
bash make.sh
```

Die App selbst muss dafür **nicht** laufen – die Szenen nutzen die statischen Screenshots in `app-screenshots/`.
Neue Screenshots entstehen aus der laufenden App (siehe [`../product/setup.md`](../product/setup.md), Demo-Login in [`../information/demo-data.md`](../information/demo-data.md)).
