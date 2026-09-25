import { Routes } from '@angular/router';

import { ConfigurationManagementComponent } from './configuration-management.component';

// Phase 1 only implements Program Details. Later phases add sibling routes here
// (payments, notifications, etc.) as each ConfigurationManagement tab gets its own
// strangled feature, without needing to rewire the parent route - each would become
// another child alongside program-details, with its own side-menu entry.
export const configurationManagementRoutes: Routes = [
  {
    path: '',
    component: ConfigurationManagementComponent,
    children: [
      {
        path: '',
        redirectTo: 'program-details',
        pathMatch: 'full'
      },
      {
        path: 'program-details',
        loadComponent: () =>
          import('./program-details/program-details.component').then((m) => m.ProgramDetailsComponent)
      }
    ]
  }
];
