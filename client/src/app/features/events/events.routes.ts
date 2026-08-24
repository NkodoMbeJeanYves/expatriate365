import { Routes } from '@angular/router';

export const EVENTS_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full',
  },
  {
    path: 'list',
    loadComponent: () => import('./pages/event-list/event-list.page').then(m => m.EventListPage),
    data: { roles: ['school_admin', 'super_admin', 'director', 'registrar'] },
  },
];
