import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  ElectionDto, ElectionFilters, ElectionCandidateDto, ElectionResultDto,
  ElectionStatsDto, CreateElectionRequest, UpdateElectionRequest,
  AddCandidateRequest, CastVoteRequest,
} from '@models/election.model';

@Injectable({ providedIn: 'root' })
export class ElectionsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/elections`;

  list(filters: ElectionFilters): Observable<PagedResult<ElectionDto>> {
    const params: Record<string, string | number> = { page: filters.page, limit: filters.limit };
    if (filters.status) params['status'] = filters.status;
    if (filters.type) params['type'] = filters.type;
    return this.http.get<PagedResult<ElectionDto>>(this.base, { params });
  }

  getById(id: string): Observable<{ election: ElectionDto; candidates: ElectionCandidateDto[]; results: ElectionResultDto[] }> {
    return this.http.get<{ election: ElectionDto; candidates: ElectionCandidateDto[]; results: ElectionResultDto[] }>(`${this.base}/${id}`);
  }

  stats(): Observable<ElectionStatsDto> {
    return this.http.get<ElectionStatsDto>(`${this.base}/stats`);
  }

  create(dto: CreateElectionRequest): Observable<ElectionDto> {
    return this.http.post<ElectionDto>(this.base, dto);
  }

  update(id: string, dto: UpdateElectionRequest): Observable<ElectionDto> {
    return this.http.put<ElectionDto>(`${this.base}/${id}`, dto);
  }

  open(id: string): Observable<ElectionDto> {
    return this.http.post<ElectionDto>(`${this.base}/${id}/open`, {});
  }

  close(id: string): Observable<ElectionDto> {
    return this.http.post<ElectionDto>(`${this.base}/${id}/close`, {});
  }

  publishResults(id: string): Observable<ElectionDto> {
    return this.http.post<ElectionDto>(`${this.base}/${id}/publish-results`, {});
  }

  addCandidate(id: string, dto: AddCandidateRequest): Observable<ElectionCandidateDto> {
    return this.http.post<ElectionCandidateDto>(`${this.base}/${id}/candidates`, dto);
  }

  removeCandidate(electionId: string, candidateId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${electionId}/candidates/${candidateId}`);
  }

  castVote(id: string, dto: CastVoteRequest): Observable<{ voted: boolean }> {
    return this.http.post<{ voted: boolean }>(`${this.base}/${id}/vote`, dto);
  }
}
