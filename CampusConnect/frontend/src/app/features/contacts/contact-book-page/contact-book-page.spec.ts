import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ContactBookPage } from './contact-book-page';

describe('ContactBookPage', () => {
  let fixture: ComponentFixture<ContactBookPage>;
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [ContactBookPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ContactBookPage);
    http = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('should show the search entry point and favorites section without a full contact list', () => {
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Kontakte suchen');
    expect(text).toContain('Meine Favoriten');
    expect(fixture.nativeElement.querySelector('.contact-list')).toBeNull();
  });

  it('should open the contact search modal from the search card', () => {
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.contact-search-card button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="dialog"]')).not.toBeNull();
  });

  it('should add and remove a favorite from the search results', () => {
    vi.useFakeTimers();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.contact-search-card button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('#contact-search-query') as HTMLInputElement;
    input.value = 'Ali';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(350);

    const request = http.expectOne('/api/contacts?query=Ali&limit=10');
    request.flush([{
      id: 'contact-1',
      displayName: 'Alice Example',
      email: 'alice@dhbw-loerrach.de',
      studyProgram: 'Computer Science',
      course: 'TIF25A',
      phoneNumber: '',
      location: '',
      profileNote: '',
      role: 'Student',
    }]);
    fixture.detectChanges();

    const favoriteButton = fixture.nativeElement.querySelector('.contact-result-card__favorite') as HTMLButtonElement;
    favoriteButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.favorites-dropdown__count')?.textContent.trim()).toBe('1');
    expect(fixture.nativeElement.textContent).toContain('Alice Example');
    expect(fixture.nativeElement.textContent).toContain('alice@dhbw-loerrach.de');

    favoriteButton.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.favorites-dropdown__count')?.textContent.trim()).toBe('0');
  });

  it('restores saved favorite contacts', async () => {
    localStorage.setItem('campusconnect.contacts.favorites', JSON.stringify([{
      id: 'contact-2',
      displayName: 'Bob Example',
      email: 'bob@dhbw-loerrach.de',
      studyProgram: 'Business Informatics',
      course: 'WWI25A',
      phoneNumber: '',
      location: '',
      profileNote: '',
      role: 'Student',
    }]));

    fixture = TestBed.createComponent(ContactBookPage);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.favorites-dropdown__count')?.textContent.trim()).toBe('1');
    expect(fixture.nativeElement.textContent).toContain('Bob Example');
  });
});
