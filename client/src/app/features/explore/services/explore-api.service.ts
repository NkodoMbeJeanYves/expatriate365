import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { APP_CONFIG } from '@core/config/app-config.token';
import { PagedResult } from '@shared/models/pagination.model';
import { PostSummaryDto, PostDto } from '@models/post.model';
import { PublicTenant } from '@core/auth/models/user.model';

@Injectable({ providedIn: 'root' })
export class ExploreApiService {
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  private get base(): string { return `${this.config.apiUrl}/api/v1`; }

  getTenants() {
    return this.http.get<PublicTenant[]>(`${this.base}/tenants/public`);
  }

  getPosts(slug: string, page = 1, limit = 12, search?: string) {
    let params = new HttpParams().set('page', page).set('limit', limit);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<PostSummaryDto>>(`${this.base}/explore/${slug}/posts`, { params });
  }

  getPost(slug: string, id: string) {
    return this.http.get<PostDto>(`${this.base}/explore/${slug}/posts/${id}`);
  }
}
