import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantSettingsDto {
  name: string;
  slug: string;
  base_currency: string;
  currency_symbol: string;
  country_code: string;
  logo_url?: string;
  subscription_tier: string;
  subscription_status: string;
}

export interface UpdateTenantSettingsRequest {
  name?: string;
  base_currency?: string;
  currency_symbol?: string;
  country_code?: string;
  logo_url?: string;
}

@Injectable({ providedIn: 'root' })
export class TenantApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/tenant`;

  getSettings(): Observable<TenantSettingsDto> {
    return this.http.get<TenantSettingsDto>(`${this.base}/settings`);
  }

  updateSettings(body: UpdateTenantSettingsRequest): Observable<TenantSettingsDto> {
    return this.http.put<TenantSettingsDto>(`${this.base}/settings`, body);
  }
}
