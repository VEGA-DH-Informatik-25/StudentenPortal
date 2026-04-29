import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { Auth } from './auth';
import { AuthResponse, UserProfile } from '../models/auth.model';

describe('Auth', () => {
  let service: Auth;
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(Auth);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should store the full profile returned by login', () => {
    service.login({ email: profile.email, password: 'secret' }).subscribe();

    const request = http.expectOne('/api/auth/login');
    const response: AuthResponse = {
      token: 'jwt-token',
      displayName: profile.displayName,
      email: profile.email,
      role: profile.role,
      profile,
    };
    request.flush(response);

    expect(service.getToken()).toBe('jwt-token');
    expect(service.isLoggedIn()).toBe(true);
    expect(service.userProfile()).toEqual(profile);
    expect(service.displayName()).toBe(profile.displayName);
    expect(service.userRole()).toBe(profile.role);
  });

  it('should update the cached profile after saving changes', () => {
    const updatedProfile = { ...profile, displayName: 'Alice A.', semester: 4 };

    service.updateProfile({
      displayName: updatedProfile.displayName,
      course: updatedProfile.course,
      phoneNumber: updatedProfile.phoneNumber,
      location: updatedProfile.location,
      profileNote: updatedProfile.profileNote,
    }).subscribe();

    const request = http.expectOne('/api/auth/me');
    expect(request.request.method).toBe('PUT');
    request.flush(updatedProfile);

    expect(service.userProfile()).toEqual(updatedProfile);
    expect(service.displayName()).toBe('Alice A.');
  });
});
