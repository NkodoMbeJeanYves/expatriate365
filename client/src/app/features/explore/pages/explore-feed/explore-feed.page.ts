import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ExploreApiService } from '../../services/explore-api.service';
import { PostSummaryDto } from '@models/post.model';
import { PublicTenant } from '@core/auth/models/user.model';

@Component({
  selector: 'app-explore-feed',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FormsModule, DatePipe, ButtonModule, InputTextModule, ProgressSpinnerModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- Header -->
      <header class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-6 py-4
                     flex items-center justify-between">
        <div class="flex items-center gap-3">
          <a routerLink="/explore" class="text-gray-400 hover:text-gray-600 transition-colors">
            <i class="pi pi-arrow-left"></i>
          </a>
          @if (tenant()?.logo_url) {
            <img [src]="tenant()!.logo_url!" [alt]="tenant()!.name"
                 class="w-8 h-8 rounded-lg object-cover" />
          }
          <span class="font-bold text-gray-900 dark:text-white">{{ tenant()?.name ?? slug() }}</span>
        </div>
        <a routerLink="/auth/login">
          <p-button [label]="'auth.login' | translate" icon="pi pi-sign-in" severity="secondary" size="small" />
        </a>
      </header>

      <main class="max-w-5xl mx-auto px-6 py-8">

        <!-- Search -->
        <div class="mb-6 flex gap-3">
          <input pInputText [(ngModel)]="searchTerm" [placeholder]="'common.search' | translate"
                 class="w-64" (keyup.enter)="search()" />
          <p-button icon="pi pi-search" severity="secondary" (onClick)="search()" />
        </div>

        @if (loading()) {
          <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
        } @else if (posts().length === 0) {
          <div class="text-center py-16 text-gray-400">
            <i class="pi pi-comments text-4xl mb-3 block"></i>
            <p>{{ 'community.no_posts' | translate }}</p>
          </div>
        } @else {
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
            @for (post of posts(); track post.id) {
              <a [routerLink]="['/explore', slug(), post.id]"
                 class="bg-white dark:bg-gray-900 rounded-xl border border-gray-100 dark:border-gray-800
                        shadow-sm p-5 flex flex-col gap-3 hover:shadow-md transition-shadow no-underline">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/30
                              flex items-center justify-center shrink-0">
                    <i class="pi pi-user text-primary-600 dark:text-primary-400 text-sm"></i>
                  </div>
                  <div class="min-w-0">
                    <p class="text-xs font-medium text-gray-700 dark:text-gray-300 truncate">{{ post.author_name }}</p>
                    <p class="text-xs text-gray-400">
                      {{ (post.published_at ?? post.created_at) | date:'mediumDate' }}
                    </p>
                  </div>
                </div>
                <h3 class="font-semibold text-gray-900 dark:text-white line-clamp-2 leading-snug">
                  {{ post.title }}
                </h3>
                <p class="text-sm text-gray-500 dark:text-gray-400 line-clamp-3 flex-1">
                  {{ post.content_preview }}
                </p>
                @if (post.attachment_count > 0) {
                  <p class="text-xs text-gray-400 flex items-center gap-1">
                    <i class="pi pi-paperclip"></i> {{ post.attachment_count }} fichier(s)
                  </p>
                }
              </a>
            }
          </div>

          @if (hasMore()) {
            <div class="flex justify-center mt-8">
              <p-button [label]="'community.load_more' | translate" severity="secondary"
                        [loading]="loadingMore()" (onClick)="loadMore()" />
            </div>
          }
        }
      </main>
    </div>
  `,
})
export class ExploreFeedPage implements OnInit {
  private readonly api   = inject(ExploreApiService);
  private readonly route = inject(ActivatedRoute);

  readonly slug       = signal('');
  readonly tenant     = signal<PublicTenant | null>(null);
  readonly posts      = signal<PostSummaryDto[]>([]);
  readonly total      = signal(0);
  readonly loading    = signal(true);
  readonly loadingMore = signal(false);
  readonly page       = signal(1);
  readonly limit      = 12;
  readonly hasMore    = computed(() => this.posts().length < this.total());
  searchTerm          = '';

  ngOnInit(): void {
    const s = this.route.snapshot.paramMap.get('slug') ?? '';
    this.slug.set(s);
    this.loadTenant(s);
    this.loadPosts(1);
  }

  private loadTenant(slug: string): void {
    this.api.getTenants().subscribe({
      next: (list) => this.tenant.set(list.find(t => t.slug === slug) ?? null),
    });
  }

  private loadPosts(page: number, append = false): void {
    const obs = this.api.getPosts(this.slug(), page, this.limit, this.searchTerm || undefined);
    obs.subscribe({
      next: (r) => {
        this.posts.update(prev => append ? [...prev, ...r.data] : r.data);
        this.total.set(r.pagination.total);
        this.page.set(page);
        this.loading.set(false);
        this.loadingMore.set(false);
      },
      error: () => { this.loading.set(false); this.loadingMore.set(false); },
    });
  }

  search(): void { this.loading.set(true); this.loadPosts(1); }

  loadMore(): void { this.loadingMore.set(true); this.loadPosts(this.page() + 1, true); }
}
