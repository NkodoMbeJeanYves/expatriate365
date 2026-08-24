import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';
import {
  DocumentDto, DocumentFilters, DocumentStatsDto,
  CreateDocumentRequest, UpdateDocumentRequest,
} from '@models/document.model';

@Injectable({ providedIn: 'root' })
export class DocumentsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/documents`;

  list(filters: DocumentFilters): Observable<PagedResult<DocumentDto>> {
    const params: Record<string, string | number> = { page: filters.page, limit: filters.limit };
    if (filters.type) params['type'] = filters.type;
    if (filters.category) params['category'] = filters.category;
    if (filters.search) params['search'] = filters.search;
    return this.http.get<PagedResult<DocumentDto>>(this.base, { params });
  }

  stats(): Observable<DocumentStatsDto> {
    return this.http.get<DocumentStatsDto>(`${this.base}/stats`);
  }

  create(dto: CreateDocumentRequest): Observable<DocumentDto> {
    return this.http.post<DocumentDto>(this.base, dto);
  }

  update(id: string, dto: UpdateDocumentRequest): Observable<DocumentDto> {
    return this.http.put<DocumentDto>(`${this.base}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
