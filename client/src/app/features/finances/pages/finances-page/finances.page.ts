import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AppPaginatorComponent, PageChangeEvent } from '@shared/components/paginator/app-paginator.component';
import { FinanceSummaryDto, FinanceTransactionDto } from '@models/finance.model';
import { FinancesApiService } from '../../services/finances-api.service';

@Component({
  selector: 'app-finances-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppCurrencyPipe, SlicePipe, FormsModule, ButtonModule, TagModule,
    SelectModule, ProgressSpinnerModule, AppPaginatorComponent, TranslatePipe,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'finances.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'finances.subtitle' | translate }}</p>
        </div>
        <p-button icon="pi pi-refresh" severity="secondary" (onClick)="loadAll()" />
      </div>

      <!-- KPIs -->
      @if (summary()) {
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ summary()!.total_collected | appCurrency }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'finances.collected' | translate }}</div>
          </div>
          <div class="bg-yellow-50 rounded-xl p-4 border border-yellow-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-yellow-700">{{ summary()!.total_expected | appCurrency }}</div>
            <div class="text-xs text-yellow-600 mt-1">{{ 'finances.expected' | translate }}</div>
          </div>
          <div class="rounded-xl p-4 border shadow-sm text-center"
            [class]="summary()!.balance >= 0 ? 'bg-blue-50 border-blue-100' : 'bg-red-50 border-red-100'">
            <div class="text-2xl font-bold" [class]="summary()!.balance >= 0 ? 'text-blue-700' : 'text-red-600'">
              {{ summary()!.balance | appCurrency }}
            </div>
            <div class="text-xs mt-1" [class]="summary()!.balance >= 0 ? 'text-blue-600' : 'text-red-500'">{{ 'finances.balance' | translate }}</div>
          </div>
          <div class="bg-indigo-50 rounded-xl p-4 border border-indigo-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-indigo-700">{{ summary()!.collection_rate }}%</div>
            <div class="text-xs text-indigo-600 mt-1">{{ 'finances.collection_rate' | translate }}</div>
          </div>
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-700">{{ summary()!.total_transactions }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'finances.transactions' | translate }}</div>
          </div>
        </div>

        <!-- Barre de progression -->
        <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4">
          <div class="flex justify-between text-sm text-gray-600 mb-2">
            <span>{{ 'finances.collection_progress' | translate }}</span>
            <strong>{{ summary()!.collection_rate }}%</strong>
          </div>
          <div class="h-3 bg-gray-100 rounded-full overflow-hidden">
            <div class="h-full bg-green-500 rounded-full transition-all"
              [style.width.%]="summary()!.collection_rate"></div>
          </div>
        </div>
      }

      <!-- Filtres transactions -->
      <div class="flex gap-3 flex-wrap items-center">
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          [placeholder]="'finances.all_types' | translate" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          [placeholder]="'finances.all_statuses' | translate" [showClear]="true" (onChange)="applyFilters()" />
        <div class="flex items-center gap-2 text-sm text-gray-500">
          <label>{{ 'finances.date_from' | translate }}</label>
          <input type="date" [(ngModel)]="filterFrom" (change)="applyFilters()"
            class="border border-gray-200 rounded-lg px-2 py-1 text-sm" />
          <label>{{ 'finances.date_to' | translate }}</label>
          <input type="date" [(ngModel)]="filterTo" (change)="applyFilters()"
            class="border border-gray-200 rounded-lg px-2 py-1 text-sm" />
        </div>
      </div>

      <!-- Tableau transactions -->
      @if (txLoading()) {
        <div class="flex justify-center py-8"><p-progressspinner strokeWidth="4" /></div>
      } @else if (transactions().length === 0) {
        <div class="text-center py-12 text-gray-400">
          <i class="pi pi-list text-4xl mb-3 block"></i>
          <p>{{ 'finances.no_transactions' | translate }}</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-gray-500 text-xs uppercase">
              <tr>
                <th class="px-4 py-3 text-left">{{ 'finances.member' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'common.type' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'common.date' | translate }}</th>
                <th class="px-4 py-3 text-right">{{ 'common.amount' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'common.status' | translate }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-50">
              @for (tx of transactions(); track tx.id) {
                <tr class="hover:bg-gray-50">
                  <td class="px-4 py-3">
                    <div class="font-medium text-gray-800">{{ tx.member_name }}</div>
                    <div class="text-xs text-gray-400">{{ tx.membership_number }}</div>
                  </td>
                  <td class="px-4 py-3">
                    <p-tag [value]="tx.type" [severity]="tx.type === 'welfare' ? 'warn' : 'info'" />
                  </td>
                  <td class="px-4 py-3 text-gray-500">{{ tx.date | slice:0:10 }}</td>
                  <td class="px-4 py-3 text-right font-mono"
                    [class]="tx.amount < 0 ? 'text-red-600' : 'text-green-700'">
                    {{ tx.amount | appCurrency }}
                  </td>
                  <td class="px-4 py-3">
                    <p-tag [value]="tx.status"
                      [severity]="tx.status === 'confirmed' || tx.status === 'paid' ? 'success'
                        : tx.status === 'pending' ? 'warn' : 'secondary'" />
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <app-paginator [page]="currentPage" [limit]="25" [total]="totalRecords()" (pageChange)="onPageChange($event)" />
      }
    </div>
  `,
})
export class FinancesPage implements OnInit {
  private readonly api = inject(FinancesApiService);
  private readonly translate = inject(TranslateService);

  readonly summary = signal<FinanceSummaryDto | null>(null);
  readonly transactions = signal<FinanceTransactionDto[]>([]);
  readonly totalRecords = signal(0);
  readonly txLoading = signal(false);

  filterType: string | null = null;
  filterStatus: string | null = null;
  filterFrom = '';
  filterTo = '';
  protected currentPage = 1;

  get typeOptions() {
    return [
      { label: this.translate.instant('finances.type_contribution'), value: 'contribution' },
      { label: this.translate.instant('finances.type_welfare'), value: 'welfare' },
    ];
  }

  get statusOptions() {
    return [
      { label: this.translate.instant('finances.status_confirmed'), value: 'confirmed' },
      { label: this.translate.instant('finances.status_pending'), value: 'pending' },
      { label: this.translate.instant('finances.status_cancelled'), value: 'cancelled' },
    ];
  }

  ngOnInit(): void { this.loadAll(); }

  loadAll(): void {
    this.api.summary().subscribe(s => this.summary.set(s));
    this.loadTransactions();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadTransactions();
  }

  loadTransactions(): void {
    this.txLoading.set(true);
    this.api.transactions({
      page: this.currentPage,
      limit: 25,
      type: this.filterType ?? undefined,
      status: this.filterStatus ?? undefined,
      from: this.filterFrom || undefined,
      to: this.filterTo || undefined,
    }).subscribe({
      next: res => {
        this.transactions.set(res.data);
        this.totalRecords.set(res.pagination.total);
        this.txLoading.set(false);
      },
      error: () => this.txLoading.set(false),
    });
  }

  onPageChange(event: PageChangeEvent): void {
    this.currentPage = event.page;
    this.loadTransactions();
  }
}
