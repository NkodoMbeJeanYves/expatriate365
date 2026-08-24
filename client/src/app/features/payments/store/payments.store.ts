import { computed, inject, Injectable, signal } from '@angular/core';
import { Subject, switchMap, tap, catchError, EMPTY } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Payment, PagedPaymentsResult, PaymentStats } from '@models/payment.model';
import { PaginationMeta } from '@shared/models/pagination.model';
import { PaymentsApiService } from '../services/payments-api.service';
import { AuthStore } from '@core/auth/auth.store';

interface PaymentsState {
  payments: Payment[];
  stats: PaymentStats | null;
  pagination: PaginationMeta;
  loading: boolean;
  error: string | null;
  statusFilter: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentsStore {
  private readonly api = inject(PaymentsApiService);
  private readonly authStore = inject(AuthStore);

  /** entity_id of the caller when they are not a board_member; undefined otherwise */
  private readonly _ownMemberId = computed(() => {
    const u = this.authStore.user();
    return u?.entity_type !== 'board_member' ? u?.entity_id : undefined;
  });

  /** true when the caller can see all members' data */
  readonly isBoardMember = computed(() => this.authStore.user()?.entity_type === 'board_member');

  private readonly _state = signal<PaymentsState>({
    payments: [],
    stats: null,
    pagination: { page: 1, limit: 20, total: 0 },
    loading: false,
    error: null,
    statusFilter: '',
  });

  readonly payments = computed(() => this._state().payments);
  readonly stats = computed(() => this._state().stats);
  readonly page = computed(() => this._state().pagination.page);
  readonly limit = computed(() => this._state().pagination.limit);
  readonly total = computed(() => this._state().pagination.total);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);
  readonly statusFilter = computed(() => this._state().statusFilter);
  readonly totalPages = computed(() => Math.ceil(this._state().pagination.total / this._state().pagination.limit));

  private readonly paymentsTrigger$ = new Subject<{ page?: number; status?: string }>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.paymentsTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(params => {
        const state = this._state();
        const status = params.status ?? state.statusFilter;
        const page = params.page ?? state.pagination.page;
        const memberId = this._ownMemberId();
        return this.api.getPayments(page, state.pagination.limit, memberId, status || undefined).pipe(
          tap((res: PagedPaymentsResult) =>
            this._state.update(s => ({ ...s, payments: res.data, pagination: res.pagination, loading: false }))
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
          tap((stats: PaymentStats) => this._state.update(s => ({ ...s, stats }))),
          catchError(() => EMPTY)
        )
      ),
      takeUntilDestroyed(),
    ).subscribe();
  }

  loadPayments(params: { page?: number; status?: string }): void { this.paymentsTrigger$.next(params); }
  loadStats(): void { this.statsTrigger$.next(); }

  setStatusFilter(statusFilter: string): void {
    this._state.update(s => ({ ...s, statusFilter, pagination: { ...s.pagination, page: 1 } }));
  }
}
