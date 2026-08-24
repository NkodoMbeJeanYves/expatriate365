import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  EventDto, EventFilters, EventRegistrationDto, EventStatsDto,
  CreateEventRequest, UpdateEventRequest, RegisterToEventRequest, MarkAttendanceRequest,
} from '@models/event.model';

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/events`;

  list(filters: EventFilters): Observable<PagedResult<EventDto>> {
    const params: Record<string, string | number> = { page: filters.page, limit: filters.limit };
    if (filters.status) params['status'] = filters.status;
    if (filters.type) params['type'] = filters.type;
    return this.http.get<PagedResult<EventDto>>(this.base, { params });
  }

  getById(id: string): Observable<{ event: EventDto; registrations: EventRegistrationDto[] }> {
    return this.http.get<{ event: EventDto; registrations: EventRegistrationDto[] }>(`${this.base}/${id}`);
  }

  stats(): Observable<EventStatsDto> {
    return this.http.get<EventStatsDto>(`${this.base}/stats`);
  }

  create(dto: CreateEventRequest): Observable<EventDto> {
    return this.http.post<EventDto>(this.base, dto);
  }

  update(id: string, dto: UpdateEventRequest): Observable<EventDto> {
    return this.http.put<EventDto>(`${this.base}/${id}`, dto);
  }

  publish(id: string): Observable<EventDto> {
    return this.http.post<EventDto>(`${this.base}/${id}/publish`, {});
  }

  complete(id: string): Observable<EventDto> {
    return this.http.post<EventDto>(`${this.base}/${id}/complete`, {});
  }

  cancelEvent(id: string): Observable<EventDto> {
    return this.http.post<EventDto>(`${this.base}/${id}/cancel-event`, {});
  }

  getRegistrations(id: string): Observable<EventRegistrationDto[]> {
    return this.http.get<EventRegistrationDto[]>(`${this.base}/${id}/registrations`);
  }

  register(id: string, dto: RegisterToEventRequest): Observable<EventRegistrationDto> {
    return this.http.post<EventRegistrationDto>(`${this.base}/${id}/registrations`, dto);
  }

  cancelRegistration(eventId: string, regId: string): Observable<EventRegistrationDto> {
    return this.http.delete<EventRegistrationDto>(`${this.base}/${eventId}/registrations/${regId}`);
  }

  markAttendance(id: string, dto: MarkAttendanceRequest): Observable<{ updated: number }> {
    return this.http.post<{ updated: number }>(`${this.base}/${id}/attendance`, dto);
  }
}
