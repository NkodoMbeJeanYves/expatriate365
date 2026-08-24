import { Routes } from '@angular/router';

export const CONTRIBUTIONS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/contribution-list/contribution-list.page').then(m => m.ContributionListPageComponent),
  },
];
