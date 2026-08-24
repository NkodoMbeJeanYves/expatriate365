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
