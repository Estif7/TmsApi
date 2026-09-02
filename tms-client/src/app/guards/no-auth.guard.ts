import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth';

export const noAuthGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If already logged in, block access to /login
  if (authService.currentUser() !== null) {
    router.navigate(['/dashboard'], { replaceUrl: true });
    return false;
  }

  return true;
};