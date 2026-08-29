import { TenantApiService } from '@admin/services/tenant-api.service';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { APP_CONFIG } from '@core/config/app-config.token';
import { TenantService } from '@core/tenant/tenant.service';
import { TenantStore } from '@core/tenant/tenant.store';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';

const CURRENCIES = [
  { label: 'Euro (€)',               value: 'EUR', symbol: '€'  },
  { label: 'Franc CFA BEAC (FCFA)',  value: 'XAF', symbol: 'FCFA' },
  { label: 'Franc CFA BCEAO (FCFA)', value: 'XOF', symbol: 'FCFA' },
  { label: 'Roupie mauricienne (₨)', value: 'MUR', symbol: '₨'  },
  { label: 'Dollar US ($)',          value: 'USD', symbol: '$'   },
  { label: 'Livre sterling (£)',     value: 'GBP', symbol: '£'   },
  { label: 'Franc suisse (CHF)',     value: 'CHF', symbol: 'CHF' },
  { label: 'Dirham marocain (MAD)',  value: 'MAD', symbol: 'MAD' },
];

const COUNTRIES = [
  { label: 'Maurice',      value: 'MU' },
  { label: 'France',       value: 'FR' },
  { label: 'Cameroun',     value: 'CM' },
  { label: 'Belgique',     value: 'BE' },
  { label: 'Suisse',       value: 'CH' },
  { label: 'Canada',       value: 'CA' },
  { label: 'Côte d\'Ivoire', value: 'CI' },
  { label: 'Sénégal',      value: 'SN' },
  { label: 'Maroc',        value: 'MA' },
];

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, SelectModule, MessageModule, TranslatePipe],
  template: `
    <div class="p-6 max-w-2xl flex flex-col gap-6">

      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-white">{{ 'settings.title' | translate }}</h1>
        <p class="text-gray-500 text-sm mt-1">{{ 'settings.subtitle' | translate }}</p>
      </div>

      @if (loading()) {
        <div class="text-gray-400 text-sm">{{ 'common.loading' | translate }}</div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="save()" class="flex flex-col gap-6">

          <!-- Logo -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
              <i class="pi pi-image mr-2 text-blue-500"></i>{{ 'settings.logo' | translate }}
            </h2>
            <div class="flex flex-col gap-1">
              <div class="flex items-center gap-4">
                <div class="w-16 h-16 rounded-lg bg-gray-100 border border-gray-200 overflow-hidden flex items-center justify-center flex-shrink-0">
                  @if (logoPreview()) {
                    <img [src]="logoPreview()" alt="Logo" class="w-full h-full object-contain" />
                  } @else {
                    <i class="pi pi-image text-2xl text-gray-400"></i>
                  }
                </div>
                <label class="cursor-pointer inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-300 text-sm text-gray-600 hover:bg-gray-50 transition-colors">
                  @if (logoUploading()) {
                    <i class="pi pi-spin pi-spinner text-sm"></i> {{ 'common.uploading' | translate }}
                  } @else {
                    <i class="pi pi-upload text-sm"></i> {{ 'settings.upload_logo' | translate }}
                  }
                  <input type="file" class="hidden" accept="image/jpeg,image/png,image/gif,image/webp,image/svg+xml"
                    (change)="onLogoSelected($event)" [disabled]="logoUploading()" />
                </label>
                @if (logoPreview()) {
                  <button type="button" (click)="removeLogo()" class="text-xs text-red-500 hover:text-red-700">
                    <i class="pi pi-times"></i>
                  </button>
                }
              </div>
              <p class="text-xs text-gray-400 mt-2">{{ 'settings.logo_hint' | translate }}</p>
            </div>
          </div>

          <!-- Infos générales -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
              <i class="pi pi-building mr-2 text-emerald-600"></i>{{ 'settings.general' | translate }}
            </h2>
            <div class="flex flex-col gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ 'settings.association_name' | translate }}
                </label>
                <input pInputText formControlName="name" class="w-full"
                  [placeholder]="'settings.association_name' | translate" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ 'settings.country' | translate }}
                </label>
                <p-select formControlName="country_code" [options]="countries"
                  optionLabel="label" optionValue="value"
                  placeholder="Sélectionner un pays" class="w-full" />
              </div>
            </div>
          </div>

          <!-- Devise -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">
              <i class="pi pi-wallet mr-2 text-amber-500"></i>{{ 'settings.currency_section' | translate }}
            </h2>
            <div class="flex flex-col gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ 'settings.currency' | translate }}
                </label>
                <p-select formControlName="base_currency" [options]="currencies"
                  optionLabel="label" optionValue="value"
                  placeholder="Sélectionner une devise" class="w-full"
                  (onChange)="onCurrencyChange($event)" />
                <p class="text-xs text-gray-400 mt-1">{{ 'settings.currency_hint' | translate }}</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {{ 'settings.currency_symbol' | translate }}
                </label>
                <div class="flex items-center gap-3">
                  <input pInputText formControlName="currency_symbol" class="w-32"
                    placeholder="€" maxlength="10" />
                  <span class="text-sm text-gray-400">
                    {{ 'settings.preview' | translate }} : <strong class="text-gray-700 dark:text-gray-200">
                      1 500 {{ form.get('currency_symbol')?.value || '?' }}
                    </strong>
                  </span>
                </div>
                <p class="text-xs text-gray-400 mt-1">{{ 'settings.currency_symbol_hint' | translate }}</p>
              </div>
            </div>
          </div>

          <!-- Feedback -->
          @if (saved()) {
            <p-message severity="success">{{ 'settings.saved' | translate }}</p-message>
          }
          @if (error()) {
            <p-message severity="error">{{ error()! }}</p-message>
          }

          <div class="flex justify-end">
            <p-button type="submit" [label]="'common.save' | translate" icon="pi pi-check"
              [loading]="saving()" severity="success" />
          </div>

        </form>
      }
    </div>
  `,
})
export class AdminSettingsPage implements OnInit {
  private readonly tenantService = inject(TenantService);
  private readonly tenantStore   = inject(TenantStore);
  private readonly tenantApi     = inject(TenantApiService);
  private readonly http          = inject(HttpClient);
  private readonly config        = inject(APP_CONFIG);
  private readonly fb = inject(FormBuilder);
  private readonly translateService = inject(TranslateService);

