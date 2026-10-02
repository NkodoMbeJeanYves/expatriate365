import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AnalyticsOverviewDto, MemberAnalyticsDto,
  FinanceAnalyticsDto, EngagementAnalyticsDto,
} from '@models/analytics.model';

export interface DateRange { from?: string; to?: string; }

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/analytics`;

  overview(): Observable<AnalyticsOverviewDto> {
    return this.http.get<AnalyticsOverviewDto>(`${this.base}/overview`);
  }

  members(range?: DateRange): Observable<MemberAnalyticsDto> {
    return this.http.get<MemberAnalyticsDto>(`${this.base}/members`, { params: this.buildParams(range) });
  }

  finance(range?: DateRange): Observable<FinanceAnalyticsDto> {
    return this.http.get<FinanceAnalyticsDto>(`${this.base}/finance`, { params: this.buildParams(range) });
  }

  engagement(range?: DateRange): Observable<EngagementAnalyticsDto> {
    return this.http.get<EngagementAnalyticsDto>(`${this.base}/engagement`, { params: this.buildParams(range) });
  }

  private buildParams(range?: DateRange): HttpParams {
    let p = new HttpParams();
    if (range?.from) p = p.set('from', range.from);
    if (range?.to)   p = p.set('to', range.to);
    return p;
  }
}
