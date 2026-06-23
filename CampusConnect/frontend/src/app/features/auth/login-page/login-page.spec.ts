import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Auth } from '../../../core/services/auth';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let component: LoginPage;
  let fixture: ComponentFixture<LoginPage>;
  let login: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    localStorage.clear();
    login = vi.fn(() => of({ token: 'token', email: 'alice@dhbw-loerrach.de', displayName: 'Alice', role: 'Student' }));

    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        provideRouter([]),
        {
          provide: Auth,
          useValue: {
            login,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render only the login form', () => {
    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;

    expect(Array.from(buttons, button => button.textContent?.trim())).toEqual(['Anmelden']);
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
  });

  it('localizes known backend login errors', () => {
    login.mockReturnValueOnce(throwError(() => new HttpErrorResponse({
      error: { error: 'Invalid email address or password.' },
      status: 401,
    })));

    (component as any).onLogin();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Ungültige E-Mail-Adresse oder ungültiges Passwort.');
    expect(text).not.toContain('Invalid email address or password.');
  });
});
