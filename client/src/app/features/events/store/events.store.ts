import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { EventDto, EventFilters, EventStatsDto } from '@models/event.model';
import { EventsApiService } from '../services/events-api.service';

interface EventsState {
  events: EventDto[];
  pagination: PaginationMeta;
  stats: EventStatsDto | null;
  filters: EventFilters;
  loading: boolean;
  error: string | null;
}

const DEFAULT_FILTERS: EventFilters = { page: 1, limit: 12 };

@Injectable({ providedIn: 'root' })
export class EventsStore {
  private readonly api = inject(EventsApiService);
  private readonly _state = signal<EventsState>({
    events: [],
    pagination: { page: 1, limit: 12, total: 0 },
    stats: null,
    filters: DEFAULT_FILTERS,
    loading: false,
    error: null,
  });

  readonly events = computed(() => this._state().events);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly filters = computed(() => this._state().filters);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<EventFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(filters => this.api.list(filters).pipe(
        tap(res => this._state.update(s => ({
          ...s, events: res.data, pagination: res.pagination, loading: false,
        }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement des événements.' }));
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

  loadEvents(filters?: Partial<EventFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsertEvent(ev: EventDto): void {
    this._state.update(s => {
      const idx = s.events.findIndex(e => e.id === ev.id);
      const events = idx >= 0 ? s.events.map(e => e.id === ev.id ? ev : e) : [ev, ...s.events];
      return { ...s, events };
    });
  }
}
