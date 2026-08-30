import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ExploreApiService } from '../../services/explore-api.service';
import { PublicTenant } from '@core/auth/models/user.model';

@Component({
  selector: 'app-explore-home',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, ProgressSpinnerModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- Header -->
      <header class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-6 py-4
                     flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center">
            <i class="pi pi-globe text-white text-sm"></i>
          </div>
          <span class="font-bold text-gray-900 dark:text-white text-lg">Expatriate365</span>
        </div>
        <a routerLink="/auth/login">
          <p-button [label]="'auth.login' | translate" icon="pi pi-sign-in" severity="secondary" size="small" />
        </a>
      </header>

      <main class="max-w-4xl mx-auto px-6 py-12">
        <div class="text-center mb-10">
          <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            {{ 'explore.title' | translate }}
          </h1>
          <p class="text-gray-500 dark:text-gray-400">{{ 'explore.subtitle' | translate }}</p>
        </div>

        @if (loading()) {
          <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
        } @else if (tenants().length === 0) {
          <div class="text-center py-16 text-gray-400">
            <i class="pi pi-users text-4xl mb-3 block"></i>
            <p>{{ 'explore.no_communities' | translate }}</p>
          </div>
        } @else {
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            @for (t of tenants(); track t.id) {
              <a [routerLink]="['/explore', t.slug]"
                 class="flex items-center gap-4 bg-white dark:bg-gray-900 border border-gray-200
                        dark:border-gray-700 rounded-xl p-5 hover:border-primary-400 hover:shadow-md
                        transition-all duration-150 no-underline">
                @if (t.logo_url) {
                  <img [src]="t.logo_url" [alt]="t.name"
                       class="w-14 h-14 rounded-xl object-cover shrink-0" />
                } @else {
                  <div class="w-14 h-14 rounded-xl bg-primary-100 dark:bg-primary-900/30
                              flex items-center justify-center shrink-0">
                    <i class="pi pi-users text-2xl text-primary-600 dark:text-primary-400"></i>
                  </div>
                }
                <div class="flex-1 min-w-0">
                  <p class="font-semibold text-gray-900 dark:text-white truncate">{{ t.name }}</p>
                  <p class="text-xs text-gray-400 truncate mt-0.5">{{ t.slug }}</p>
                </div>
                <i class="pi pi-chevron-right text-gray-300 dark:text-gray-600 shrink-0"></i>
              </a>
            }
          </div>
        }
      </main>
    </div>
  `,
})
export class ExploreHomePage implements OnInit {
  private readonly api = inject(ExploreApiService);

  readonly tenants = signal<PublicTenant[]>([]);
  readonly loading = signal(true);

  ngOnInit(): void {
    this.api.getTenants().subscribe({
      next: (list) => { this.tenants.set(list); this.loading.set(false); },
      error: ()    => this.loading.set(false),
    });
  }
}
