import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuthUser, MeResponse } from './models/user.model';
import { Role } from './models/role.model';
import { environment } from '@env/environment';

interface AuthState {
  user: MeResponse | null;
  accessToken: string | null;
  isLoading: boolean;
}

const STORAGE_KEY = 'exp365_auth';

function loadFromStorage(): Partial<AuthState> {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    return JSON.parse(raw) as Partial<AuthState>;
  } catch {
    return {};
  }
}

function decodeJwtPayload(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1];
    const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(decoded) as Record<string, unknown>;
  } catch {
    return {};
  }
}

function extractPermissions(token: string): string[] {
  const payload = decodeJwtPayload(token);
  const raw = payload['permissions'];
  if (typeof raw === 'string') {
    try { return JSON.parse(raw) as string[]; } catch { return []; }
  }
  if (Array.isArray(raw)) return raw as string[];
  return [];
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly http = inject(HttpClient);

  private readonly _state = signal<AuthState>({
    user: loadFromStorage().user ?? null,
    accessToken: loadFromStorage().accessToken ?? null,
    isLoading: false,
  });

  /** Whether the current user is an active board member (mandate-aware) */
  readonly isBoardMember = signal<boolean>(false);

  readonly user = computed(() => this._state().user);
  readonly accessToken = computed(() => this._state().accessToken);
  readonly isLoading = computed(() => this._state().isLoading);
  readonly isAuthenticated = computed(() => this._state().user !== null && this._state().accessToken !== null);
  readonly currentUser = computed(() => this._state().user);

  /** Computed set of permissions for O(1) lookups */
  private readonly _permSet = computed(() =>
    new Set(this._state().user?.permissions ?? [])
  );

  setSession(user: MeResponse, accessToken: string): void {
    const permissions = extractPermissions(accessToken);
    const enriched: MeResponse = { ...user, permissions };
    this._state.update(s => ({ ...s, user: enriched, accessToken }));
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user: enriched, accessToken }));
    this.loadBoardMemberStatus();
  }

  loadBoardMemberStatus(): void {
    this.http.get<{ is_board_member: boolean }>(
      `${environment.apiUrl}/api/v1/governance/board/me`
    ).subscribe({ next: r => this.isBoardMember.set(r.is_board_member), error: () => this.isBoardMember.set(false) });
  }

  patchCurrentUser(partial: Partial<AuthUser>): void {
    const current = this._state().user;
    if (!current) return;
    const updated = { ...current, ...partial };
    this._state.update(s => ({ ...s, user: updated }));
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user: updated, accessToken: this._state().accessToken }));
  }

  restoreSession(): void {
    if (this.isAuthenticated()) this.loadBoardMemberStatus();
  }

  clearSession(): void {
    this._state.update(s => ({ ...s, user: null, accessToken: null }));
    this.isBoardMember.set(false);
    localStorage.removeItem(STORAGE_KEY);
  }

  setLoading(isLoading: boolean): void {
    this._state.update(s => ({ ...s, isLoading }));
  }

  // ── Role helpers (kept for backwards compatibility) ──────────────────────
  hasRole(role: Role): boolean {
    return this._state().user?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: Role[]): boolean {
    const userRoles = this._state().user?.roles ?? [];
    return roles.some(r => userRoles.includes(r));
  }

  // ── Permission helpers ───────────────────────────────────────────────────
  hasPermission(permission: string): boolean {
    return this._permSet().has(permission);
  }

  hasAnyPermission(permissions: string[]): boolean {
    const set = this._permSet();
    return permissions.some(p => set.has(p));
  }

  hasAllPermissions(permissions: string[]): boolean {
    const set = this._permSet();
    return permissions.every(p => set.has(p));
  }
}
