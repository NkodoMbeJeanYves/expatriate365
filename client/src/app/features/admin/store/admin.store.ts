import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { AdminUserDto, AdminStatsDto } from '@models/admin.model';
import { AdminApiService } from '../services/admin-api.service';

interface AdminFilters { page: number; limit: number; role?: string; status?: string; }

interface AdminState {
  users: AdminUserDto[];
  pagination: PaginationMeta;
  stats: AdminStatsDto | null;
  filters: AdminFilters;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdminStore {
  private readonly api = inject(AdminApiService);
  private readonly _state = signal<AdminState>({
    users: [],
    pagination: { page: 1, limit: 20, total: 0 },
    stats: null,
    filters: { page: 1, limit: 20 },
    loading: false,
    error: null,
  });

  readonly users = computed(() => this._state().users);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<AdminFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(f => this.api.listUsers(f.page, f.limit, f.role, f.status).pipe(
        tap(res => this._state.update(s => ({
          ...s, users: res.data, pagination: res.pagination, loading: false,
        }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur de chargement.' }));
          return EMPTY;
        }),
      )),
      takeUntilDestroyed(),
    ).subscribe();

    this.statsTrigger$.pipe(
      switchMap(() => this.api.stats().pipe(
        tap(stats => this._state.update(s => ({ ...s, stats }))),
        catchError(() => EMPTY),
      )),
      takeUntilDestroyed(),
    ).subscribe();
  }

  load(filters?: Partial<AdminFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsert(u: AdminUserDto): void {
    this._state.update(s => {
      const idx = s.users.findIndex(x => x.id === u.id);
      const users = idx >= 0 ? s.users.map(x => x.id === u.id ? u : x) : [u, ...s.users];
      return { ...s, users };
    });
  }
}
