import { Routes } from '@angular/router';
import { App } from './app';
import { AdminComponent } from './admin/admin.component';
import { RootComponent } from './root/root.component';

export const routes: Routes = [
  { path: 'admin', component: AdminComponent },
  { path: 'app', component: App },
  { path: '', component: RootComponent },
  { path: '**', redirectTo: '' }
];
