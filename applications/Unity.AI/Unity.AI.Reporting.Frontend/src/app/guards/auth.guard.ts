import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = async (route, state) => {
  console.log('=== AUTH GUARD ACTIVATED ===');
  console.log('Route:', state.url);

  const authService = inject(AuthService);
  const router = inject(Router);

  // Get debug info before authentication check
  const debugInfo = authService.getAuthDebugInfo();
  console.log('🔍 AUTH GUARD DEBUG:', debugInfo);

  const isAuthenticated = await authService.isAuthenticated();
  console.log('Is authenticated:', isAuthenticated);

  if (!isAuthenticated) {
    console.log('❌ Authentication failed - redirecting to access-denied');

    // Navigate to access denied page
    console.log('Navigating to /access-denied');
    return router.createUrlTree(['/access-denied']);
  }

  console.log('✓ Authentication successful - allowing access');
  return true;
};
