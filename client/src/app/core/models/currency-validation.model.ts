export interface CurrencyValidationResponse {
  country_code: string;
  base_currency: string;
  is_consistent: boolean;
  allowed_currencies: string[];
  coverage_status: 'covered' | 'not_covered' | 'unavailable' | 'not_checked';
  source: string;
  checked_at: string | null;
}
