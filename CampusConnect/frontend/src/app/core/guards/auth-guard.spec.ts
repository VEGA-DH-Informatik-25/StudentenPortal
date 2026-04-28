import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, UrlTree, provideRouter } from '@angular/router';

import { Auth } from '../services/auth';
import { authGuard } from './auth-guard';

describe('authGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => authGuard(...guardParameters));
  const isLoggedIn = signal(false);
  let router: Router;

  beforeEach(() => {
    isLoggedIn.set(false);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: { isLoggedIn } },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('should allow logged-in users', () => {
    isLoggedIn.set(true);

    expect(executeGuard({} as never, {} as never)).toBe(true);
  });

  it('should redirect anonymous users to login', () => {
    const result = executeGuard({} as never, {} as never) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login');
  });
});
