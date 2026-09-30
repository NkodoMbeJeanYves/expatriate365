import { computed, inject, Injectable } from '@angular/core';
import { AuthStore } from '@core/auth/auth.store';
import { ROLES } from '@core/auth/models/role.model';

export type RoleFamily = 'super_admin' | 'org_admin' | 'staff' | 'member';

export interface OnboardingStep {
  icon: string;
  titleKey: string;
  descKey: string;
  route: string;
  done?: boolean;
}

const STORAGE_PREFIX = 'exp365_onboarding_seen_';

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly auth = inject(AuthStore);

  readonly roleFamily = computed<RoleFamily>(() => {
    const roles = this.auth.user()?.roles ?? [];
    if (roles.includes(ROLES.SUPER_ADMIN)) return 'super_admin';
    if (roles.includes(ROLES.ORG_ADMIN))   return 'org_admin';
    if (roles.includes(ROLES.MEMBER) && roles.length === 1) return 'member';
    return 'staff';
  });

  readonly steps = computed<OnboardingStep[]>(() => STEPS[this.roleFamily()]);

  isSeen(): boolean {
    const userId = this.auth.user()?.id;
    if (!userId) return true;
    return localStorage.getItem(STORAGE_PREFIX + userId) === '1';
  }

  markSeen(): void {
    const userId = this.auth.user()?.id;
    if (userId) localStorage.setItem(STORAGE_PREFIX + userId, '1');
  }
}

const STEPS: Record<RoleFamily, OnboardingStep[]> = {
  super_admin: [
    { icon: 'pi pi-building', titleKey: 'onboarding.super_admin.step1_title', descKey: 'onboarding.super_admin.step1_desc', route: '/admin/tenants' },
    { icon: 'pi pi-sign-in',  titleKey: 'onboarding.super_admin.step2_title', descKey: 'onboarding.super_admin.step2_desc', route: '/admin/tenants' },
    { icon: 'pi pi-chart-bar',titleKey: 'onboarding.super_admin.step3_title', descKey: 'onboarding.super_admin.step3_desc', route: '/admin/tenants' },
  ],
  org_admin: [
    { icon: 'pi pi-cog',   titleKey: 'onboarding.org_admin.step1_title', descKey: 'onboarding.org_admin.step1_desc', route: '/admin/settings' },
    { icon: 'pi pi-users', titleKey: 'onboarding.org_admin.step2_title', descKey: 'onboarding.org_admin.step2_desc', route: '/admin/users' },
    { icon: 'pi pi-id-card',titleKey: 'onboarding.org_admin.step3_title', descKey: 'onboarding.org_admin.step3_desc', route: '/members' },
  ],
  staff: [
    { icon: 'pi pi-home',      titleKey: 'onboarding.staff.step1_title', descKey: 'onboarding.staff.step1_desc', route: '/dashboard' },
    { icon: 'pi pi-calendar',  titleKey: 'onboarding.staff.step2_title', descKey: 'onboarding.staff.step2_desc', route: '/events' },
    { icon: 'pi pi-users',     titleKey: 'onboarding.staff.step3_title', descKey: 'onboarding.staff.step3_desc', route: '/members' },
  ],
  member: [
    { icon: 'pi pi-user-edit', titleKey: 'onboarding.member.step1_title', descKey: 'onboarding.member.step1_desc', route: '/profile' },
    { icon: 'pi pi-wallet',    titleKey: 'onboarding.member.step2_title', descKey: 'onboarding.member.step2_desc', route: '/contributions' },
    { icon: 'pi pi-calendar',  titleKey: 'onboarding.member.step3_title', descKey: 'onboarding.member.step3_desc', route: '/events' },
  ],
};
