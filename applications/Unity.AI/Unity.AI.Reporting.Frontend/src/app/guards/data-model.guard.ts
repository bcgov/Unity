import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guards the /models (Data Model) route behind the "Create/Edit Data Model" permission.
 *
 * Stack this AFTER authGuard: by the time it runs the user is already authenticated and
 * has AI Reporting access — they simply lack the child permission — so we send them back
 * to the main app rather than the access-denied page.
 *
 * The backend independently 403s every /api/data-models/* call, so this guard is UX only.
 */
export const dataModelGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.canEditDataModel()) {
    return true;
  }

  router.navigate(['/app']);
  return false;
};
