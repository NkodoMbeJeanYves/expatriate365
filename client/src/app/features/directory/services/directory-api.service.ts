import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

export interface DirectoryMember {
  id: string;
  first_name: string;
  last_name: string;
  profession?: string;
  photo_url?: string;
  address?: string;
  tenant_name: string;
  tenant_slug: string;
}

@Injectable({ providedIn: 'root' })
export class DirectoryApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/directory`;

  getDirectory(slug: string, search?: string, profession?: string): Observable<DirectoryMember[]> {
    const params: Record<string, string> = {};
    if (search) params['search'] = search;
    if (profession) params['profession'] = profession;
    return this.http.get<DirectoryMember[]>(`${this.base}/${slug}`, { params });
  }
}
