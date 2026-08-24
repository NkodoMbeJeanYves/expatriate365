import { Routes } from '@angular/router';

export const FINANCES_ROUTES: Routes = [
  { path: '', redirectTo: 'overview', pathMatch: 'full' },
  {
    path: 'overview',
    loadComponent: () =>
      import('./pages/finances-page/finances.page').then(m => m.FinancesPage),
  },
];
