import { Routes } from '@angular/router';

export const MEETINGS_ROUTES: Routes = [
  { path: '', redirectTo: 'list', pathMatch: 'full' },
  {
    path: 'list',
    loadComponent: () => import('./pages/meeting-list/meeting-list.page').then(m => m.MeetingListPage),
  },
];
