import { computed, Injectable, signal } from '@angular/core';
import { AuthUser, MeResponse } from './models/user.model';
import { Role } from './models/role.model';

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

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly _state = signal<AuthState>({
    user: loadFromStorage().user ?? null,
    accessToken: loadFromStorage().accessToken ?? null,
    isLoading: false,
  });

  readonly user = computed(() => this._state().user);
  readonly accessToken = computed(() => this._state().accessToken);
  readonly isLoading = computed(() => this._state().isLoading);
  readonly isAuthenticated = computed(() => this._state().user !== null && this._state().accessToken !== null);
  readonly currentUser = computed(() => this._state().user);

  setSession(user: MeResponse, accessToken: string): void {
    this._state.update(s => ({ ...s, user, accessToken }));
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user, accessToken }));
  }

  patchCurrentUser(partial: Partial<AuthUser>): void {
    const current = this._state().user;
    if (!current) return;
    const updated = { ...current, ...partial };
    this._state.update(s => ({ ...s, user: updated }));
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user: updated, accessToken: this._state().accessToken }));
  }

  clearSession(): void {
    this._state.update(s => ({ ...s, user: null, accessToken: null }));
    localStorage.removeItem(STORAGE_KEY);
  }

  setLoading(isLoading: boolean): void {
    this._state.update(s => ({ ...s, isLoading }));
  }

  hasRole(role: Role): boolean {
    return this._state().user?.roles.includes(role) ?? false;
  }

  hasAnyRole(roles: Role[]): boolean {
    const userRoles = this._state().user?.roles ?? [];
    return roles.some(r => userRoles.includes(r));
  }
}
