import { Routes } from '@angular/router';

export const ELECTIONS_ROUTES: Routes = [
  { path: '', redirectTo: 'list', pathMatch: 'full' },
  {
    path: 'list',
    loadComponent: () => import('./pages/election-list/election-list.page').then(m => m.ElectionListPage),
  },
];
