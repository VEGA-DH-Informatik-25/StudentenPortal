import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CanActivateFn, Router, UrlTree, provideRouter } from '@angular/router';

import { Auth } from '../services/auth';
import { adminGuard } from './admin-guard';

describe('adminGuard', () => {
  const executeGuard: CanActivateFn = (...guardParameters) =>
    TestBed.runInInjectionContext(() => adminGuard(...guardParameters));
  const userRole = signal('Student');
  let router: Router;

  beforeEach(() => {
    userRole.set('Student');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: Auth, useValue: { userRole } },
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
});
