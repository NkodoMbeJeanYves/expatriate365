import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';
import { TenantSummaryDto, CreateTenantRequest } from '@models/admin.model';

@Injectable({ providedIn: 'root' })
export class SuperAdminApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/superadmin`;

  listTenants(): Observable<TenantSummaryDto[]> {
    return this.http.get<TenantSummaryDto[]>(`${this.base}/tenants`);
  }

  createTenant(dto: CreateTenantRequest): Observable<TenantSummaryDto> {
    return this.http.post<TenantSummaryDto>(`${this.base}/tenants`, dto);
  }
}
