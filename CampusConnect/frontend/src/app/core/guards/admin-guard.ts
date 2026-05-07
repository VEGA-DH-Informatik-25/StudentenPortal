import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { Auth } from '../services/auth';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  if (auth.userRole() === 'Admin') {
    return true;
  }

  if (!auth.isLoggedIn()) {
    return auth.restoreSession().pipe(
      map(isRestored => isRestored && auth.userRole() === 'Admin' ? true : router.createUrlTree(['/feed']))
    );
  }

  return router.createUrlTree(['/feed']);
};
