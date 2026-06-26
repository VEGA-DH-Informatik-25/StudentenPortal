import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { I18n } from '../../../core/i18n/i18n';
import { deTranslations, TranslationKey } from '../../../core/i18n/translations';
import { LegalPage } from './legal-page';

describe('LegalPage', () => {
  let fixture: ComponentFixture<LegalPage>;
  let legalPage: string;

  beforeEach(async () => {
    legalPage = 'impressum';

    await TestBed.configureTestingModule({
      imports: [LegalPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              get data() {
                return { legalPage };
              },
            },
          },
        },
        {
          provide: I18n,
          useValue: {
            translate: (key: TranslationKey) => deTranslations[key] ?? key,
          },
        },
      ],
    }).compileComponents();
  });

  it('renders the selected legal page with the project notice', () => {
    const text = renderPage('impressum');

    expect(text).toContain('Impressum');
    expect(text).toContain('Hinweis');
    expect(text).toContain('campusconnect@dhbw-loerrach.de');
  });

  it('renders the privacy policy for the local pilot operation', () => {
    const text = renderPage('datenschutz');

    expect(text).toContain('Datenschutzerklärung');
    expect(text).toContain('Hinweis zum Pilotbetrieb');
    expect(text).toContain('CampusConnect Projektteam');
    expect(text).toContain('campusconnect@dhbw-loerrach.de');
    expect(text).toContain('lokalen Pilotbetrieb');
    expect(text).toContain('Kein Tracking');
  });

  it('renders the usage rules for group communication without future chat wording', () => {
    const text = renderPage('nutzungsordnung');

    expect(text).toContain('Nutzungsordnung');
    expect(text).toContain('Hinweis zum Pilotbetrieb');
    expect(text).toContain('Beiträge, Kommentare, Reaktionen und Gruppenkommunikation');
    expect(text).toContain('Zugangsdaten dürfen nicht weitergegeben werden');
    expect(text).toContain('Beleidigungen, Diskriminierung, Drohungen');
    expect(text).toContain('campusconnect@dhbw-loerrach.de');
    expect(text).not.toContain('zukünftige Chat');
    expect(text).not.toContain('vor Veröffentlichung geprüft');
  });

  function renderPage(page: string): string {
    legalPage = page;
    fixture = TestBed.createComponent(LegalPage);
    fixture.detectChanges();
    return fixture.nativeElement.textContent;
  }
});
