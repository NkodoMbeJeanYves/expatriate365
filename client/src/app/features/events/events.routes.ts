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
    data: { roles: ['super_admin', 'org_admin', 'president', 'vice_president', 'secretary', 'event_manager', 'member'] },
  },
  {
    path: ':id',
    loadComponent: () => import('./pages/event-detail/event-detail.page').then(m => m.EventDetailPage),
    data: { roles: ['super_admin', 'org_admin', 'president', 'vice_president', 'secretary', 'event_manager', 'member'] },
  },
];
