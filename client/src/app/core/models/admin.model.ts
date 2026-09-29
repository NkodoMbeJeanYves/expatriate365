export interface AdminUserDto {
  id: string;
  email: string;
  full_name: string;
  phone?: string;
  role: string;
  status: string;
  is_active: boolean;
  last_login_at?: string;
  created_at: string;
  updated_at?: string;
}

export interface AdminStatsDto {
  total_users: number;
  active_users: number;
  inactive_users: number;
}

export interface InviteUserRequest {
  email: string;
  first_name: string;
  last_name: string;
  phone?: string;
  role: string;
}

export interface ChangeRoleRequest {
  role: string;
}

export interface RoleDto {
  id: string;
  name: string;
  label: string;
  description?: string;
  permissions: string[];
  is_active: boolean;
  is_customized: boolean;
}

// Per-tenant role override — id is tenant_roles.id, not global role id
export interface TenantRoleDto {
  id: string;         // tenant_roles.id — used for update/reset calls
  role_id: string;    // global roles.id
  name: string;
  label: string;
  description?: string;
  permissions: string | string[];   // API returns JSON string; page normalises it
  is_customized: boolean;
  is_active: boolean;
}

export interface PermissionDomain {
  domain: string;
  permissions: string[];
}

export interface UpdateRolePermissionsRequest {
  permissions: string[];
}

// ── Super-admin tenant management ─────────────────────────────────────────────

export interface TenantSummaryDto {
  id: string;
  name: string;
  slug: string;
  country_code: string;
  base_currency: string;
  is_active: boolean;
  created_at: string;
  updated_at?: string;
  admin_email: string;
  admin_full_name: string;
  user_count: number;
}

export interface CreateTenantRequest {
  association_name: string;
  slug: string;
  admin_first_name: string;
  admin_last_name: string;
  admin_email: string;
  admin_password: string;
  phone?: string;
  country_code?: string;
  base_currency?: string;
}
