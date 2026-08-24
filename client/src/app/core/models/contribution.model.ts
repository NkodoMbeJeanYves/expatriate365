import { PaginationMeta } from '@shared/models/pagination.model';

export interface ContributionType {
  id: string;
  tenant_id: string;
  name: string;
  description?: string;
  frequency: 'monthly' | 'quarterly' | 'annual' | 'one_time';
  base_amount: number;
  late_penalty_rate: number;
  grace_period_days: number;
  is_active: boolean;
  effective_from: string;
  effective_to?: string;
  created_at: string;
  updated_at?: string;
}

export interface ContributionCharge {
  id: string;
  tenant_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  contribution_type_id: string;
  contribution_type_name: string;
  due_date: string;
  base_amount: number;
  penalty_amount: number;
  waiver_amount: number;
  amount_paid: number;
  total_due: number;
  balance: number;
  status: 'pending' | 'paid' | 'overdue' | 'waived';
  is_active: boolean;
  created_at: string;
  updated_at?: string;
}

export interface ContributionStats {
  total_charges: number;
  paid_count: number;
  pending_count: number;
  overdue_count: number;
  waived_count: number;
  total_expected: number;
  total_collected: number;
  total_pending: number;
}

export interface PagedChargesResult {
  data: ContributionCharge[];
  pagination: PaginationMeta;
}

export interface CreateContributionTypeRequest {
  name: string;
  description?: string;
  frequency: string;
  base_amount: number;
  late_penalty_rate?: number;
  grace_period_days?: number;
  effective_from: string;
  effective_to?: string;
}

export interface UpdateContributionTypeRequest extends CreateContributionTypeRequest {
  is_active: boolean;
}

export interface CreateContributionChargeRequest {
  member_id: string;
  contribution_type_id: string;
  due_date: string;
  amount_override?: number;
}

export interface MarkChargePaidRequest {
  amount_paid: number;
}

export interface WaiveChargeRequest {
  waiver_amount?: number;
}

export interface BulkGenerateRequest {
  contribution_type_id: string;
  due_date: string;
}
