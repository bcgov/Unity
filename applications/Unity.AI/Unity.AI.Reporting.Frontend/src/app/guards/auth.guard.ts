import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = async (route, state) => {
  console.log('=== AUTH GUARD ACTIVATED ===');
  console.log('Route:', state.url);

  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  console.log('Token present:', !!token);
  console.log('Token value:', token ? `${token.substring(0, 20)}...` : 'null');

  const isAuthenticated = await authService.isAuthenticated();
  console.log('Is authenticated:', isAuthenticated);

  if (!isAuthenticated) {
    console.log('❌ Authentication failed - redirecting to access-denied');

    // Navigate to access denied page
    console.log('Navigating to /access-denied');
    router.navigate(['/access-denied']);
    return false;
  }

  console.log('✓ Authentication successful - allowing access');
  console.log('============================');
  return true;
};
