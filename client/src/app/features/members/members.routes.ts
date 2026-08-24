import { Routes } from '@angular/router';

export const MEMBERS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/member-list/member-list.page').then((m) => m.MemberListPageComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/member-detail/member-detail.page').then((m) => m.MemberDetailPageComponent),
  },
];
