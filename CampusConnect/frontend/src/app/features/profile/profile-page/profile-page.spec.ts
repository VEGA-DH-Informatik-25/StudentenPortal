import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UserProfile } from '../../../core/models/auth.model';
import { ProfilePage } from './profile-page';

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;
  let http: HttpTestingController;

  const profile: UserProfile = {
    id: 'user-1',
    email: 'alice@dhbw-loerrach.de',
    displayName: 'Alice',
    studyProgram: 'Informatik',
    semester: 3,
    course: 'TIF25A',
    phoneNumber: '+49 7621 123456',
    location: 'Bibliothek',
    profileNote: 'Sucht eine Projektgruppe.',
    role: 'Student',
    createdAt: '2026-04-27T10:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfilePage);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should load the current user profile', () => {
    fixture.detectChanges();

    const coursesRequest = http.expectOne('/api/courses');
    expect(coursesRequest.request.method).toBe('GET');
    coursesRequest.flush([
      { code: 'TIF25A', studyProgram: 'Informatik', semester: 3, isActive: true, createdAt: '2026-04-27T10:00:00Z' },
    ]);

    const request = http.expectOne('/api/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush(profile);
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('alice@dhbw-loerrach.de');
    expect(text).toContain('TIF25A');
    expect(text).toContain('Profilnotiz');
  });
});
