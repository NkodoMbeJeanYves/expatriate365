import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AuthService } from '@core/auth/auth.service';
import { PublicTenant } from '@core/auth/models/user.model';
import { TenantStore } from '@core/tenant/tenant.store';

@Component({
  selector: 'app-select-tenant-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, ProgressSpinnerModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex items-center justify-center p-6">
      <div class="w-full max-w-lg">

        <div class="text-center mb-8">
          <div class="w-14 h-14 rounded-2xl bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center mx-auto mb-4">
            <i class="pi pi-globe text-2xl text-primary-600 dark:text-primary-400"></i>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
            {{ 'auth.select_tenant_title' | translate }}
          </h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {{ 'auth.select_tenant_subtitle' | translate }}
          </p>
        </div>

        @if (loading()) {
          <div class="flex justify-center py-12">
            <p-progressspinner strokeWidth="4" />
          </div>
        } @else if (tenants().length === 0) {
          <div class="text-center py-12 text-gray-400">
            <i class="pi pi-inbox text-4xl mb-3 block"></i>
            <p>{{ 'auth.no_tenants' | translate }}</p>
          </div>
        } @else {
          <div class="flex flex-col gap-3">
            @for (t of tenants(); track t.id) {
              <button
                type="button"
                (click)="select(t)"
                [disabled]="selecting()"
                class="flex items-center gap-4 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700
                       rounded-xl p-4 text-left hover:border-primary-400 hover:shadow-md
                       transition-all duration-150 disabled:opacity-60 disabled:cursor-not-allowed cursor-pointer">
                @if (t.logo_url) {
                  <img [src]="t.logo_url" [alt]="t.name"
                       class="w-12 h-12 rounded-lg object-cover shrink-0" />
                } @else {
                  <div class="w-12 h-12 rounded-lg bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center shrink-0">
                    <i class="pi pi-users text-xl text-primary-600 dark:text-primary-400"></i>
                  </div>
                }
                <div class="flex-1 min-w-0">
                  <p class="font-semibold text-gray-900 dark:text-white truncate">{{ t.name }}</p>
                  <p class="text-xs text-gray-400 truncate">{{ t.slug }}</p>
                </div>
                <i class="pi pi-chevron-right text-gray-300 dark:text-gray-600"></i>
              </button>
            }
          </div>
        }

        @if (errorMessage()) {
          <div class="mt-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800
                      p-3 text-sm text-red-700 dark:text-red-400">
            <i class="pi pi-exclamation-circle mr-2"></i>{{ errorMessage() }}
          </div>
        }

      </div>
    </div>
  `,
})
export class SelectTenantPage implements OnInit {
  private readonly auth    = inject(AuthService);
  private readonly tenant  = inject(TenantStore);
  private readonly router  = inject(Router);

  readonly tenants      = signal<PublicTenant[]>([]);
  readonly loading      = signal(true);
  readonly selecting    = signal(false);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.auth.getPublicTenants().subscribe({
      next: (list) => { this.tenants.set(list); this.loading.set(false); },
      error: ()    => { this.loading.set(false); },
    });
  }

  select(t: PublicTenant): void {
    this.selecting.set(true);
    this.errorMessage.set(null);
    this.auth.selectTenant(t.id).subscribe({
      next: () => {
        this.tenant.load();
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.error ?? 'Une erreur est survenue.');
        this.selecting.set(false);
      },
    });
  }
}
