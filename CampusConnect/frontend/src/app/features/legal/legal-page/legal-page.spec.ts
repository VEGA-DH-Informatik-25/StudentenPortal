import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { LegalPage } from './legal-page';

describe('LegalPage', () => {
  let fixture: ComponentFixture<LegalPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LegalPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { legalPage: 'impressum' } } } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LegalPage);
    fixture.detectChanges();
  });

  it('renders the selected legal placeholder page', () => {
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Impressum');
    expect(text).toContain('Platzhalter');
  });
});
