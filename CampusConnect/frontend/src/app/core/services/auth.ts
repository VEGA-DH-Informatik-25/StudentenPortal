import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest, UpdateProfileRequest, UserProfile } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly _http = inject(HttpClient);
  private readonly _router = inject(Router);

  private _token: string | null = null;

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
      tap(profile => this._storeProfile(profile))
    );
  }

  updateProfile(req: UpdateProfileRequest): Observable<UserProfile> {
    return this._http.put<UserProfile>('/api/auth/me', req).pipe(
      tap(profile => this._storeProfile(profile))
    );
  }

  logout(): void {
    this._token = null;
    this.isLoggedIn.set(false);
    this.displayName.set('');
    this.userRole.set('');
    this.userProfile.set(null);
    this._router.navigate(['/login']);
  }

  getToken(): string | null {
    return this._token;
  }

  private _storeSession(res: AuthResponse): void {
    this._token = res.token;
    this.isLoggedIn.set(true);
    this._storeProfile(res.profile ?? this._profileFromAuthResponse(res));
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
      semester: 1,
      course: '',
      phoneNumber: '',
      location: '',
      profileNote: '',
      role: res.role,
      createdAt: '',
    };
  }
}

