// Records each animated scene to a webm via Playwright.
const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE = 'http://localhost:8099';
const OUT = path.resolve(__dirname, '..', 'clips');
const W = 1920, H = 1080;

const SCENES = [
  { name: 'intro',     url: `${BASE}/intro-scene.html?dur=4000`,           dur: 4000 },
  { name: 'feed',      url: `${BASE}/scene.html?slide=feed&dur=2900`,      dur: 2900 },
  { name: 'mensa',     url: `${BASE}/scene.html?slide=mensa&dur=2700`,     dur: 2700 },
  { name: 'timetable', url: `${BASE}/scene.html?slide=timetable&dur=3300`, dur: 3300 },
  { name: 'grades',    url: `${BASE}/scene.html?slide=grades&dur=2900`,    dur: 2900 },
  { name: 'groups',    url: `${BASE}/scene.html?slide=groups&dur=4000`,    dur: 4000 },
  { name: 'cta',       url: `${BASE}/cta-anim.html`,                       dur: 3800 },
];

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  fs.mkdirSync(OUT, { recursive: true });
  const only = process.argv.slice(2);
  const scenes = only.length ? SCENES.filter(s => only.includes(s.name)) : SCENES;
  const browser = await chromium.launch();
  for (const s of scenes) {
    const ctx = await browser.newContext({
      viewport: { width: W, height: H },
      deviceScaleFactor: 1,
      recordVideo: { dir: OUT, size: { width: W, height: H } },
    });
    const page = await ctx.newPage();
    await page.goto(s.url, { waitUntil: 'load' });
    // wait for fonts + scene start, then full animation + buffer
    try { await page.waitForFunction('window.__sceneReady===true', { timeout: 5000 }); } catch (e) {}
    await page.waitForTimeout(s.dur + 500);
    const vid = page.video();
    await ctx.close(); // finalizes the webm
    const p = await vid.path();
    const dest = path.join(OUT, `rec-${s.name}.webm`);
    fs.renameSync(p, dest);
    console.log(`recorded ${s.name} -> ${dest}`);
  }
  await browser.close();
  console.log('ALL DONE');
})().catch(e => { console.error(e); process.exit(1); });
