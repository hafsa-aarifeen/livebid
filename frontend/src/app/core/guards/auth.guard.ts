import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../../state/auth.store';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  // Bounce to login, remembering where they were headed
  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};
