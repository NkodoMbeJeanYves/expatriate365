import { Routes } from '@angular/router';
import { authGuard, hasRoleGuard } from '@core/auth/auth.guard';
import { STAFF_ROLES } from '@core/auth/models/role.model';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'auth',
    loadComponent: () => import('@layouts/auth-layout/auth-layout').then((m) => m.AuthLayoutComponent),
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      { path: 'login', loadComponent: () => import('@auth/pages/login/login.page').then((m) => m.LoginPageComponent) },
    ],
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('@layouts/main-layout/main-layout').then((m) => m.MainLayoutComponent),
    children: [
      { path: 'dashboard', canActivate: [hasRoleGuard(STAFF_ROLES)], loadComponent: () => import('@analytics/pages/dashboard/dashboard.page').then((m) => m.DashboardPageComponent) },
      { path: 'member-dashboard', loadComponent: () => import('./features/member-dashboard/member-dashboard.page').then((m) => m.MemberDashboardPage) },
      { path: 'members', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@members/members.routes').then((m) => m.MEMBERS_ROUTES) },
      { path: 'contributions', loadChildren: () => import('@contributions/contributions.routes').then((m) => m.CONTRIBUTIONS_ROUTES) },
      { path: 'payments', loadChildren: () => import('@payments/payments.routes').then((m) => m.PAYMENTS_ROUTES) },
      { path: 'finances', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@finances/finances.routes').then((m) => m.FINANCES_ROUTES) },
      { path: 'events', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@events/events.routes').then((m) => m.EVENTS_ROUTES) },
      { path: 'welfare', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@welfare/welfare.routes').then((m) => m.WELFARE_ROUTES) },
      { path: 'elections', loadChildren: () => import('@elections/elections.routes').then((m) => m.ELECTIONS_ROUTES) },
      { path: 'meetings', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@meetings/meetings.routes').then((m) => m.MEETINGS_ROUTES) },
      { path: 'communications', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@communications/communications.routes').then((m) => m.COMMUNICATIONS_ROUTES) },
      { path: 'documents', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@documents/documents.routes').then((m) => m.DOCUMENTS_ROUTES) },
      { path: 'analytics', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@analytics/analytics.routes').then((m) => m.ANALYTICS_ROUTES) },
      { path: 'governance', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@governance/governance.routes').then((m) => m.GOVERNANCE_ROUTES) },
      { path: 'admin', canActivate: [hasRoleGuard(STAFF_ROLES)], loadChildren: () => import('@admin/admin.routes').then((m) => m.ADMIN_ROUTES) },
    ],
  },
  { path: 'forbidden', loadComponent: () => import('@shared/components/forbidden/forbidden.component').then((m) => m.ForbiddenComponent) },
  { path: '**', redirectTo: 'dashboard' },
];
