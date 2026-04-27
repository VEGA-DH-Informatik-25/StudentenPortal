import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly _http = inject(HttpClient);
  private readonly _router = inject(Router);

  private _token: string | null = null;

  readonly isLoggedIn = signal(false);
  readonly displayName = signal('');
  readonly userRole = signal('');

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

  logout(): void {
    this._token = null;
    this.isLoggedIn.set(false);
    this.displayName.set('');
    this.userRole.set('');
    this._router.navigate(['/login']);
  }

  getToken(): string | null {
    return this._token;
  }

  private _storeSession(res: AuthResponse): void {
    this._token = res.token;
    this.isLoggedIn.set(true);
    this.displayName.set(res.displayName);
    this.userRole.set(res.role);
  }
}

