import { MEMBER_ADMIN_ROLES, Role, ROLES, STAFF_ROLES } from '@core/auth/models/role.model';

export interface NavItem {
  labelKey: string;
  icon: string;
  route: string;
  roles?: Role[];
}

export interface NavGroup {
  titleKey: string;
  items: NavItem[];
}

export const NAV_GROUPS: NavGroup[] = [
  {
    titleKey: '',
    items: [
      { labelKey: 'nav.dashboard', icon: 'pi pi-home', route: '/dashboard', roles: STAFF_ROLES },
      { labelKey: 'nav.member_dashboard', icon: 'pi pi-home', route: '/member-dashboard', roles: [ROLES.MEMBER] },
    ],
  },
  {
    titleKey: 'Communauté',
    items: [
      { labelKey: 'nav.members', icon: 'pi pi-users', route: '/members', roles: STAFF_ROLES },
      { labelKey: 'nav.member_categories', icon: 'pi pi-tags', route: '/members/categories', roles: MEMBER_ADMIN_ROLES },
      { labelKey: 'nav.contributions', icon: 'pi pi-credit-card', route: '/contributions' },
      { labelKey: 'nav.payments', icon: 'pi pi-wallet', route: '/payments' },
      { labelKey: 'nav.welfare', icon: 'pi pi-heart', route: '/welfare', roles: STAFF_ROLES },
    ],
  },
  {
    titleKey: 'Finances',
    items: [
      { labelKey: 'nav.finances', icon: 'pi pi-chart-bar', route: '/finances', roles: [ROLES.PRESIDENT, ROLES.TREASURER, ROLES.AUDITOR] },
      { labelKey: 'nav.analytics', icon: 'pi pi-chart-line', route: '/analytics', roles: [ROLES.PRESIDENT, ROLES.TREASURER] },
    ],
  },
  {
    titleKey: 'Activités',
    items: [
      { labelKey: 'nav.events', icon: 'pi pi-calendar', route: '/events', roles: STAFF_ROLES },
      { labelKey: 'nav.meetings', icon: 'pi pi-microphone', route: '/meetings', roles: [ROLES.PRESIDENT, ROLES.SECRETARY, ROLES.COMMITTEE_MEMBER] },
      { labelKey: 'nav.elections', icon: 'pi pi-check-square', route: '/elections' },
      { labelKey: 'nav.community', icon: 'pi pi-comments', route: '/community' },
      { labelKey: 'nav.community_moderation', icon: 'pi pi-shield', route: '/community/moderation', roles: STAFF_ROLES },
    ],
  },
  {
    titleKey: 'Gestion',
    items: [
      { labelKey: 'nav.communications', icon: 'pi pi-envelope', route: '/communications', roles: [ROLES.PRESIDENT, ROLES.SECRETARY] },
      { labelKey: 'nav.documents', icon: 'pi pi-file', route: '/documents', roles: STAFF_ROLES },
      { labelKey: 'nav.governance', icon: 'pi pi-shield', route: '/governance', roles: [ROLES.PRESIDENT, ROLES.SECRETARY, ROLES.COMMITTEE_MEMBER] },
      { labelKey: 'nav.admin', icon: 'pi pi-users', route: '/admin', roles: [ROLES.SUPER_ADMIN] },
      { labelKey: 'nav.audit', icon: 'pi pi-history', route: '/admin/audit', roles: [ROLES.SUPER_ADMIN] },
      { labelKey: 'nav.roles', icon: 'pi pi-shield', route: '/admin/roles', roles: [ROLES.SUPER_ADMIN] },
      { labelKey: 'nav.settings', icon: 'pi pi-cog', route: '/admin/settings', roles: [ROLES.PRESIDENT, ROLES.SUPER_ADMIN] },
    ],
  },
];
