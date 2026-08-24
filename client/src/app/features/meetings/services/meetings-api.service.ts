import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  MeetingDto, MeetingFilters, MeetingAttendanceDto, MeetingMinuteDto, MeetingStatsDto,
  CreateMeetingRequest, UpdateMeetingRequest, RecordAttendanceRequest, SaveMinutesRequest,
} from '@models/meeting.model';

@Injectable({ providedIn: 'root' })
export class MeetingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/meetings`;

  list(filters: MeetingFilters): Observable<PagedResult<MeetingDto>> {
    const params: Record<string, string | number> = { page: filters.page, limit: filters.limit };
    if (filters.status) params['status'] = filters.status;
    if (filters.type) params['type'] = filters.type;
    return this.http.get<PagedResult<MeetingDto>>(this.base, { params });
  }

  getById(id: string): Observable<{ meeting: MeetingDto; attendances: MeetingAttendanceDto[]; minute: MeetingMinuteDto | null }> {
    return this.http.get<{ meeting: MeetingDto; attendances: MeetingAttendanceDto[]; minute: MeetingMinuteDto | null }>(`${this.base}/${id}`);
  }

  stats(): Observable<MeetingStatsDto> {
    return this.http.get<MeetingStatsDto>(`${this.base}/stats`);
  }

  create(dto: CreateMeetingRequest): Observable<MeetingDto> {
    return this.http.post<MeetingDto>(this.base, dto);
  }

  update(id: string, dto: UpdateMeetingRequest): Observable<MeetingDto> {
    return this.http.put<MeetingDto>(`${this.base}/${id}`, dto);
  }

  start(id: string): Observable<MeetingDto> {
    return this.http.post<MeetingDto>(`${this.base}/${id}/start`, {});
  }

  close(id: string): Observable<MeetingDto> {
    return this.http.post<MeetingDto>(`${this.base}/${id}/close`, {});
  }

  cancel(id: string): Observable<MeetingDto> {
    return this.http.post<MeetingDto>(`${this.base}/${id}/cancel`, {});
  }

  recordAttendance(id: string, dto: RecordAttendanceRequest): Observable<{ updated: number }> {
    return this.http.post<{ updated: number }>(`${this.base}/${id}/attendance`, dto);
  }

  saveMinutes(id: string, dto: SaveMinutesRequest): Observable<MeetingMinuteDto> {
    return this.http.put<MeetingMinuteDto>(`${this.base}/${id}/minutes`, dto);
  }

  approveMinutes(id: string): Observable<MeetingMinuteDto> {
    return this.http.post<MeetingMinuteDto>(`${this.base}/${id}/minutes/approve`, {});
  }
}