  readonly loading       = signal(true);
  readonly saving        = signal(false);
  readonly saved         = signal(false);
  readonly error         = signal<string | null>(null);
  readonly logoPreview   = signal<string | null>(null);
  readonly logoUploading = signal(false);

  readonly currencies = CURRENCIES;
  readonly countries  = COUNTRIES;

  readonly form = this.fb.group({
    name:            ['', Validators.required],
    country_code:    [''],
    base_currency:   ['EUR'],
    currency_symbol: ['€'],
  });

  ngOnInit(): void {
    // Initialize logo preview from cached store
    this.logoPreview.set(this.tenantStore.logoUrl());

    // Pre-fill from cached store, then refresh from API
    const cached = this.tenantStore.settings();
    if (cached.name) {
      this.form.patchValue({
        name:            cached.name,
        country_code:    cached.country_code,
        base_currency:   cached.base_currency,
        currency_symbol: cached.currency_symbol,
      });
      this.loading.set(false);
    }
    this.tenantService.load().subscribe({
      next: s => {
        this.form.patchValue({
          name:            s.name,
          country_code:    s.country_code,
          base_currency:   s.base_currency,
          currency_symbol: s.currency_symbol,
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onCurrencyChange(event: any): void {
    const match = CURRENCIES.find(c => c.value === event.value);
    if (match) this.form.patchValue({ currency_symbol: match.symbol });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.logoUploading.set(true);
    const formData = new FormData();
    formData.append('file', file);

    this.http.post<{ file_url: string }>(`${this.config.apiUrl}/api/v1/upload?folder=logos`, formData).subscribe({
      next: ({ file_url }) => {
        const timestamp = new Date().getTime();
        this.logoPreview.set(`${file_url}?t=${timestamp}`);
        this.logoUploading.set(false);
        this.tenantApi.updateSettings({ logo_url: file_url }).subscribe();
      },
      error: () => {
        this.logoUploading.set(false);
        this.error.set(this.translateService.instant('errors.save_error'));
      },
    });
  }

  removeLogo(): void {
    this.logoPreview.set(null);
    this.tenantApi.updateSettings({ logo_url: null as unknown as undefined }).subscribe();
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    const v = this.form.value;
    this.tenantService.update({
      name:            v.name ?? undefined,
      country_code:    v.country_code ?? undefined,
      base_currency:   v.base_currency ?? undefined,
      currency_symbol: v.currency_symbol ?? undefined,
    }).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); },
      error: () => { this.saving.set(false); this.error.set(this.translateService.instant('errors.save_error')); },
    });
  }
}
