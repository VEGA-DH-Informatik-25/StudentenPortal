import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { I18n } from '../../../core/i18n/i18n';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { Auth } from '../../../core/services/auth';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly _auth = inject(Auth);
  private readonly _i18n = inject(I18n);
  private readonly _router = inject(Router);

  protected readonly _error = signal('');
  protected readonly _isLoading = signal(false);

  protected readonly _loginForm = { email: '', password: '' };

  protected onLogin(): void {
    this._isLoading.set(true);
    this._error.set('');
    this._auth.login({ email: this._loginForm.email, password: this._loginForm.password }).subscribe({
      next: response => {
        const target = response.profile?.onboardingCompleted === false ? '/onboarding' : '/feed';
        this._router.navigate([target]).then(navigated => {
          if (!navigated) {
            this._error.set(this._i18n.translate('login.failed'));
            this._isLoading.set(false);
          }
        });
      },
      error: error => {
        this._error.set(this._i18n.readError(error, 'login.failed'));
        this._isLoading.set(false);
      },
    });
  }
}
