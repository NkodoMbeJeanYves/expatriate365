import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { PermissionDomain, RoleDto, UpdateRolePermissionsRequest } from '@models/admin.model';

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/roles`;

  list(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(this.base);
  }

  listPermissions(): Observable<PermissionDomain[]> {
    return this.http.get<PermissionDomain[]>(`${this.base}/permissions`);
  }

  updatePermissions(roleId: string, dto: UpdateRolePermissionsRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${roleId}/permissions`, dto);
  }
}
