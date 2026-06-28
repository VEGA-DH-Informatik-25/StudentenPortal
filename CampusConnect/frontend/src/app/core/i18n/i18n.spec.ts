import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { I18n } from './i18n';
import { translations } from './translations';

describe('I18n', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
    localStorage.clear();
    document.documentElement.lang = '';
  });

  it('defaults to German when no language is stored', () => {
    const service = TestBed.inject(I18n);

    expect(service.language()).toBe('de');
    expect(service.locale()).toBe('de-DE');
    expect(service.translate('login.login')).toBe('Anmelden');
    expect(document.documentElement.lang).toBe('de');
  });

  it('loads a stored language preference', () => {
    localStorage.setItem('campusconnect.language', 'en');

    const service = TestBed.inject(I18n);

    expect(service.language()).toBe('en');
    expect(service.locale()).toBe('en-US');
    expect(service.translate('login.login')).toBe('Log in');
    expect(document.documentElement.lang).toBe('en');
  });

  it('loads a stored French language preference', () => {
    localStorage.setItem('campusconnect.language', 'fr');

    const service = TestBed.inject(I18n);

    expect(service.language()).toBe('fr');
    expect(service.locale()).toBe('fr-FR');
    expect(service.translate('login.login')).toBe('Se connecter');
    expect(document.documentElement.lang).toBe('fr');
  });

  it('normalizes a stored language preference', () => {
    localStorage.setItem('campusconnect.language', ' DE ');

    const service = TestBed.inject(I18n);

    expect(service.language()).toBe('de');
    expect(localStorage.getItem('campusconnect.language')).toBe('de');
    expect(document.documentElement.lang).toBe('de');
  });

  it('ignores invalid language values', () => {
    const service = TestBed.inject(I18n);

    service.setLanguage('es');

    expect(service.language()).toBe('de');
    expect(localStorage.getItem('campusconnect.language')).toBeNull();
  });

  it('removes invalid stored language values', () => {
    localStorage.setItem('campusconnect.language', 'es');

    const service = TestBed.inject(I18n);

    expect(service.language()).toBe('de');
    expect(localStorage.getItem('campusconnect.language')).toBeNull();
  });

  it('updates the document language when language changes', () => {
    const service = TestBed.inject(I18n);

    service.setLanguage('EN');

    expect(service.language()).toBe('en');
    expect(localStorage.getItem('campusconnect.language')).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('normalizes and persists French when language changes', () => {
    const service = TestBed.inject(I18n);

    service.setLanguage('FR');

    expect(service.language()).toBe('fr');
    expect(service.locale()).toBe('fr-FR');
    expect(localStorage.getItem('campusconnect.language')).toBe('fr');
    expect(document.documentElement.lang).toBe('fr');
  });

  it('keeps translation keys and interpolation parameters aligned across languages', () => {
    const otherLanguages: Array<keyof typeof translations> = ['en', 'fr'];
    const referenceTranslations = translations.de;
    const referenceKeys = Object.keys(referenceTranslations).sort();

    for (const language of otherLanguages) {
      const localizedTranslations = translations[language];
      expect(Object.keys(localizedTranslations).sort()).toEqual(referenceKeys);

      for (const key of referenceKeys) {
        expect(interpolationTokens(localizedTranslations[key as keyof typeof localizedTranslations])).toEqual(
          interpolationTokens(referenceTranslations[key as keyof typeof referenceTranslations])
        );
      }
    }
  });

  it('interpolates translation parameters', () => {
    const service = TestBed.inject(I18n);

    expect(service.translate('admin.courseCreated', { code: 'TIF25A' })).toBe('Kurs TIF25A wurde angelegt.');
  });

  it('localizes known backend errors', () => {
    const service = TestBed.inject(I18n);
    const error = new HttpErrorResponse({
      error: { error: 'Invalid email address or password.' },
      status: 401,
    });

    expect(service.readError(error, 'login.failed')).toBe('Ungültige E-Mail-Adresse oder ungültiges Passwort.');
  });

  it('uses the localized fallback for unknown backend errors', () => {
    const service = TestBed.inject(I18n);
    const error = new HttpErrorResponse({
      error: { error: 'Unexpected backend detail.' },
      status: 400,
    });

    expect(service.readError(error, 'login.failed')).toBe('Anmeldung fehlgeschlagen.');
  });
});

function interpolationTokens(template: string): string[] {
  return Array.from(template.matchAll(/{{\s*(\w+)\s*}}/g), match => match[1]).sort();
}
