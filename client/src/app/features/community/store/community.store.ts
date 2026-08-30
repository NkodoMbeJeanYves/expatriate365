import { Injectable, inject, signal, computed } from '@angular/core';
import { PostSummaryDto } from '@models/post.model';
import { CommunityApiService } from '../services/community-api.service';

@Injectable({ providedIn: 'root' })
export class CommunityStore {
  private readonly api = inject(CommunityApiService);

  private readonly _posts   = signal<PostSummaryDto[]>([]);
  private readonly _total   = signal(0);
  private readonly _loading = signal(false);
  private readonly _page    = signal(1);
  readonly limit = 12;

  readonly posts   = computed(() => this._posts());
  readonly total   = computed(() => this._total());
  readonly loading = computed(() => this._loading());
  readonly page    = computed(() => this._page());
  readonly hasMore = computed(() => this._posts().length < this._total());

  load(params: { page?: number; status?: string; search?: string } = {}): void {
    this._loading.set(true);
    const page = params.page ?? 1;
    this._page.set(page);
    this.api.list({ page, limit: this.limit, status: params.status, search: params.search })
      .subscribe({
        next: r => {
          this._posts.set(r.data);
          this._total.set(r.pagination.total);
          this._loading.set(false);
        },
        error: () => this._loading.set(false),
      });
  }

  remove(id: string): void {
    this._posts.update(list => list.filter(p => p.id !== id));
    this._total.update(t => t - 1);
  }

  upsertSummary(post: PostSummaryDto): void {
    this._posts.update(list => {
      const idx = list.findIndex(p => p.id === post.id);
      return idx >= 0 ? list.with(idx, post) : [post, ...list];
    });
  }
}
