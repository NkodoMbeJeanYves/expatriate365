import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config.token';
import { PagedResult } from '@core/api/api-types';
import {
  CreateMemberRequest, Member, MemberFilters, MemberListItem,
  MembershipCategory, UpdateMemberRequest,
} from '@core/models/member.model';

@Injectable({ providedIn: 'root' })
export class MembersApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  private get base(): string {
    return `${this.config.apiUrl}/api/v1/members`;
  }

  list(filters: MemberFilters): Observable<PagedResult<MemberListItem>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('limit', filters.limit);
    if (filters.search) params = params.set('search', filters.search);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.category_id) params = params.set('category_id', filters.category_id);
    return this.http.get<PagedResult<MemberListItem>>(this.base, { params });
  }

  getById(id: string): Observable<Member> {
    return this.http.get<Member>(`${this.base}/${id}`);
  }

  create(body: CreateMemberRequest): Observable<Member> {
    return this.http.post<Member>(this.base, body);
  }

  update(id: string, body: UpdateMemberRequest): Observable<Member> {
    return this.http.put<Member>(`${this.base}/${id}`, body);
  }

  patchStatus(id: string, status: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, { status });
  }

  categories(): Observable<MembershipCategory[]> {
    return this.http.get<MembershipCategory[]>(`${this.base}/categories`);
  }

  exportCsv(status?: string): void {
    let url = `${this.base}/export`;
    if (status) url += `?status=${status}`;
    const a = document.createElement('a');
    a.href = url;
    a.download = `membres_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
  }
}
