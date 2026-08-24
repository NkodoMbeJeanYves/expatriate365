import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  BoardMemberDto, ResolutionDto, GovernanceStatsDto,
  CreateBoardMemberRequest, CreateResolutionRequest, AdoptResolutionRequest,
} from '@models/governance.model';

@Injectable({ providedIn: 'root' })
export class GovernanceApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/governance`;

  stats(): Observable<GovernanceStatsDto> {
    return this.http.get<GovernanceStatsDto>(`${this.base}/stats`);
  }

  listBoard(): Observable<BoardMemberDto[]> {
    return this.http.get<BoardMemberDto[]>(`${this.base}/board`);
  }

  addBoardMember(dto: CreateBoardMemberRequest): Observable<BoardMemberDto> {
    return this.http.post<BoardMemberDto>(`${this.base}/board`, dto);
  }

  removeBoardMember(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/board/${id}`);
  }

  listResolutions(page = 1, limit = 20, status?: string): Observable<PagedResult<ResolutionDto>> {
    const params: Record<string, string | number> = { page, limit };
    if (status) params['status'] = status;
    return this.http.get<PagedResult<ResolutionDto>>(`${this.base}/resolutions`, { params });
  }

  createResolution(dto: CreateResolutionRequest): Observable<ResolutionDto> {
    return this.http.post<ResolutionDto>(`${this.base}/resolutions`, dto);
  }

  adoptResolution(id: string, dto: AdoptResolutionRequest): Observable<ResolutionDto> {
    return this.http.post<ResolutionDto>(`${this.base}/resolutions/${id}/adopt`, dto);
  }

  deleteResolution(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/resolutions/${id}`);
  }
}
