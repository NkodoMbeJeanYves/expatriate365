import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  CommunicationDto, CommunicationFilters, CommunicationStatsDto,
  CreateCommunicationRequest, UpdateCommunicationRequest, RecipientDto,
} from '@models/communication.model';

@Injectable({ providedIn: 'root' })
export class CommunicationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/communications`;

  list(filters: CommunicationFilters): Observable<PagedResult<CommunicationDto>> {
    const params: Record<string, string | number> = { page: filters.page, limit: filters.limit };
    if (filters.status) params['status'] = filters.status;
    if (filters.type) params['type'] = filters.type;
    if (filters.channel) params['channel'] = filters.channel;
    return this.http.get<PagedResult<CommunicationDto>>(this.base, { params });
  }

  getById(id: string): Observable<{ communication: CommunicationDto; recipients: RecipientDto[] }> {
    return this.http.get<{ communication: CommunicationDto; recipients: RecipientDto[] }>(`${this.base}/${id}`);
  }

  stats(): Observable<CommunicationStatsDto> {
    return this.http.get<CommunicationStatsDto>(`${this.base}/stats`);
  }

  create(dto: CreateCommunicationRequest): Observable<CommunicationDto> {
    return this.http.post<CommunicationDto>(this.base, dto);
  }

  update(id: string, dto: UpdateCommunicationRequest): Observable<CommunicationDto> {
    return this.http.put<CommunicationDto>(`${this.base}/${id}`, dto);
  }

  send(id: string): Observable<CommunicationDto> {
    return this.http.post<CommunicationDto>(`${this.base}/${id}/send`, {});
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/read`, {});
  }
}
