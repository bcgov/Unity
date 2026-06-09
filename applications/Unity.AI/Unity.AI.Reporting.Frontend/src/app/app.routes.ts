import { Routes } from '@angular/router';
import { App } from './app';
import { AdminComponent } from './admin/admin.component';
import { RootComponent } from './root/root.component';
import { AccessDeniedComponent } from './access-denied/access-denied.component';
import { authGuard } from './guards/auth.guard';
import { dataModelGuard } from './guards/data-model.guard';

export const routes: Routes = [
  // Public route - no auth required
  { path: 'access-denied', component: AccessDeniedComponent },

  // Protected routes - require valid token
  { path: 'admin', component: AdminComponent, canActivate: [authGuard] },
  { path: 'app', component: App, canActivate: [authGuard] },
  {
    path: 'models',
    loadComponent: () => import('./data-models/data-models-page').then(m => m.DataModelsPageComponent),
    canActivate: [authGuard, dataModelGuard],
  },
  { path: '', component: RootComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
