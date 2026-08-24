import { Routes } from '@angular/router';

export const WELFARE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/welfare-list/welfare-list.page').then(m => m.WelfareListPageComponent),
  },
];
