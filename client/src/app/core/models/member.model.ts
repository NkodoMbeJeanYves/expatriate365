export interface Member {
  id: string;
  tenant_id: string;
  user_id: string;
  membership_number: string;
  first_name: string;
  last_name: string;
  email: string;
  phone?: string;
  status: MemberStatus;
  category_id?: string;
  category_name?: string;
  joined_date: string;
  expiry_date?: string;
  photo_url?: string;
  address?: string;
  profession?: string;
  date_of_birth?: string;
  gender?: string;
  emergency_contact_name?: string;
  emergency_contact_phone?: string;
  is_active: boolean;
  created_at: string;
  updated_at?: string;
  email_verified_at?: string;
  role: string;
}

export interface MemberListItem {
  id: string;
  membership_number: string;
  first_name: string;
  last_name: string;
  email: string;
  phone?: string;
  status: MemberStatus;
  category_id?: string;
  category_name?: string;
  joined_date: string;
  photo_url?: string;
  is_active: boolean;
}

export type MemberStatus = 'active' | 'suspended' | 'inactive' | 'pending';

export interface MembershipCategory {
  id: string;
  name: string;
  description?: string;
  contribution_rate: number;
  voting_rights: boolean;
  welfare_eligible: boolean;
  is_active: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
  contribution_rate: number;
  voting_rights: boolean;
  welfare_eligible: boolean;
}

export interface CreateMemberRequest {
  first_name: string;
  last_name: string;
  email?: string;
  phone?: string;
  category_id?: string;
  joined_date: string;
  expiry_date?: string;
  photo_url?: string;
  address?: string;
  profession?: string;
  date_of_birth?: string;
  gender?: string;
  emergency_contact_name?: string;
  emergency_contact_phone?: string;
}

export interface UpdateMemberRequest extends CreateMemberRequest {
  is_active: boolean;
}

export interface PagedMembersResult {
  data: MemberListItem[];
  pagination: { page: number; limit: number; total: number };
}

export interface MemberFilters {
  search?: string;
  status?: MemberStatus | '';
  category_id?: string;
  page: number;
  limit: number;
}
