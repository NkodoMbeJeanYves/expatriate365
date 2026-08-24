import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import { AdminUserDto, AdminStatsDto, InviteUserRequest, ChangeRoleRequest } from '@models/admin.model';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/admin`;

  stats(): Observable<AdminStatsDto> {
    return this.http.get<AdminStatsDto>(`${this.base}/stats`);
  }

  listUsers(page = 1, limit = 20, role?: string, status?: string): Observable<PagedResult<AdminUserDto>> {
    const params: Record<string, string | number> = { page, limit };
    if (role) params['role'] = role;
    if (status) params['status'] = status;
    return this.http.get<PagedResult<AdminUserDto>>(`${this.base}/users`, { params });
  }

  invite(dto: InviteUserRequest): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${this.base}/users/invite`, dto);
  }

  changeRole(id: string, dto: ChangeRoleRequest): Observable<AdminUserDto> {
    return this.http.put<AdminUserDto>(`${this.base}/users/${id}/role`, dto);
  }

  activate(id: string): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${this.base}/users/${id}/activate`, {});
  }

  deactivate(id: string): Observable<AdminUserDto> {
    return this.http.post<AdminUserDto>(`${this.base}/users/${id}/deactivate`, {});
  }
}
