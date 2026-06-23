import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';

import { Auth } from '../services/auth';
import { authGuard } from './auth-guard';

describe('authGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => authGuard(...guardParameters));
  const isLoggedIn = signal(false);
  const userProfile = signal(null);
  let restoreSession: ReturnType<typeof vi.fn>;
  let router: Router;

  beforeEach(() => {
    isLoggedIn.set(false);
    userProfile.set(null);
    restoreSession = vi.fn(() => of(false));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: { isLoggedIn, userProfile, restoreSession } },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('should allow logged-in users', () => {
    isLoggedIn.set(true);

    expect(executeGuard({} as never, {} as never)).toBe(true);
  });

  it('should restore cookie sessions before redirecting', async () => {
    restoreSession.mockReturnValue(of(true));

    const result = await firstValueFrom(executeGuard({} as never, {} as never) as Observable<boolean | UrlTree>);

    expect(result).toBe(true);
  });

  it('should redirect anonymous users to login', async () => {
    const result = await firstValueFrom(executeGuard({} as never, {} as never) as Observable<boolean | UrlTree>);

    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
  });
});
