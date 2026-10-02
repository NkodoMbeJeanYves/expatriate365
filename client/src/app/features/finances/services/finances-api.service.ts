import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import { FinanceSummaryDto, FinanceTransactionDto, FinanceTransactionFilters, ExpenseDto, ExpenseStatsDto, CreateExpenseRequest } from '@models/finance.model';

@Injectable({ providedIn: 'root' })
export class FinancesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/finances`;

  summary(): Observable<FinanceSummaryDto> {
    return this.http.get<FinanceSummaryDto>(`${this.base}/summary`);
  }

  transactions(f: FinanceTransactionFilters): Observable<PagedResult<FinanceTransactionDto>> {
    const params: Record<string, string | number> = { page: f.page, limit: f.limit };
    if (f.type) params['type'] = f.type;
    if (f.status) params['status'] = f.status;
    if (f.from) params['from'] = f.from;
    if (f.to) params['to'] = f.to;
    return this.http.get<PagedResult<FinanceTransactionDto>>(`${this.base}/transactions`, { params });
  }

  // --- Expenses ---
  private readonly expBase = `${environment.apiUrl}/api/v1/expenses`;

  listExpenses(page = 1, limit = 20, status?: string, category?: string): Observable<PagedResult<ExpenseDto>> {
    const params: Record<string, string | number> = { page, limit };
    if (status) params['status'] = status;
    if (category) params['category'] = category;
    return this.http.get<PagedResult<ExpenseDto>>(this.expBase, { params });
  }

  expenseStats(): Observable<ExpenseStatsDto> {
    return this.http.get<ExpenseStatsDto>(`${this.expBase}/stats`);
  }

  createExpense(dto: CreateExpenseRequest): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>(this.expBase, dto);
  }

  updateExpense(id: string, dto: Partial<CreateExpenseRequest>): Observable<ExpenseDto> {
    return this.http.put<ExpenseDto>(`${this.expBase}/${id}`, dto);
  }

  validateExpense(id: string): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>(`${this.expBase}/${id}/validate`, {});
  }

  rejectExpense(id: string): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>(`${this.expBase}/${id}/reject`, {});
  }

  deleteExpense(id: string): Observable<void> {
    return this.http.delete<void>(`${this.expBase}/${id}`);
  }
}
