import { Routes } from '@angular/router';

export const ANALYTICS_ROUTES: Routes = [
  { path: '', redirectTo: 'overview', pathMatch: 'full' },
  {
    path: 'overview',
    loadComponent: () =>
      import('./pages/analytics-page/analytics.page').then(m => m.AnalyticsPage),
  },
];
