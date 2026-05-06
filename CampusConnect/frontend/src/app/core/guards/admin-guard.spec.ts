import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, of } from 'rxjs';

import { Auth } from '../services/auth';
import { adminGuard } from './admin-guard';

describe('adminGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => adminGuard(...guardParameters));
  const isLoggedIn = signal(true);
  const userRole = signal('Student');
  let restoreSession: ReturnType<typeof vi.fn>;
  let router: Router;

  beforeEach(() => {
    isLoggedIn.set(true);
    userRole.set('Student');
    restoreSession = vi.fn(() => of(false));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: { isLoggedIn, userRole, restoreSession } },
      ],
    });
    router = TestBed.inject(Router);
  });

  it('should allow admins', () => {
    userRole.set('Admin');

    expect(executeGuard({} as never, {} as never)).toBe(true);
  });

  it('should redirect non-admin users to feed', () => {
    const result = executeGuard({} as never, {} as never) as UrlTree;

    expect(router.serializeUrl(result)).toBe('/feed');
  });

  it('should restore cookie sessions before checking the admin role', async () => {
    isLoggedIn.set(false);
    restoreSession.mockReturnValue(of(true));
    restoreSession.mockImplementation(() => {
      userRole.set('Admin');
      return of(true);
    });

    const result = await firstValueFrom(executeGuard({} as never, {} as never) as Observable<boolean | UrlTree>);

    expect(result).toBe(true);
  });
});
