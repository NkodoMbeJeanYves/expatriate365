export const ROLES = {
  SUPER_ADMIN: 'super_admin',
  PRESIDENT: 'president',
  VICE_PRESIDENT: 'vice_president',
  TREASURER: 'treasurer',
  DEPUTY_TREASURER: 'deputy_treasurer',
  SECRETARY: 'secretary',
  DEPUTY_SECRETARY: 'deputy_secretary',
  AUDITOR: 'auditor',
  DEPUTY_AUDITOR: 'deputy_auditor',
  CENSOR: 'censor',
  COMMITTEE_MEMBER: 'committee_member',
  REGIONAL_COORDINATOR: 'regional_coordinator',
  EVENT_MANAGER: 'event_manager',
  MEMBER: 'member',
} as const;

export type Role = (typeof ROLES)[keyof typeof ROLES];

export const ADMIN_ROLES: Role[] = [ROLES.SUPER_ADMIN];

/** Roles allowed to create/update members and membership categories. */
export const MEMBER_ADMIN_ROLES: Role[] = [ROLES.SUPER_ADMIN, ROLES.SECRETARY];
export const BOARD_ROLES: Role[] = [ROLES.PRESIDENT, ROLES.TREASURER, ROLES.SECRETARY];
export const FINANCE_ROLES: Role[] = [ROLES.PRESIDENT, ROLES.TREASURER, ROLES.AUDITOR];
export const COMMITTEE_ROLES: Role[] = [...BOARD_ROLES, ROLES.COMMITTEE_MEMBER, ROLES.AUDITOR];

/** All roles that have staff/admin privileges — excludes the basic MEMBER role. */
export const STAFF_ROLES: Role[] = [
  ROLES.SUPER_ADMIN, ROLES.PRESIDENT, ROLES.TREASURER, ROLES.SECRETARY,
  ROLES.COMMITTEE_MEMBER, ROLES.AUDITOR, ROLES.REGIONAL_COORDINATOR, ROLES.EVENT_MANAGER,
];
