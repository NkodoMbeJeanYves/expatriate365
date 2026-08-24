export const PERMISSIONS = {
  // Members
  MEMBERS_READ:            'members.read',
  MEMBERS_READ_OWN:        'members.read_own',
  MEMBERS_VIEW_CONTACT:    'members.view_contact',
  MEMBERS_CREATE:          'members.create',
  MEMBERS_UPDATE:          'members.update',
  MEMBERS_UPDATE_OWN:      'members.update_own',
  MEMBERS_DELETE:          'members.delete',
  MEMBERS_SEND_ACTIVATION: 'members.send_activation',
  MEMBERS_IMPORT:          'members.import',
  MEMBERS_EXPORT:          'members.export',

  // Categories
  CATEGORIES_READ:   'categories.read',
  CATEGORIES_CREATE: 'categories.create',
  CATEGORIES_UPDATE: 'categories.update',
  CATEGORIES_DELETE: 'categories.delete',

  // Contributions
  CONTRIBUTIONS_READ:     'contributions.read',
  CONTRIBUTIONS_READ_OWN: 'contributions.read_own',
  CONTRIBUTIONS_CREATE:   'contributions.create',
  CONTRIBUTIONS_UPDATE:   'contributions.update',
  CONTRIBUTIONS_DELETE:   'contributions.delete',
  CONTRIBUTIONS_VALIDATE: 'contributions.validate',
  CONTRIBUTIONS_EXPORT:   'contributions.export',
  CONTRIBUTIONS_IMPORT:   'contributions.import',

  // Payments
  PAYMENTS_READ:          'payments.read',
  PAYMENTS_READ_OWN:      'payments.read_own',
  PAYMENTS_CREATE:        'payments.create',
  PAYMENTS_UPDATE:        'payments.update',
  PAYMENTS_DELETE:        'payments.delete',
  PAYMENTS_VALIDATE:      'payments.validate',
  PAYMENTS_REFUND:        'payments.refund',
  PAYMENTS_EXPORT:        'payments.export',
  PAYMENTS_RECEIPT_PRINT: 'payments.receipt.print',

  // Votes
  VOTES_READ:    'votes.read',
  VOTES_CAST:    'votes.cast',
  VOTES_RESULTS: 'votes.results',
  VOTES_CREATE:  'votes.create',
  VOTES_MANAGE:  'votes.manage',
  VOTES_DELETE:  'votes.delete',
  VOTES_EXPORT:  'votes.export',

  // Events
  EVENTS_READ:             'events.read',
  EVENTS_REGISTER:         'events.register',
  EVENTS_CREATE:           'events.create',
  EVENTS_UPDATE:           'events.update',
  EVENTS_DELETE:           'events.delete',
  EVENTS_MANAGE_ATTENDEES: 'events.manage_attendees',
  EVENTS_EXPORT:           'events.export',

  // Documents
  DOCUMENTS_READ:    'documents.read',
  DOCUMENTS_UPLOAD:  'documents.upload',
  DOCUMENTS_MANAGE:  'documents.manage',
  DOCUMENTS_PUBLISH: 'documents.publish',

  // Announcements
  ANNOUNCEMENTS_READ:    'announcements.read',
  ANNOUNCEMENTS_CREATE:  'announcements.create',
  ANNOUNCEMENTS_UPDATE:  'announcements.update',
  ANNOUNCEMENTS_DELETE:  'announcements.delete',
  ANNOUNCEMENTS_PUBLISH: 'announcements.publish',

  // Notifications
  NOTIFICATIONS_READ_OWN: 'notifications.read_own',
  NOTIFICATIONS_SEND:     'notifications.send',
  NOTIFICATIONS_MANAGE:   'notifications.manage',

  // Dashboard & Reports
  DASHBOARD_READ:          'dashboard.read',
  DASHBOARD_FINANCIAL:     'dashboard.financial',
  DASHBOARD_MEMBERS:       'dashboard.members',
  REPORTS_READ:            'reports.read',
  REPORTS_FINANCIAL:       'reports.financial',
  REPORTS_MEMBERS:         'reports.members',
  REPORTS_CONTRIBUTIONS:   'reports.contributions',
  REPORTS_EXPORT:          'reports.export',
  AUDIT_READ:              'audit.read',

  // Settings
  SETTINGS_READ:          'settings.read',
  SETTINGS_UPDATE:        'settings.update',
  SETTINGS_BILLING:       'settings.billing',
  SETTINGS_NOTIFICATIONS: 'settings.notifications',
  SETTINGS_INTEGRATIONS:  'settings.integrations',

  // Roles
  ROLES_READ:   'roles.read',
  ROLES_CREATE: 'roles.create',
  ROLES_UPDATE: 'roles.update',
  ROLES_DELETE: 'roles.delete',
  ROLES_ASSIGN: 'roles.assign',

  // Users
  USERS_READ:           'users.read',
  USERS_CREATE:         'users.create',
  USERS_UPDATE:         'users.update',
  USERS_DELETE:         'users.delete',
  USERS_RESET_PASSWORD: 'users.reset_password',
  USERS_IMPERSONATE:    'users.impersonate',
} as const;

export type Permission = typeof PERMISSIONS[keyof typeof PERMISSIONS];
