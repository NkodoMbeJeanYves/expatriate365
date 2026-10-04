import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  { path: '', redirectTo: 'users', pathMatch: 'full' },
  {
    path: 'audit',
    loadChildren: () => import('@audit/audit.routes').then(m => m.AUDIT_ROUTES),
  },
  {
    path: 'users',
    loadComponent: () =>
      import('./pages/admin-user-list/admin-user-list.page').then(m => m.AdminUserListPage),
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./pages/admin-settings/admin-settings.page').then(m => m.AdminSettingsPage),
  },
  {
    path: 'tenants',
    loadComponent: () =>
      import('./pages/admin-tenants/admin-tenants.page').then(m => m.AdminTenantsPage),
  },
];
