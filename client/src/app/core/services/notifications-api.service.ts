import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { NotificationsResponse } from '@models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/notifications`;

  get(page = 1, limit = 20, unreadOnly = false): Observable<NotificationsResponse> {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (unreadOnly) params = params.set('unread_only', true);
    return this.http.get<NotificationsResponse>(this.base, { params });
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/read`, {});
  }

  markAllRead(): Observable<{ marked: number }> {
    return this.http.post<{ marked: number }>(`${this.base}/read-all`, {});
  }
}
