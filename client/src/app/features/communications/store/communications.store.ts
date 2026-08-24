import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { CommunicationDto, CommunicationFilters, CommunicationStatsDto } from '@models/communication.model';
import { CommunicationsApiService } from '../services/communications-api.service';

interface CommunicationsState {
  communications: CommunicationDto[];
  pagination: PaginationMeta;
  stats: CommunicationStatsDto | null;
  filters: CommunicationFilters;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class CommunicationsStore {
  private readonly api = inject(CommunicationsApiService);
  private readonly _state = signal<CommunicationsState>({
    communications: [],
    pagination: { page: 1, limit: 20, total: 0 },
    stats: null,
    filters: { page: 1, limit: 20 },
    loading: false,
    error: null,
  });

  readonly communications = computed(() => this._state().communications);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly filters = computed(() => this._state().filters);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<CommunicationFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(filters => this.api.list(filters).pipe(
        tap(res => this._state.update(s => ({
          ...s, communications: res.data, pagination: res.pagination, loading: false,
        }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement des communications.' }));
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

  load(filters?: Partial<CommunicationFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsert(c: CommunicationDto): void {
    this._state.update(s => {
      const idx = s.communications.findIndex(x => x.id === c.id);
      const communications = idx >= 0
        ? s.communications.map(x => x.id === c.id ? c : x)
        : [c, ...s.communications];
      return { ...s, communications };
    });
  }
}
