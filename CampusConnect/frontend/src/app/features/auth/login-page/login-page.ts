import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly _auth = inject(Auth);
  private readonly _router = inject(Router);

  protected readonly _mode = signal<'login' | 'register'>('login');
  protected readonly _error = signal('');
  protected readonly _isLoading = signal(false);

  protected readonly _loginForm = { email: '', password: '' };
  protected readonly _registerForm = {
    email: '',
    password: '',
    displayName: '',
    studyProgram: '',
    semester: 1,
    course: '',
  };

  protected switchMode(mode: 'login' | 'register'): void {
    this._mode.set(mode);
    this._error.set('');
  }

  protected onLogin(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._auth.login({ email: this._loginForm.email, password: this._loginForm.password }).subscribe({
      next: () => this._router.navigate(['/feed']),
      error: err => {
        this._error.set(err.error?.error ?? 'Anmeldung fehlgeschlagen.');
        this._isLoading.set(false);
      },
    });
  }

  protected onRegister(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._auth.register(this._registerForm).subscribe({
      next: () => this._router.navigate(['/feed']),
      error: err => {
        this._error.set(err.error?.error ?? 'Registrierung fehlgeschlagen.');
        this._isLoading.set(false);
      },
    });
  }
}
