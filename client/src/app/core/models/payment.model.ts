import { PaginationMeta } from '@shared/models/pagination.model';

export interface Payment {
  id: string;
  tenant_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  charge_id: string;
  contribution_type_name: string;
  receipt_number: string;
  amount: number;
  currency: string;
  payment_method: string;
  notes?: string;
  receipt_file_url?: string;
  status: 'pending' | 'confirmed' | 'reversed';
  payment_date: string;
  confirmed_at?: string;
  reversed_at?: string;
  reversal_reason?: string;
  created_at: string;
  updated_at?: string;
}

export interface PaymentStats {
  total_count: number;
  confirmed_count: number;
  pending_count: number;
  reversed_count: number;
  total_confirmed: number;
  total_pending: number;
}

export interface PagedPaymentsResult {
  data: Payment[];
  pagination: PaginationMeta;
}

export const PAYMENT_METHODS = [
  { label: 'Espèces', value: 'cash' },
  { label: 'Virement bancaire', value: 'bank_transfer' },
  { label: 'Mobile Money', value: 'mobile_money' },
  { label: 'Carte bancaire', value: 'card' },
  { label: 'Chèque', value: 'cheque' },
] as const;

export type PaymentMethod = 'cash' | 'bank_transfer' | 'mobile_money' | 'card' | 'cheque';

export interface RecordPaymentRequest {
  charge_id: string;
  amount: number;
  payment_method: string;
  payment_date: string;
  notes?: string;
}

export interface ReversePaymentRequest {
  reason: string;
}
