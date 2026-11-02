import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LangSwitcherComponent } from '@shared/components/lang-switcher/lang-switcher.component';
import { ButtonModule } from 'primeng/button';
import { CommunityShowcase, ShowcasesApiService } from '../services/showcases-api.service';

@Component({
  selector: 'app-showcase-directory-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, TranslatePipe, LangSwitcherComponent],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <header
        class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900"
      >
        <a routerLink="/explore" class="font-bold text-gray-900 no-underline dark:text-white">
          Expatriate<span class="text-emerald-600">365</span>
        </a>
        <div class="flex items-center gap-3">
          <app-lang-switcher />
          <a routerLink="/auth/login">
            <p-button
              [label]="'auth.login' | translate"
              icon="pi pi-sign-in"
              severity="secondary"
              size="small"
            />
          </a>
        </div>
      </header>
      <main class="mx-auto max-w-6xl px-5 py-10">
        <p class="mb-2 text-sm text-gray-500">
          {{ 'showcases.community' | translate }} · {{ tenantSlug() }}
        </p>
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
          {{ 'showcases.title' | translate }}
        </h1>
        <p class="mt-2 text-gray-600 dark:text-gray-300">
          {{ 'showcases.public_intro' | translate }}
        </p>

        @if (loading()) {
          <p class="py-12 text-center text-gray-500">{{ 'common.loading' | translate }}</p>
        } @else if (error()) {
          <p class="mt-8 rounded-lg bg-red-50 p-4 text-red-700">
            {{ 'showcases.load_error' | translate }}
          </p>
        } @else if (items().length === 0) {
          <p class="py-12 text-center text-gray-500">{{ 'showcases.empty' | translate }}</p>
        } @else {
          <div class="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            @for (showcase of items(); track showcase.id) {
              <a
                [routerLink]="['/showcases', tenantSlug(), showcase.slug]"
                class="overflow-hidden rounded-2xl border border-gray-200 bg-white text-inherit no-underline shadow-sm transition hover:shadow-md dark:border-gray-800 dark:bg-gray-900"
              >
                @if (showcase.photo_url) {
                  <img
                    [src]="showcase.photo_url"
                    [alt]="showcase.title"
                    class="aspect-video w-full object-cover"
                  />
                }
                <div class="p-5">
                  <span class="text-xs font-semibold uppercase tracking-wide text-emerald-700">{{
                    showcase.category
                  }}</span>
                  <h2 class="mt-2 text-xl font-semibold text-gray-900 dark:text-white">
                    {{ showcase.title }}
                  </h2>
                  <p class="mt-1 text-sm text-gray-500">
                    {{ showcase.display_name }} · {{ showcase.tenant_name }}
                  </p>
                  <p class="mt-3 line-clamp-3 text-sm text-gray-600 dark:text-gray-300">
                    {{ showcase.description }}
                  </p>
                  @if (showcase.service_area) {
                    <p class="mt-3 text-sm text-gray-500">{{ showcase.service_area }}</p>
                  }
                </div>
              </a>
            }
          </div>
          @if (totalPages() > 1) {
            <nav
              class="mt-8 flex items-center justify-center gap-4"
              [attr.aria-label]="'showcases.pagination' | translate"
            >
              <button
                type="button"
                class="rounded-lg border border-gray-300 px-4 py-2 text-sm disabled:opacity-50 dark:border-gray-700"
                [disabled]="loading() || page() <= 1"
                (click)="loadPage(page() - 1)"
              >
                {{ 'showcases.previous_page' | translate }}
              </button>
              <span class="text-sm text-gray-600 dark:text-gray-300">{{
                'showcases.page_status' | translate: { page: page(), total: totalPages() }
              }}</span>
              <button
                type="button"
                class="rounded-lg border border-gray-300 px-4 py-2 text-sm disabled:opacity-50 dark:border-gray-700"
                [disabled]="loading() || page() >= totalPages()"
                (click)="loadPage(page() + 1)"
              >
                {{ 'showcases.next_page' | translate }}
              </button>
            </nav>
          }
        }
      </main>
    </div>
  `,
})
export class ShowcaseDirectoryPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ShowcasesApiService);
  readonly tenantSlug = signal('');
  readonly items = signal<CommunityShowcase[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly page = signal(1);
  readonly total = signal(0);
  readonly totalPages = computed(() => Math.ceil(this.total() / 20));

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('tenantSlug') ?? '';
    this.tenantSlug.set(slug);
    this.loadPage(1);
  }

  loadPage(page: number): void {
    this.loading.set(true);
    this.error.set(false);
    this.api.list(this.tenantSlug(), page).subscribe({
      next: (result) => {
        this.items.set(result.data);
        this.page.set(result.pagination.page);
        this.total.set(result.pagination.total);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
