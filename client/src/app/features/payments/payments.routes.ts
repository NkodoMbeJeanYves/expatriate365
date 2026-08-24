import { Routes } from '@angular/router';

export const PAYMENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/payment-list/payment-list.page').then(m => m.PaymentListPageComponent),
  },
];
