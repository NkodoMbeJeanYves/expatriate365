import { Routes } from '@angular/router';

export const COMMUNICATIONS_ROUTES: Routes = [
  { path: '', redirectTo: 'list', pathMatch: 'full' },
  {
    path: 'list',
    loadComponent: () =>
      import('./pages/communication-list/communication-list.page').then(m => m.CommunicationListPage),
  },
];
