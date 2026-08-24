import { Routes } from '@angular/router';

export const GOVERNANCE_ROUTES: Routes = [
  { path: '', redirectTo: 'overview', pathMatch: 'full' },
  {
    path: 'overview',
    loadComponent: () =>
      import('./pages/governance-page/governance.page').then(m => m.GovernancePage),
  },
];
