import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { ElectionDto, ElectionFilters, ElectionStatsDto } from '@models/election.model';
import { ElectionsApiService } from '../services/elections-api.service';

interface ElectionsState {
  elections: ElectionDto[];
  pagination: PaginationMeta;
  stats: ElectionStatsDto | null;
  filters: ElectionFilters;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class ElectionsStore {
  private readonly api = inject(ElectionsApiService);
  private readonly _state = signal<ElectionsState>({
    elections: [],
    pagination: { page: 1, limit: 12, total: 0 },
    stats: null,
    filters: { page: 1, limit: 12 },
    loading: false,
    error: null,
  });

  readonly elections = computed(() => this._state().elections);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly filters = computed(() => this._state().filters);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<ElectionFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(filters => this.api.list(filters).pipe(
        tap(res => this._state.update(s => ({ ...s, elections: res.data, pagination: res.pagination, loading: false }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement des élections.' }));
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

  loadElections(filters?: Partial<ElectionFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsertElection(e: ElectionDto): void {
    this._state.update(s => {
      const idx = s.elections.findIndex(x => x.id === e.id);
      const elections = idx >= 0 ? s.elections.map(x => x.id === e.id ? e : x) : [e, ...s.elections];
      return { ...s, elections };
    });
  }
}
