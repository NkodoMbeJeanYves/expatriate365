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
    path: 'roles',
    loadComponent: () =>
      import('./pages/admin-roles/admin-roles.page').then(m => m.AdminRolesPage),
  },
];
