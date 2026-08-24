import { computed, inject, Injectable, signal } from '@angular/core';
import { MemberFilters, MemberListItem, MembershipCategory, PagedMembersResult } from '@core/models/member.model';
import { PaginationMeta } from '@shared/models/pagination.model';
import { MembersApiService } from '../services/members-api.service';

interface MembersState {
  members: MemberListItem[];
  pagination: PaginationMeta;
  filters: MemberFilters;
  categories: MembershipCategory[];
  loading: boolean;
  error: string | null;
}

const DEFAULT_FILTERS: MemberFilters = { page: 1, limit: 20 };

@Injectable({ providedIn: 'root' })
export class MembersStore {
  private readonly api = inject(MembersApiService);

  private readonly _state = signal<MembersState>({
    members: [],
    pagination: { page: 1, limit: 20, total: 0 },
    filters: DEFAULT_FILTERS,
    categories: [],
    loading: false,
    error: null,
  });

  readonly members    = computed(() => this._state().members);
  readonly pagination = computed(() => this._state().pagination);
  readonly filters    = computed(() => this._state().filters);
  readonly categories = computed(() => this._state().categories);
  readonly loading    = computed(() => this._state().loading);
  readonly error      = computed(() => this._state().error);
  readonly totalPages = computed(() => Math.ceil(this._state().pagination.total / this._state().pagination.limit));
  readonly hasMembers = computed(() => this._state().members.length > 0);

  loadMembers(filters: MemberFilters): void {
    this._state.update(s => ({ ...s, loading: true, error: null }));
    this.api.list(filters).subscribe({
      next: (res: PagedMembersResult) =>
        this._state.update(s => ({ ...s, members: res.data ?? [], pagination: res.pagination, filters, loading: false })),
      error: (err) => {
        console.error('[MembersStore] loadMembers error', err);
        this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement.' }));
      },
    });
  }

  loadCategories(): void {
    this.api.categories().subscribe({
      next: (categories: MembershipCategory[]) =>
        this._state.update(s => ({ ...s, categories })),
      error: (err) => console.error('[MembersStore] loadCategories error', err),
    });
  }

  setFilters(partial: Partial<MemberFilters>): void {
    const filters = { ...this._state().filters, ...partial, page: partial.page ?? 1 };
    this._state.update(s => ({ ...s, filters }));
  }
}
