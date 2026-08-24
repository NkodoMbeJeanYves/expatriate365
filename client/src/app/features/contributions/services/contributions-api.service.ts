import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  BulkGenerateRequest,
  ContributionStats,
  ContributionType,
  CreateContributionChargeRequest,
  CreateContributionTypeRequest,
  MarkChargePaidRequest,
  PagedChargesResult,
  UpdateContributionTypeRequest,
  WaiveChargeRequest,
  ContributionCharge,
} from '@models/contribution.model';

@Injectable({ providedIn: 'root' })
export class ContributionsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1`;

  getTypes(isActive?: boolean): Observable<ContributionType[]> {
    let params = new HttpParams();
    if (isActive !== undefined) params = params.set('is_active', String(isActive));
    return this.http.get<ContributionType[]>(`${this.base}/contribution-types`, { params });
  }

  createType(dto: CreateContributionTypeRequest): Observable<ContributionType> {
    return this.http.post<ContributionType>(`${this.base}/contribution-types`, dto);
  }

  updateType(id: string, dto: UpdateContributionTypeRequest): Observable<ContributionType> {
    return this.http.put<ContributionType>(`${this.base}/contribution-types/${id}`, dto);
  }

  getCharges(page = 1, limit = 20, memberId?: string, typeId?: string, status?: string): Observable<PagedChargesResult> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (memberId) params = params.set('member_id', memberId);
    if (typeId) params = params.set('type_id', typeId);
    if (status) params = params.set('status', status);
    return this.http.get<PagedChargesResult>(`${this.base}/contribution-charges`, { params });
  }

  getStats(): Observable<ContributionStats> {
    return this.http.get<ContributionStats>(`${this.base}/contribution-charges/stats`);
  }

  getChargeById(id: string): Observable<ContributionCharge> {
    return this.http.get<ContributionCharge>(`${this.base}/contribution-charges/${id}`);
  }

  createCharge(dto: CreateContributionChargeRequest): Observable<ContributionCharge> {
    return this.http.post<ContributionCharge>(`${this.base}/contribution-charges`, dto);
  }

  markPaid(id: string, dto: MarkChargePaidRequest): Observable<ContributionCharge> {
    return this.http.post<ContributionCharge>(`${this.base}/contribution-charges/${id}/pay`, dto);
  }

  waive(id: string, dto: WaiveChargeRequest): Observable<ContributionCharge> {
    return this.http.post<ContributionCharge>(`${this.base}/contribution-charges/${id}/waive`, dto);
  }

  bulkGenerate(dto: BulkGenerateRequest): Observable<{ generated: number }> {
    return this.http.post<{ generated: number }>(`${this.base}/contribution-charges/bulk-generate`, dto);
  }
}
