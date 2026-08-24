import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { MeetingDto, MeetingFilters, MeetingStatsDto } from '@models/meeting.model';
import { MeetingsApiService } from '../services/meetings-api.service';

interface MeetingsState {
  meetings: MeetingDto[];
  pagination: PaginationMeta;
  stats: MeetingStatsDto | null;
  filters: MeetingFilters;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class MeetingsStore {
  private readonly api = inject(MeetingsApiService);
  private readonly _state = signal<MeetingsState>({
    meetings: [],
    pagination: { page: 1, limit: 12, total: 0 },
    stats: null,
    filters: { page: 1, limit: 12 },
    loading: false,
    error: null,
  });

  readonly meetings = computed(() => this._state().meetings);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly filters = computed(() => this._state().filters);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<MeetingFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(filters => this.api.list(filters).pipe(
        tap(res => this._state.update(s => ({
          ...s, meetings: res.data, pagination: res.pagination, loading: false,
        }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement des réunions.' }));
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

  loadMeetings(filters?: Partial<MeetingFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsertMeeting(m: MeetingDto): void {
    this._state.update(s => {
      const idx = s.meetings.findIndex(x => x.id === m.id);
      const meetings = idx >= 0 ? s.meetings.map(x => x.id === m.id ? m : x) : [m, ...s.meetings];
      return { ...s, meetings };
    });
  }
}
