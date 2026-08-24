import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  AnalyticsOverviewDto, MemberAnalyticsDto,
  FinanceAnalyticsDto, EngagementAnalyticsDto,
} from '@models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/analytics`;

  overview(): Observable<AnalyticsOverviewDto> {
    return this.http.get<AnalyticsOverviewDto>(`${this.base}/overview`);
  }

  members(): Observable<MemberAnalyticsDto> {
    return this.http.get<MemberAnalyticsDto>(`${this.base}/members`);
  }

  finance(): Observable<FinanceAnalyticsDto> {
    return this.http.get<FinanceAnalyticsDto>(`${this.base}/finance`);
  }

  engagement(): Observable<EngagementAnalyticsDto> {
    return this.http.get<EngagementAnalyticsDto>(`${this.base}/engagement`);
  }
}
