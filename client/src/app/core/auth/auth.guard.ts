import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth.store';
import { Role, ROLES } from './models/role.model';

export const authGuard: CanActivateFn = () => {
  const store = inject(AuthStore);
  const router = inject(Router);
  if (!store.isAuthenticated()) return router.createUrlTree(['/auth/login']);
  return true;
};

export function hasRoleGuard(roles: Role[]): CanActivateFn {
  return () => {
    const store = inject(AuthStore);
    const router = inject(Router);
    if (!store.isAuthenticated()) return router.createUrlTree(['/auth/login']);
    if (store.hasAnyRole(roles)) return true;
    // Members land on their default page instead of forbidden
    if (store.hasAnyRole([ROLES.MEMBER])) return router.createUrlTree(['/payments']);
    return router.createUrlTree(['/forbidden']);
  };
}
