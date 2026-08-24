export interface ElectionDto {
  id: string;
  tenant_id: string;
  title: string;
  description?: string;
  type: string;
  status: string;
  start_date?: string;
  end_date?: string;
  max_choices: number;
  candidate_count: number;
  vote_count: number;
  created_at: string;
  updated_at?: string;
  has_voted: boolean;
}

export interface ElectionCandidateDto {
  id: string;
  election_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  statement?: string;
  display_order: number;
  vote_count: number;
  rank: number;
}

export interface ElectionResultDto {
  candidate_id: string;
  member_name: string;
  membership_number: string;
  vote_count: number;
  rank: number;
  percentage: number;
}

export interface ElectionStatsDto {
  total: number;
  draft: number;
  open: number;
  closed: number;
  results_published: number;
}

export interface CreateElectionRequest {
  title: string;
  description?: string;
  type: string;
  start_date?: string;
  end_date?: string;
  max_choices: number;
}

export interface UpdateElectionRequest extends CreateElectionRequest {}

export interface AddCandidateRequest {
  member_id: string;
  statement?: string;
  display_order: number;
}

export interface CastVoteRequest {
  candidate_ids: string[];
}

export interface ElectionFilters {
  page: number;
  limit: number;
  status?: string;
  type?: string;
}

export const ELECTION_TYPES = ['board', 'president', 'custom'] as const;
export const ELECTION_STATUSES = ['draft', 'open', 'closed', 'results_published'] as const;
