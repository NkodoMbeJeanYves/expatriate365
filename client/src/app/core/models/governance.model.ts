export interface BoardRoleDto {
  id: string;
  tenant_id: string;
  name: string;
  label: string;
  is_active: boolean;
  created_at: string;
  updated_at?: string;
}

export interface BoardMemberDto {
  id: string;
  tenant_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  role_id?: string;
  role_name?: string;
  role_label?: string;
  start_date: string;
  end_date?: string;
  notes?: string;
  created_at: string;
  updated_at?: string;
}

export interface ResolutionDto {
  id: string;
  tenant_id: string;
  title: string;
  content: string;
  status: string;
  meeting_id?: string;
  adopted_at?: string;
  votes_for: number;
  votes_against: number;
  abstentions: number;
  created_at: string;
  updated_at?: string;
}

export interface GovernanceStatsDto {
  total_board_members: number;
  total_resolutions: number;
  adopted_resolutions: number;
}

export interface CreateBoardRoleRequest {
  name: string;
  label: string;
}

export interface UpdateBoardRoleRequest {
  name: string;
  label: string;
  is_active: boolean;
}

export interface CreateBoardMemberRequest {
  member_id: string;
  role_id?: string;
  start_date: string;
  end_date?: string;
  notes?: string;
}

export interface CreateResolutionRequest {
  title: string;
  content: string;
  meeting_id?: string;
}

export interface AdoptResolutionRequest {
  adopted_at: string;
  votes_for: number;
  votes_against: number;
  abstentions: number;
}
