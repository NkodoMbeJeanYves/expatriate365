import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import { FinanceSummaryDto, FinanceTransactionDto, FinanceTransactionFilters } from '@models/finance.model';

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
}
