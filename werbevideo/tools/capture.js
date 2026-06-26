// Captures fresh CampusConnect app screenshots for the Werbevideo.
// Logs in as the main demo student (Lena, TIF25A) against the locally running
// app and writes viewport (cc-*) and full-page (full-*) PNGs into app-screenshots/.
//
// Usage:  node capture.js            (requires frontend on :4200, backend on :5135)
const { chromium } = require('playwright');
const path = require('path');

const FRONTEND = process.env.FRONTEND || 'http://localhost:4200';
const EMAIL = process.env.DEMO_EMAIL || 'lena.tif25a@dhbw-loerrach.de';
const PASSWORD = process.env.DEMO_PASSWORD || 'CampusDemo2026!';
const OUT = path.resolve(__dirname, '..', 'app-screenshots');
const VW = 1440, VH = 900;

// route -> which captures to take. 'cc' = 1440x900 viewport, 'full' = full page.
const PAGES = [
  { route: 'feed',      cc: 'cc-feed.png',      full: 'full-feed.png' },
  { route: 'mensa',     cc: 'cc-mensa.png',     full: 'full-mensa.png' },
  { route: 'timetable', cc: 'cc-timetable.png', full: 'full-timetable.png' },
  { route: 'grades',    cc: 'cc-grades.png',    full: 'full-grades.png' },
  { route: 'groups',    cc: 'cc-groups.png',    full: 'full-groups.png' },
  { route: 'calendar',  cc: 'cc-calendar.png' },
  { route: 'contacts',  cc: 'cc-contacts.png' },
];

const sleep = ms => new Promise(r => setTimeout(r, ms));

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({
    viewport: { width: VW, height: VH },
    deviceScaleFactor: 1,
  });
  const page = await ctx.newPage();

  // --- login ---
  await page.goto(`${FRONTEND}/login`, { waitUntil: 'networkidle' });
  await page.fill('input[name="email"]', EMAIL);
  await page.fill('input[name="password"]', PASSWORD);
  await page.click('button[type="submit"]');
  await page.waitForURL('**/feed', { timeout: 20000 });
  await page.waitForLoadState('networkidle');
  console.log('logged in as', EMAIL);

  // --- per route ---
  for (const p of PAGES) {
    await page.goto(`${FRONTEND}/${p.route}`, { waitUntil: 'networkidle' });
    // give external data (mensa/timetable) and entrance animations time to settle
    await sleep(p.route === 'mensa' || p.route === 'timetable' ? 4000 : 1800);
    await page.waitForLoadState('networkidle').catch(() => {});

    if (p.cc) {
      await page.evaluate(() => window.scrollTo(0, 0));
      await sleep(300);
      await page.screenshot({ path: path.join(OUT, p.cc) });
      console.log('  saved', p.cc);
    }
    if (p.full) {
      await page.evaluate(() => window.scrollTo(0, 0));
      await sleep(200);
      await page.screenshot({ path: path.join(OUT, p.full), fullPage: true });
      console.log('  saved', p.full);
    }
  }

  await ctx.close();
  await browser.close();
  console.log('CAPTURE DONE');
})().catch(e => { console.error(e); process.exit(1); });
