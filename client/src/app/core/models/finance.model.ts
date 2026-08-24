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

export interface FinanceTransactionFilters {
  page: number;
  limit: number;
  type?: string;
  status?: string;
  from?: string;
  to?: string;
}
