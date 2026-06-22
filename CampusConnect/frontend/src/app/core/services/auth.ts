import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, finalize, map, shareReplay, tap } from 'rxjs/operators';
import { Observable, of } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UpdateProfileRequest, UserProfile } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly _idleTimeoutMs = 15 * 60 * 1000;
  private readonly _keepAliveIntervalMs = 5 * 60 * 1000;
  private readonly _activityEvents = ['click', 'keydown', 'mousemove', 'scroll', 'touchstart', 'focus'];
  private readonly _http = inject(HttpClient);
  private readonly _router = inject(Router);

  private _token: string | null = null;
  private _restoreSession$: Observable<boolean> | null = null;
  private _idleTimerId: number | null = null;
  private _lastKeepAliveAt = 0;
  private _hasActivityListeners = false;
  private readonly _activityHandler = () => this._recordActivity();

  readonly isLoggedIn = signal(false);
  readonly displayName = signal('');
  readonly userRole = signal('');
  readonly userProfile = signal<UserProfile | null>(null);

  login(req: LoginRequest): Observable<AuthResponse> {
    return this._http.post<AuthResponse>('/api/auth/login', req).pipe(
      tap(res => this._storeSession(res))
    );
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this._http.post<AuthResponse>('/api/auth/register', req).pipe(
      tap(res => this._storeSession(res))
    );
  }

  loadProfile(): Observable<UserProfile> {
    return this._http.get<UserProfile>('/api/auth/me').pipe(
      tap(profile => this._storeAuthenticatedProfile(profile))
    );
  }

  updateProfile(req: UpdateProfileRequest): Observable<UserProfile> {
    return this._http.put<UserProfile>('/api/auth/me', req).pipe(
      tap(profile => this._storeProfile(profile))
    );
  }

  logout(): void {
    this.clearSession();
    this._http.post<void>('/api/auth/logout', {}).subscribe({ error: () => undefined });
  }

  clearSession(redirectToLogin = true): void {
    this._token = null;
    this.isLoggedIn.set(false);
    this.displayName.set('');
    this.userRole.set('');
    this.userProfile.set(null);
    this._stopInactivityTracking();
    if (redirectToLogin) {
      this._router.navigate(['/login']);
    }
  }

  getToken(): string | null {
    return this._token;
  }

  restoreSession(): Observable<boolean> {
    if (this.isLoggedIn()) {
      return of(true);
    }

    if (this._restoreSession$) {
      return this._restoreSession$;
    }

    this._restoreSession$ = this.loadProfile().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession(false);
        return of(false);
      }),
      finalize(() => {
        this._restoreSession$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    return this._restoreSession$;
  }

  private _storeSession(res: AuthResponse): void {
    this._token = res.token;
    this._storeAuthenticatedProfile(res.profile ?? this._profileFromAuthResponse(res));
  }

  private _storeAuthenticatedProfile(profile: UserProfile): void {
    this.isLoggedIn.set(true);
    this._storeProfile(profile);
    this._lastKeepAliveAt = Date.now();
    this._startInactivityTracking();
  }

  private _storeProfile(profile: UserProfile): void {
    this.userProfile.set(profile);
    this.displayName.set(profile.displayName);
    this.userRole.set(profile.role);
  }

  private _profileFromAuthResponse(res: AuthResponse): UserProfile {
    return {
      id: '',
      email: res.email,
      displayName: res.displayName,
      studyProgram: '',
      course: '',
      phoneNumber: '',
      location: '',
      profileNote: '',
      role: res.role,
      createdAt: '',
    };
  }

  private _startInactivityTracking(): void {
    if (typeof window === 'undefined') {
      return;
    }

    if (!this._hasActivityListeners) {
      for (const eventName of this._activityEvents) {
        window.addEventListener(eventName, this._activityHandler, { passive: true });
      }
      this._hasActivityListeners = true;
    }

    this._resetIdleTimer();
  }

  private _stopInactivityTracking(): void {
    if (typeof window === 'undefined') {
      return;
    }

    if (this._idleTimerId !== null) {
      window.clearTimeout(this._idleTimerId);
      this._idleTimerId = null;
    }

    if (this._hasActivityListeners) {
      for (const eventName of this._activityEvents) {
        window.removeEventListener(eventName, this._activityHandler);
      }
      this._hasActivityListeners = false;
    }
  }

  private _recordActivity(): void {
    if (!this.isLoggedIn()) {
      return;
    }

    this._resetIdleTimer();
    const now = Date.now();
    if (now - this._lastKeepAliveAt < this._keepAliveIntervalMs) {
      return;
    }

    this._lastKeepAliveAt = now;
    this.loadProfile().subscribe({ error: () => this.clearSession() });
  }

  private _resetIdleTimer(): void {
    if (typeof window === 'undefined') {
      return;
    }

    if (this._idleTimerId !== null) {
      window.clearTimeout(this._idleTimerId);
    }

    this._idleTimerId = window.setTimeout(() => this.logout(), this._idleTimeoutMs);
  }
}

