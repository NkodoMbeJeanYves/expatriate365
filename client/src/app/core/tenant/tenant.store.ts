import { computed, Injectable, signal } from '@angular/core';

export interface TenantSettings {
  name: string;
  slug: string;
  base_currency: string;
  currency_symbol: string;
  country_code: string;
  logo_url?: string;
  subscription_tier: string;
  subscription_status: string;
}

const STORAGE_KEY = 'exp365_tenant';

function loadFromStorage(): TenantSettings | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as TenantSettings) : null;
  } catch {
    return null;
  }
}

const DEFAULT: TenantSettings = {
  name: '',
  slug: '',
  base_currency: 'XAF',
  currency_symbol: 'FCFA',
  country_code: 'CM',
  subscription_tier: 'community',
  subscription_status: 'active',
};

@Injectable({ providedIn: 'root' })
export class TenantStore {
  private readonly _settings = signal<TenantSettings>(loadFromStorage() ?? DEFAULT);

  readonly settings    = computed(() => this._settings());
  readonly name        = computed(() => this._settings().name);
  readonly logoUrl     = computed(() => this._settings().logo_url ? `${this._settings().logo_url}?t=${new Date().getTime()}` : null);
  readonly currency    = computed(() => this._settings().base_currency);
  readonly symbol      = computed(() => this._settings().currency_symbol);
  readonly countryCode = computed(() => this._settings().country_code);

  set(settings: TenantSettings): void {
    this._settings.set(settings);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
  }

  patch(partial: Partial<TenantSettings>): void {
    const updated = { ...this._settings(), ...partial };
    this._settings.set(updated);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  }

  clear(): void {
    this._settings.set(DEFAULT);
    localStorage.removeItem(STORAGE_KEY);
  }
}
