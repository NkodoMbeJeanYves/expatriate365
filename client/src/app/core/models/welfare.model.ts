import { PaginationMeta } from '@shared/models/pagination.model';

export interface WelfareRequest {
  id: string;
  tenant_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  type: string;
  description: string;
  amount_requested: number;
  amount_approved?: number;
  amount_paid?: number;
  status: 'pending' | 'approved' | 'rejected' | 'paid';
  rejection_reason?: string;
  notes?: string;
  reviewed_at?: string;
  paid_at?: string;
  created_at: string;
  updated_at?: string;
}

export interface WelfareStats {
  total_count: number;
  pending_count: number;
  approved_count: number;
  rejected_count: number;
  paid_count: number;
  total_requested: number;
  total_approved: number;
  total_paid: number;
}

export interface PagedWelfareResult {
  data: WelfareRequest[];
  pagination: PaginationMeta;
}

export const WELFARE_TYPES = [
  'death',
  'illness',
  'birth',
  'financial_hardship',
  'education',
  'other',
] as const;

export type WelfareType = typeof WELFARE_TYPES[number];

export interface CreateWelfareRequestRequest {
  member_id: string;
  type: string;
  description: string;
  amount_requested: number;
  notes?: string;
}

export interface ApproveWelfareRequestRequest {
  amount_approved: number;
  notes?: string;
}

export interface RejectWelfareRequestRequest {
  reason: string;
}
