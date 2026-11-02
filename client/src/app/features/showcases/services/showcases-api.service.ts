import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { APP_CONFIG } from '@core/config/app-config.token';
import { PagedResult } from '@shared/models/pagination.model';
import { Observable } from 'rxjs';

export interface ShowcaseOffer {
  id: string;
  name: string;
  description: string;
  price?: number;
  price_on_request: boolean;
  details?: string;
  position: number;
}

export interface CommunityShowcase {
  id: string;
  slug: string;
  display_name: string;
  title: string;
  description: string;
  category: string;
  service_area?: string;
  contact_phone?: string;
  contact_link?: string;
  contact_enabled: boolean;
  photo_url?: string;
  status: 'draft' | 'published' | 'suspended';
  tenant_name: string;
  tenant_slug: string;
  currency_code: string;
  currency_symbol: string;
  contact_available: boolean;
  created_at: string;
  updated_at?: string;
  offers: ShowcaseOffer[];
}

export interface ShowcaseOfferRequest {
  name: string;
  description: string;
  price: number | null;
  price_on_request: boolean;
  details: string;
}

export interface SaveShowcaseRequest {
  display_name: string;
  title: string;
  description: string;
  category: string;
  service_area: string;
  contact_phone: string;
  contact_link: string;
  contact_enabled: boolean;
  photo_url: string;
  offers: ShowcaseOfferRequest[];
}

export interface ShowcaseReport {
  id: string;
  showcase_id: string;
  showcase_title: string;
  showcase_slug: string;
  reason: string;
  status: string;
  resolution_note?: string;
  created_at: string;
}

@Injectable({ providedIn: 'root' })
export class ShowcasesApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  private get base(): string {
    return `${this.config.apiUrl}/api/v1/showcases`;
  }

  list(tenantSlug: string, page = 1): Observable<PagedResult<CommunityShowcase>> {
    const params = new HttpParams().set('page', page).set('limit', 20);
    return this.http.get<PagedResult<CommunityShowcase>>(`${this.base}/${tenantSlug}`, { params });
  }

  get(tenantSlug: string, slug: string) {
    return this.http.get<CommunityShowcase>(`${this.base}/${tenantSlug}/${slug}`);
  }

  getMine() {
    return this.http.get<CommunityShowcase | null>(`${this.config.apiUrl}/api/v1/my/showcases/me`);
  }

  saveMine(request: SaveShowcaseRequest) {
    return this.http.put<CommunityShowcase>(`${this.config.apiUrl}/api/v1/my/showcases/me`, request);
  }

  publishMine() {
    return this.http.post<CommunityShowcase>(`${this.config.apiUrl}/api/v1/my/showcases/me/publish`, {});
  }

  unpublishMine() {
    return this.http.post<CommunityShowcase>(`${this.config.apiUrl}/api/v1/my/showcases/me/unpublish`, {});
  }

  uploadPhoto(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ file_url: string }>(
      `${this.config.apiUrl}/api/v1/upload?folder=showcases`,
      form,
    );
  }

  contact(tenantSlug: string, slug: string, request: {
    name: string;
    email: string;
    message: string;
    website: string;
  }) {
    return this.http.post<void>(`${this.base}/${tenantSlug}/${slug}/contact`, request);
  }

  report(tenantSlug: string, slug: string, reason: string, website: string) {
    return this.http.post<void>(`${this.base}/${tenantSlug}/${slug}/reports`, { reason, website });
  }

  getReports(page = 1) {
    const params = new HttpParams().set('page', page).set('limit', 20);
    return this.http.get<PagedResult<ShowcaseReport>>(
      `${this.config.apiUrl}/api/v1/moderation/showcases/reports`,
      { params },
    );
  }

  resolveReport(id: string, action: 'hide' | 'dismiss') {
    return this.http.post<{ report_id: string; status: string }>(
      `${this.config.apiUrl}/api/v1/moderation/showcases/reports/${id}/resolve`,
      { action },
    );
  }
}
