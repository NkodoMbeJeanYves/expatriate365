import { computed, inject, Injectable, signal } from '@angular/core';
import { Subject, switchMap, tap, catchError, EMPTY } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { WelfareRequest, WelfareStats, PagedWelfareResult } from '@models/welfare.model';
import { PaginationMeta } from '@shared/models/pagination.model';
import { WelfareApiService } from '../services/welfare-api.service';

interface WelfareState {
  requests: WelfareRequest[];
  stats: WelfareStats | null;
  pagination: PaginationMeta;
  loading: boolean;
  error: string | null;
  statusFilter: string;
  typeFilter: string;
}

@Injectable({ providedIn: 'root' })
export class WelfareStore {
  private readonly api = inject(WelfareApiService);

  private readonly _state = signal<WelfareState>({
    requests: [],
    stats: null,
    pagination: { page: 1, limit: 20, total: 0 },
    loading: false,
    error: null,
    statusFilter: '',
    typeFilter: '',
  });

  readonly requests = computed(() => this._state().requests);
  readonly stats = computed(() => this._state().stats);
  readonly page = computed(() => this._state().pagination.page);
  readonly limit = computed(() => this._state().pagination.limit);
  readonly total = computed(() => this._state().pagination.total);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);
  readonly statusFilter = computed(() => this._state().statusFilter);
  readonly typeFilter = computed(() => this._state().typeFilter);
  readonly totalPages = computed(() => Math.ceil(this._state().pagination.total / this._state().pagination.limit));

  private readonly loadTrigger$ = new Subject<{ page?: number; status?: string; type?: string }>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(params => {
        const state = this._state();
        const status = params.status ?? state.statusFilter;
        const type = params.type ?? state.typeFilter;
        const page = params.page ?? state.pagination.page;
        return this.api.getRequests(page, state.pagination.limit, status || undefined, type || undefined).pipe(
          tap((res: PagedWelfareResult) =>
            this._state.update(s => ({ ...s, requests: res.data, pagination: res.pagination, loading: false }))
          ),
          catchError((err) => {
            this._state.update(s => ({ ...s, loading: false, error: err?.error?.error ?? 'Erreur de chargement' }));
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(),
    ).subscribe();

    this.statsTrigger$.pipe(
      switchMap(() =>
        this.api.getStats().pipe(
          tap((stats: WelfareStats) => this._state.update(s => ({ ...s, stats }))),
          catchError(() => EMPTY)
        )
      ),
      takeUntilDestroyed(),
    ).subscribe();
  }

  loadRequests(params: { page?: number; status?: string; type?: string } = {}): void {
    this.loadTrigger$.next(params);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  setStatusFilter(statusFilter: string): void {
    this._state.update(s => ({ ...s, statusFilter, pagination: { ...s.pagination, page: 1 } }));
  }

  setTypeFilter(typeFilter: string): void {
    this._state.update(s => ({ ...s, typeFilter, pagination: { ...s.pagination, page: 1 } }));
  }
}
