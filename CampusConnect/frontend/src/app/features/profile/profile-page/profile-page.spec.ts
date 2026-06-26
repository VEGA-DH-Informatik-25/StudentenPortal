import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UserProfile } from '../../../core/models/auth.model';
import { ProfilePage } from './profile-page';

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;
  let http: HttpTestingController;
  let storage: Record<string, string>;

  const profile: UserProfile = {
    id: 'user-1',
    email: 'alice@dhbw-loerrach.de',
    displayName: 'Alice',
    studyProgram: 'Computer Science',
    course: 'TIF25A',
    phoneNumber: '+49 7621 123456',
    location: 'Library',
    profileNote: 'Looking for a project group.',
    role: 'Student',
    mustChangePassword: false,
    onboardingCompleted: true,
    onboardingCompletedAt: null,
    createdAt: '2026-04-27T10:00:00Z',
  };

  beforeEach(async () => {
    storage = {};
    vi.stubGlobal('localStorage', {
      getItem: vi.fn((key: string) => storage[key] ?? null),
      setItem: vi.fn((key: string, value: string) => {
        storage[key] = value;
      }),
      removeItem: vi.fn((key: string) => {
        delete storage[key];
      }),
      clear: vi.fn(() => {
        storage = {};
      }),
    });

    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePage);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http?.verify();
    vi.unstubAllGlobals();
  });

  it('should load the current user profile', () => {
    fixture.detectChanges();

    const request = http.expectOne('/api/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush(profile);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('alice@dhbw-loerrach.de');
    expect(text).toContain('TIF25A');
    expect(text).toContain('Profilnotiz');
  });

  it('shows the assigned course as read-only profile data', () => {
    fixture.detectChanges();

    const request = http.expectOne('/api/auth/me');
    request.flush(profile);
    fixture.detectChanges();

    http.expectNone('/api/courses');
    const courseSelect = fixture.nativeElement.querySelector('select[name="course"]');
    expect(courseSelect).toBeNull();

    const courseInput = fixture.nativeElement.querySelector('input[name="course"]') as HTMLInputElement | null;
    expect(courseInput?.readOnly).toBe(true);
    expect(courseInput?.value).toBe('TIF25A');

    const studyProgramInput = fixture.nativeElement.querySelector('input[name="studyProgram"]') as HTMLInputElement | null;
    expect(studyProgramInput?.readOnly).toBe(true);
    expect(studyProgramInput?.value).toBe('Computer Science');
  });
});
