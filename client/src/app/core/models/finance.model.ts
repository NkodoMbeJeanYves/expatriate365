export interface FinanceSummaryDto {
  total_collected: number;
  total_expected: number;
  balance: number;
  collection_rate: number;
  total_transactions: number;
}

export interface FinanceTransactionDto {
  id: string;
  type: string;
  member_name: string;
  membership_number: string;
  amount: number;
  currency: string;
  status: string;
  date: string;
  description?: string;
}

export interface ExpenseDto {
  id: string;
  label: string;
  description?: string;
  category: string;
  amount: number;
  currency: string;
  date: string;
  status: string;
  validated_by?: string;
  validated_at?: string;
  created_at: string;
}

export interface ExpenseStatsDto {
  total_amount: number;
  total_count: number;
  pending_count: number;
  validated_count: number;
  rejected_count: number;
}

export interface CreateExpenseRequest {
  label: string;
  description?: string;
  category: string;
  amount: number;
  currency: string;
  date: string;
}

export interface FinanceTransactionFilters {
  page: number;
  limit: number;
  type?: string;
  status?: string;
  from?: string;
  to?: string;
}
