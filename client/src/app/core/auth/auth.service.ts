import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { AuthStore } from './auth.store';
import { LoginRequest, LoginResponse, MeResponse } from './models/user.model';
import { APP_CONFIG } from '@core/config/app-config.token';
import { TenantStore } from '@core/tenant/tenant.store';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly store = inject(AuthStore);
  private readonly tenantStore = inject(TenantStore);
  private readonly router = inject(Router);
  private readonly config = inject(APP_CONFIG);

  private get base(): string {
    return `${this.config.apiUrl}/api/v1`;
  }

  login(body: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.base}/auth/login`, body).pipe(
      tap((res) => {
        this.store.setSession(res.user as MeResponse, res.access_token);
        localStorage.setItem('exp365_refresh', res.refresh_token);
      })
    );
  }

  logout(): void {
    const refreshToken = localStorage.getItem('exp365_refresh');
    if (refreshToken) {
      this.http.post(`${this.base}/auth/logout`, { refresh_token: refreshToken }).subscribe({ error: () => {} });
    }
    this.store.clearSession();
    this.tenantStore.clear();
    localStorage.removeItem('exp365_refresh');
    this.router.navigateByUrl('/auth/login');
  }

  forceLogout(): void {
    this.store.clearSession();
    this.tenantStore.clear();
    localStorage.removeItem('exp365_refresh');
    this.router.navigateByUrl('/auth/login');
  }

  refresh() {
    const refreshToken = localStorage.getItem('exp365_refresh');
    return this.http
      .post<LoginResponse>(`${this.base}/auth/refresh`, { refresh_token: refreshToken })
      .pipe(tap((res) => {
        this.store.setSession(res.user as MeResponse, res.access_token);
        localStorage.setItem('exp365_refresh', res.refresh_token);
      }));
  }

  me() {
    return this.http.get<MeResponse>(`${this.base}/auth/me`).pipe(
      tap((user) => {
        const token = this.store.accessToken();
        if (token) this.store.setSession(user, token);
      })
    );
  }

  bootstrap(): Promise<void> {
    const token = this.store.accessToken();
    if (!token) return Promise.resolve();
    return new Promise((resolve) => {
      this.me().subscribe({ next: () => resolve(), error: () => resolve() });
    });
  }
}
