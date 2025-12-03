import { Routes } from '@angular/router';
import { App } from './app';
import { AdminComponent } from './admin/admin.component';
import { RootComponent } from './root/root.component';
import { AccessDeniedComponent } from './access-denied/access-denied.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  // Public route - no auth required
  { path: 'access-denied', component: AccessDeniedComponent },

  // Protected routes - require valid token
  { path: 'admin', component: AdminComponent, canActivate: [authGuard] },
  { path: 'app', component: App, canActivate: [authGuard] },
  { path: '', component: RootComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
