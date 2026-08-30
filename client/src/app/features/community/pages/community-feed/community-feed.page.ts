import {
  ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { PostSummaryDto } from '@models/post.model';
import { CommunityStore } from '../../store/community.store';
import { CommunityApiService } from '../../services/community-api.service';
import { PostFormDrawerComponent } from '../../components/post-form-drawer/post-form-drawer.component';

@Component({
  selector: 'app-community-feed',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule, DatePipe, ButtonModule, InputTextModule,
    ProgressSpinnerModule, TagModule, TooltipModule, TranslatePipe,
    PostFormDrawerComponent,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'community.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'community.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'community.new_post' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Search -->
      <div class="flex gap-3">
        <input pInputText [(ngModel)]="searchTerm" [placeholder]="'common.search' | translate" class="w-64"
          (input)="onSearch()" />
      </div>

      <!-- Feed -->
      @if (store.loading()) {
        <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.posts().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-users text-4xl mb-3 block"></i>
          <p>{{ 'community.no_posts' | translate }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
          @for (post of store.posts(); track post.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5 flex flex-col gap-3 hover:shadow-md transition-shadow">
              <!-- Author & date + status badge -->
              <div class="flex items-center justify-between gap-2">
                <div class="flex items-center gap-3 min-w-0">
                  <div class="w-9 h-9 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-700 font-bold text-sm shrink-0">
                    {{ post.author_name[0]?.toUpperCase() }}
                  </div>
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-gray-800 truncate">{{ post.author_name }}</p>
                    <p class="text-xs text-gray-400">{{ post.published_at ?? post.created_at | date:'mediumDate' }}</p>
                  </div>
                </div>
                <span [class]="statusClass(post.status)" class="shrink-0 text-xs font-medium px-2 py-0.5 rounded-full">
                  {{ ('community.status_' + post.status) | translate }}
                </span>
              </div>

              <!-- Title & preview -->
              <div>
                <h3 class="font-semibold text-gray-800 mb-1 line-clamp-2">{{ post.title }}</h3>
                <p class="text-sm text-gray-500 line-clamp-3">{{ post.content_preview }}</p>
              </div>

              <!-- Footer -->
              <div class="flex items-center justify-between pt-2 border-t border-gray-50">
                <div class="flex items-center gap-2 text-xs text-gray-400">
                  @if (post.attachment_count > 0) {
                    <span><i class="pi pi-paperclip mr-1"></i>{{ post.attachment_count }}</span>
                  }
                </div>
                <a [routerLink]="['/community', post.id]">
                  <p-button size="small" [label]="'community.read_more' | translate" severity="secondary" />
                </a>
              </div>
            </div>
          }
        </div>

        @if (store.hasMore()) {
          <div class="flex justify-center">
            <p-button [label]="'common.load_more' | translate" severity="secondary"
              (onClick)="loadMore()" [loading]="loadingMore()" />
          </div>
        }
      }
    </div>

    <app-post-form-drawer #formDrawer (saved)="onSaved()" />
  `,
})
export class CommunityFeedPage implements OnInit {
  protected readonly store = inject(CommunityStore);
  private readonly api     = inject(CommunityApiService);

  private readonly formDrawer = viewChild.required<PostFormDrawerComponent>('formDrawer');

  searchTerm  = '';
  readonly loadingMore = signal(false);
  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  readonly statusClass = (status: string): string => ({
    published: 'bg-emerald-100 text-emerald-700',
    draft:     'bg-yellow-100 text-yellow-700',
    rejected:  'bg-red-100 text-red-600',
  })[status] ?? 'bg-gray-100 text-gray-500';

  ngOnInit(): void {
    this.store.load();
  }

  openForm(): void {
    this.formDrawer().open();
  }

  onSaved(): void {
    this.store.load();
  }

  onSearch(): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.store.load({ search: this.searchTerm || undefined });
    }, 300);
  }

  loadMore(): void {
    this.loadingMore.set(true);
    const next = this.store.page() + 1;
    this.api.list({ page: next, limit: this.store.limit, search: this.searchTerm || undefined })
      .subscribe({ /* loadMore */
        next: r => {
          this.store.load({ page: next, search: this.searchTerm || undefined });
          this.loadingMore.set(false);
        },
        error: () => this.loadingMore.set(false),
      });
  }
}
