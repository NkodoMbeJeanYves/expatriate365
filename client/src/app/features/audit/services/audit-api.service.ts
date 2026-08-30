import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { AuditLogDto, TenantStatsDto, AnomalyDto } from '@models/audit.model';
import { PagedResult } from '@shared/models/pagination.model';

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/audit`;

  getLogs(filters: {
    page?: number; limit?: number;
    tenant_id?: string; user_id?: string;
    action?: string; from?: string; to?: string;
  } = {}): Observable<PagedResult<AuditLogDto>> {
    let params = new HttpParams()
      .set('page',  filters.page  ?? 1)
      .set('limit', filters.limit ?? 20);
    if (filters.tenant_id) params = params.set('tenant_id', filters.tenant_id);
    if (filters.user_id)   params = params.set('user_id',   filters.user_id);
    if (filters.action)    params = params.set('action',    filters.action);
    if (filters.from)      params = params.set('from',      filters.from);
    if (filters.to)        params = params.set('to',        filters.to);
    return this.http.get<PagedResult<AuditLogDto>>(`${this.base}/logs`, { params });
  }

  getTenants(): Observable<TenantStatsDto[]> {
    return this.http.get<TenantStatsDto[]>(`${this.base}/tenants`);
  }

  getAnomalies(): Observable<AnomalyDto[]> {
    return this.http.get<AnomalyDto[]>(`${this.base}/anomalies`);
  }
}
