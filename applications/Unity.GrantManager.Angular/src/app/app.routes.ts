import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'configuration-management',
        pathMatch: 'full'
      },
      {
        path: 'configuration-management',
        loadChildren: () =>
          import('./features/configuration-management/configuration-management.routes').then(
            (m) => m.configurationManagementRoutes
          )
      }
    ]
  }
];
