import { computed, inject, Injectable, signal } from '@angular/core';
import { Subject, switchMap, tap, catchError, EMPTY } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ContributionCharge, ContributionStats, ContributionType, PagedChargesResult } from '@models/contribution.model';
import { PaginationMeta } from '@shared/models/pagination.model';
import { ContributionsApiService } from '../services/contributions-api.service';

interface ContributionsState {
  types: ContributionType[];
  charges: ContributionCharge[];
  stats: ContributionStats | null;
  pagination: PaginationMeta;
  loading: boolean;
  error: string | null;
  statusFilter: string;
  typeFilter: string;
}

@Injectable({ providedIn: 'root' })
export class ContributionsStore {
  private readonly api = inject(ContributionsApiService);

  private readonly _state = signal<ContributionsState>({
    types: [],
    charges: [],
    stats: null,
    pagination: { page: 1, limit: 20, total: 0 },
    loading: false,
    error: null,
    statusFilter: '',
    typeFilter: '',
  });

  readonly types = computed(() => this._state().types);
  readonly charges = computed(() => this._state().charges);
  readonly stats = computed(() => this._state().stats);
  readonly page = computed(() => this._state().pagination.page);
  readonly limit = computed(() => this._state().pagination.limit);
  readonly total = computed(() => this._state().pagination.total);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);
  readonly statusFilter = computed(() => this._state().statusFilter);
  readonly typeFilter = computed(() => this._state().typeFilter);
  readonly totalPages = computed(() => Math.ceil(this._state().pagination.total / this._state().pagination.limit));
  readonly activeTypes = computed(() => this._state().types.filter(t => t.is_active));

  private readonly typesTrigger$ = new Subject<void>();
  private readonly chargesTrigger$ = new Subject<{ page?: number; status?: string; typeId?: string; memberId?: string }>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.typesTrigger$.pipe(
      switchMap(() =>
        this.api.getTypes().pipe(
          tap((types: ContributionType[]) => this._state.update(s => ({ ...s, types }))),
          catchError(() => EMPTY)
        )
      ),
      takeUntilDestroyed(),
    ).subscribe();

    this.chargesTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(params => {
        const state = this._state();
        const status = params.status ?? state.statusFilter;
        const typeId = params.typeId ?? state.typeFilter;
        const page = params.page ?? state.pagination.page;
        const memberId = params.memberId;
        return this.api.getCharges(page, state.pagination.limit, memberId, typeId || undefined, status || undefined).pipe(
          tap((res: PagedChargesResult) =>
            this._state.update(s => ({
              ...s,
              charges: res.data,
              pagination: res.pagination,
              loading: false,
            }))
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
          tap((stats: ContributionStats) => this._state.update(s => ({ ...s, stats }))),
          catchError(() => EMPTY)
        )
      ),
      takeUntilDestroyed(),
    ).subscribe();
  }

  loadTypes(): void { this.typesTrigger$.next(); }
  loadCharges(params: { page?: number; status?: string; typeId?: string; memberId?: string }): void { this.chargesTrigger$.next(params); }
  loadStats(): void { this.statsTrigger$.next(); }

  setStatusFilter(statusFilter: string): void {
    this._state.update(s => ({ ...s, statusFilter, pagination: { ...s.pagination, page: 1 } }));
  }

  setTypeFilter(typeFilter: string): void {
    this._state.update(s => ({ ...s, typeFilter, pagination: { ...s.pagination, page: 1 } }));
  }
}
