import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { Auth } from '../services/auth';

export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(Auth);
  const router = inject(Router);

  const onboardingRedirect = () => {
    const profile = auth.userProfile();
    return profile?.onboardingCompleted === false && state.url !== '/onboarding'
      ? router.createUrlTree(['/onboarding'])
      : true;
  };

  if (auth.isLoggedIn()) {
    return onboardingRedirect();
  }

  return auth.restoreSession().pipe(
    map(isRestored => isRestored ? onboardingRedirect() : router.createUrlTree(['/login']))
  );
};
