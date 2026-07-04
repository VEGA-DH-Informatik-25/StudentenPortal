import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { ContactSearchModal } from './contact-search-modal';

describe('ContactSearchModal', () => {
  let fixture: ComponentFixture<ContactSearchModal>;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactSearchModal],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ContactSearchModal);
    http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  it('should show a minimum length hint before searching', () => {
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Gib mindestens 3 Zeichen ein');
    expect(fixture.nativeElement.querySelector('.contact-result-card')).toBeNull();
  });

  it('should wait for at least 3 characters before sending a debounced search request', () => {
    vi.useFakeTimers();
    const input = fixture.nativeElement.querySelector('#contact-search-query') as HTMLInputElement;

    input.value = 'Al';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(400);

    http.expectNone('/api/contacts?query=Al&limit=10');

    input.value = 'Ali';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    vi.advanceTimersByTime(349);

    http.expectNone('/api/contacts?query=Ali&limit=10');

    vi.advanceTimersByTime(1);

    const request = http.expectOne('/api/contacts?query=Ali&limit=10');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('should render compact search results', () => {
    vi.useFakeTimers();
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
      phoneNumber: '+49 7621 123456',
      location: 'Library',
      role: 'Student',
    }]);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Alice Example');
    expect(text).toContain('TIF25A');
    expect(text).toContain('Computer Science');
    expect(text).toContain('+49 7621 123456');
    expect(text).toContain('Library');
    expect(text).toContain('alice@dhbw-loerrach.de');
    expect(fixture.nativeElement.querySelector('.contact-result-card__favorite')).not.toBeNull();
  });
});
