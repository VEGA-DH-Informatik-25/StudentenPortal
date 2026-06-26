import { expect, test, type Page } from '@playwright/test';

const demoPassword = 'CampusDemo2026!';
const tabletMenuToggleName = /Navigation umschalten|Toggle navigation menu/;

async function openNavigationIfCollapsed(page: Page): Promise<void> {
  const menuToggle = page.getByRole('button', { name: tabletMenuToggleName });
  if (await menuToggle.isVisible().catch(() => false)) {
    await menuToggle.click();
  }
}

async function navigateTo(page: Page, linkName: string | RegExp, urlPattern: RegExp): Promise<void> {
  await openNavigationIfCollapsed(page);
  await page.getByRole('link', { name: linkName }).click();
  await expect(page).toHaveURL(urlPattern);
  await expectNoPageHorizontalOverflow(page);
}

async function expectNoPageHorizontalOverflow(page: Page): Promise<void> {
  await expect
    .poll(() => page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth))
    .toBeLessThanOrEqual(1);
}

async function login(page: Page, email: string): Promise<void> {
  await page.goto('/login');
  await expectNoPageHorizontalOverflow(page);
  await page.getByLabel('E-Mail').fill(email);
  await page.getByLabel('Passwort').fill(demoPassword);
  await page.getByRole('button', { name: 'Anmelden' }).click();
  await expect(page).toHaveURL(/\/feed$/);
  await expect(page.getByRole('heading', { name: 'Aktuelles und Termine' })).toBeVisible();
  await expectNoPageHorizontalOverflow(page);
}

test('public legal placeholder pages are reachable without login', async ({ page }) => {
  const legalPages = [
    { path: '/legal/impressum', heading: 'Impressum' },
    { path: '/legal/datenschutz', heading: 'Datenschutzerklärung' },
    { path: '/legal/nutzungsordnung', heading: 'Nutzungsordnung' },
  ];

  for (const legalPage of legalPages) {
    await page.goto(legalPage.path);
    await expect(page.getByRole('heading', { name: legalPage.heading })).toBeVisible();
    await expect(page.getByText('Prüfpflichtiger Platzhalter')).toBeVisible();
    await expectNoPageHorizontalOverflow(page);
  }
});

test('demo student can sign in, navigate core features, and sign out', async ({ page }) => {
  await login(page, 'lena.tif25a@dhbw-loerrach.de');

  await navigateTo(page, 'Mensa', /\/mensa$/);
  await expect(page.getByRole('heading', { name: 'Mensa' })).toBeVisible();

  await navigateTo(page, 'Stundenplan', /\/timetable$/);
  await expect(page.getByRole('heading', { name: /Vorlesungsplan|TIF25A/ })).toBeVisible();
  await page.getByRole('button', { name: 'Woche' }).click();
  await expect(page.getByRole('button', { name: 'Heute' })).toBeVisible();
  await expectNoPageHorizontalOverflow(page);
  await page.getByRole('button', { name: 'Tag' }).click();
  await expect(page.getByRole('button', { name: 'Heute' })).toBeVisible();
  await expectNoPageHorizontalOverflow(page);
  await page.getByRole('button', { name: 'Liste' }).click();

  await navigateTo(page, 'Noten', /\/grades$/);
  await expect(page.getByRole('heading', { name: /Noten eintragen/ })).toBeVisible();

  await navigateTo(page, 'Gruppen', /\/groups$/);
  await expect(page.getByRole('heading', { name: 'Gruppen' })).toBeVisible();
  await page.getByRole('button', { name: 'Neue Gruppe erstellen' }).click();
  await expect(page.getByRole('dialog', { name: 'Neue Gruppe erstellen' })).toBeVisible();
  await page.getByRole('dialog', { name: 'Neue Gruppe erstellen' }).getByRole('button', { name: 'Abbrechen' }).first().click();

  await page.getByRole('tab', { name: /Entdecken/ }).click();
  const joinButton = page.getByRole('button', { name: 'Beitreten' }).first();
  if (await joinButton.isVisible().catch(() => false)) {
    await joinButton.click();
    await expect(page.getByText('Du bist der Gruppe beigetreten.')).toBeVisible();
  } else {
    await expect(page.getByRole('button', { name: /Öffnen|Beitritt anfragen/ }).first()).toBeVisible();
  }

  await navigateTo(page, 'Kontakte', /\/contacts$/);
  await expect(page.getByRole('heading', { name: 'Personen suchen' })).toBeVisible();
  await page.getByRole('button', { name: 'Kontakte suchen' }).click();
  await expect(page.getByRole('dialog', { name: 'Kontakte suchen' })).toBeVisible();
  await page.keyboard.press('Escape');

  await openNavigationIfCollapsed(page);
  await page.getByLabel('Benutzermenü öffnen').click();
  await page.getByRole('button', { name: 'Abmelden' }).click();
  await expect(page).toHaveURL(/\/login$/);
});

test('demo admin can open the admin area', async ({ page }) => {
  await login(page, 'demo.admin@dhbw-loerrach.de');

  await navigateTo(page, 'Admin', /\/admin$/);
  await expect(page.getByRole('heading', { name: 'Administration' })).toBeVisible();
  await page.getByRole('button', { name: 'Benutzer' }).click();
  await expect(page.getByRole('heading', { name: 'Benutzerverwaltung' })).toBeVisible();
  await page.getByRole('button', { name: '+ Neuer Nutzer' }).click();
  await expect(page.getByRole('dialog', { name: 'Neuer Nutzer' })).toBeVisible();
  await page.getByRole('button', { name: 'Editor schließen' }).click();
  await page.getByRole('button', { name: 'Kurse' }).click();
  await expect(page.getByRole('heading', { name: 'Kursverwaltung' })).toBeVisible();
  await expectNoPageHorizontalOverflow(page);
});
