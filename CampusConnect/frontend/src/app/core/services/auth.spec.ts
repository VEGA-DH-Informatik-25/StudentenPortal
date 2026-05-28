import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { Auth } from './auth';
import { AuthResponse, UserProfile } from '../models/auth.model';

describe('Auth', () => {
  let service: Auth;
  let http: HttpTestingController;

  const profile: UserProfile = {
    id: 'user-1',
    email: 'alice@dhbw-loerrach.de',
    displayName: 'Alice',
    studyProgram: 'Computer Science',
    semester: 3,
    course: 'TIF25A',
    phoneNumber: '+49 7621 123456',
    location: 'Library',
    profileNote: 'Looking for a project group.',
    role: 'Student',
    createdAt: '2026-04-27T10:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: 'login', component: TestLoginComponent }])],
    });
    service = TestBed.inject(Auth);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.clearSession(false);
    vi.useRealTimers();
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

  it('should restore an authenticated cookie session from the profile endpoint', async () => {
    const restored = firstValueFrom(service.restoreSession());

    const request = http.expectOne('/api/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush(profile);

    await expect(restored).resolves.toBe(true);
    expect(service.getToken()).toBeNull();
    expect(service.isLoggedIn()).toBe(true);
    expect(service.userProfile()).toEqual(profile);
  });

  it('should clear the session after 15 minutes without activity', () => {
    vi.useFakeTimers();

    service.login({ email: profile.email, password: 'secret' }).subscribe();

    const request = http.expectOne('/api/auth/login');
    request.flush({
      token: 'jwt-token',
      displayName: profile.displayName,
      email: profile.email,
      role: profile.role,
      profile,
    } satisfies AuthResponse);

    vi.advanceTimersByTime(15 * 60 * 1000 - 1);
    expect(service.isLoggedIn()).toBe(true);

    vi.advanceTimersByTime(1);
    expect(service.isLoggedIn()).toBe(false);
    expect(service.getToken()).toBeNull();

    const logoutRequest = http.expectOne('/api/auth/logout');
    expect(logoutRequest.request.method).toBe('POST');
    logoutRequest.flush(null);
  });

  it('should reset the inactivity timer when the user is active', () => {
    vi.useFakeTimers();

    service.login({ email: profile.email, password: 'secret' }).subscribe();

    const request = http.expectOne('/api/auth/login');
    request.flush({
      token: 'jwt-token',
      displayName: profile.displayName,
      email: profile.email,
      role: profile.role,
      profile,
    } satisfies AuthResponse);

    vi.advanceTimersByTime(4 * 60 * 1000);
    window.dispatchEvent(new Event('click'));
    vi.advanceTimersByTime(14 * 60 * 1000);

    expect(service.isLoggedIn()).toBe(true);

    vi.advanceTimersByTime(60 * 1000);
    expect(service.isLoggedIn()).toBe(false);

    const logoutRequest = http.expectOne('/api/auth/logout');
    expect(logoutRequest.request.method).toBe('POST');
    logoutRequest.flush(null);
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

@Component({ template: '' })
class TestLoginComponent {}
