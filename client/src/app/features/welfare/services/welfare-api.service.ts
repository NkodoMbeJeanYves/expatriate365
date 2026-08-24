import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  ApproveWelfareRequestRequest,
  CreateWelfareRequestRequest,
  PagedWelfareResult,
  RejectWelfareRequestRequest,
  WelfareRequest,
  WelfareStats,
} from '@models/welfare.model';

@Injectable({ providedIn: 'root' })
export class WelfareApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/welfare-requests`;

  getRequests(page = 1, limit = 20, status?: string, type?: string): Observable<PagedWelfareResult> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (status) params = params.set('status', status);
    if (type) params = params.set('type', type);
    return this.http.get<PagedWelfareResult>(this.base, { params });
  }

  getStats(): Observable<WelfareStats> {
    return this.http.get<WelfareStats>(`${this.base}/stats`);
  }

  create(dto: CreateWelfareRequestRequest): Observable<WelfareRequest> {
    return this.http.post<WelfareRequest>(this.base, dto);
  }

  approve(id: string, dto: ApproveWelfareRequestRequest): Observable<WelfareRequest> {
    return this.http.post<WelfareRequest>(`${this.base}/${id}/approve`, dto);
  }

  reject(id: string, dto: RejectWelfareRequestRequest): Observable<WelfareRequest> {
    return this.http.post<WelfareRequest>(`${this.base}/${id}/reject`, dto);
  }

  markPaid(id: string): Observable<WelfareRequest> {
    return this.http.post<WelfareRequest>(`${this.base}/${id}/pay`, {});
  }
}
