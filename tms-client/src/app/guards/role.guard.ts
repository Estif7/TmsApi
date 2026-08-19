import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

export const roleGuard = (requiredRole: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.hasRole(requiredRole)
    ? true
    : router.createUrlTree(['/unauthorized']);
};
