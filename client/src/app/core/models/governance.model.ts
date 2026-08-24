export interface BoardMemberDto {
  id: string;
  tenant_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  role: string;
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

export interface CreateBoardMemberRequest {
  member_id: string;
  role: string;
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

export const BOARD_ROLES = [
  'Président(e)', 'Vice-Président(e)', 'Secrétaire', 'Trésorier(ère)',
  'Secrétaire adjoint(e)', 'Trésorier(ère) adjoint(e)', 'Commissaire aux comptes',
  'Membre du bureau',
] as const;
