import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TenantSettings, TenantStore } from './tenant.store';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(TenantStore);
  private readonly base = `${environment.apiUrl}/api/v1/tenant`;

  load(): Observable<TenantSettings> {
    return this.http.get<TenantSettings>(`${this.base}/settings`).pipe(
      tap(settings => this.store.set(settings)),
    );
  }

  update(body: Partial<TenantSettings>): Observable<TenantSettings> {
    return this.http.put<TenantSettings>(`${this.base}/settings`, body).pipe(
      tap(settings => this.store.set(settings)),
    );
  }

  bootstrap(): Promise<void> {
    return new Promise(resolve => {
      this.load().subscribe({ next: () => resolve(), error: () => resolve() });
    });
  }
}
