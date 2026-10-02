import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { AppPaginatorComponent, PageChangeEvent } from '@shared/components/paginator/app-paginator.component';
import { ExpenseDto, ExpenseStatsDto, CreateExpenseRequest } from '@models/finance.model';
import { FinancesApiService } from '../../services/finances-api.service';
import { AuthStore } from '@core/auth/auth.store';
import { STAFF_ROLES } from '@core/auth/models/role.model';

@Component({
  selector: 'app-expenses-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService, MessageService],
  imports: [
    FormsModule, ButtonModule, TagModule, SelectModule, DialogModule,
    InputTextModule, TextareaModule, ConfirmDialogModule, ToastModule,
    AppCurrencyPipe, AppPaginatorComponent, TranslatePipe,
  ],
  template: `
    <p-toast />
    <p-confirmdialog />

    <div class="p-6 flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'expenses.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'expenses.subtitle' | translate }}</p>
        </div>
        @if (isStaff()) {
          <p-button icon="pi pi-plus" [label]="'expenses.new' | translate" (onClick)="openCreate()" />
        }
      </div>

      <!-- Stats -->
      @if (stats()) {
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-red-50 rounded-xl p-4 border border-red-100 text-center">
            <div class="text-2xl font-bold text-red-700">{{ stats()!.total_amount | appCurrency }}</div>
            <div class="text-xs text-red-600 mt-1">{{ 'expenses.total_validated' | translate }}</div>
          </div>
          <div class="bg-white rounded-xl p-4 border border-gray-100 text-center">
            <div class="text-2xl font-bold text-gray-700">{{ stats()!.total_count }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'expenses.total' | translate }}</div>
          </div>
          <div class="bg-yellow-50 rounded-xl p-4 border border-yellow-100 text-center">
            <div class="text-2xl font-bold text-yellow-700">{{ stats()!.pending_count }}</div>
            <div class="text-xs text-yellow-600 mt-1">{{ 'expenses.pending' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 text-center">
            <div class="text-2xl font-bold text-green-700">{{ stats()!.validated_count }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'expenses.validated' | translate }}</div>
          </div>
          <div class="bg-gray-50 rounded-xl p-4 border border-gray-100 text-center">
            <div class="text-2xl font-bold text-gray-500">{{ stats()!.rejected_count }}</div>
            <div class="text-xs text-gray-400 mt-1">{{ 'expenses.rejected' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          [placeholder]="'expenses.all_statuses' | translate" [showClear]="true" (onChange)="loadExpenses()" />
        <p-select [options]="categoryOptions" [(ngModel)]="filterCategory" optionLabel="label" optionValue="value"
          [placeholder]="'expenses.all_categories' | translate" [showClear]="true" (onChange)="loadExpenses()" />
      </div>

      <!-- Table -->
      @if (loading()) {
        <div class="flex justify-center py-8"><i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i></div>
      } @else if (expenses().length === 0) {
        <div class="text-center py-12 text-gray-400">
          <i class="pi pi-wallet text-4xl mb-3 block"></i>
          <p>{{ 'expenses.empty' | translate }}</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 text-gray-500 text-xs uppercase">
                <tr>
                  <th class="px-4 py-3 text-left">{{ 'expenses.label' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'expenses.category' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'common.date' | translate }}</th>
                  <th class="px-4 py-3 text-right">{{ 'common.amount' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'common.status' | translate }}</th>
                  @if (isStaff()) {
                    <th class="px-4 py-3"></th>
                  }
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-50">
                @for (exp of expenses(); track exp.id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3">
                      <div class="font-medium text-gray-800">{{ exp.label }}</div>
                      @if (exp.description) {
                        <div class="text-xs text-gray-400">{{ exp.description }}</div>
                      }
                    </td>
                    <td class="px-4 py-3">
                      <p-tag [value]="('expenses.cat_' + exp.category) | translate" severity="secondary" />
                    </td>
                    <td class="px-4 py-3 text-gray-500">{{ exp.date | slice:0:10 }}</td>
                    <td class="px-4 py-3 text-right font-mono text-red-600 font-semibold">
                      {{ exp.amount | appCurrency }}
                    </td>
                    <td class="px-4 py-3">
                      <p-tag [value]="('expenses.status_' + exp.status) | translate"
                        [severity]="exp.status === 'validated' ? 'success' : exp.status === 'pending' ? 'warn' : 'secondary'" />
                    </td>
                    @if (isStaff()) {
                      <td class="px-4 py-3">
                        <div class="flex gap-1 justify-end">
                          @if (exp.status === 'pending') {
                            <p-button icon="pi pi-check" severity="success" size="small"
                              [pTooltip]="'expenses.validate' | translate"
                              (onClick)="validateExpense(exp)" [loading]="actingId() === exp.id" />
                            <p-button icon="pi pi-times" severity="danger" size="small"
                              [pTooltip]="'expenses.reject' | translate"
                              (onClick)="rejectExpense(exp)" [loading]="actingId() === exp.id" />
                          }
                          <p-button icon="pi pi-pencil" severity="secondary" size="small"
                            (onClick)="openEdit(exp)" />
                          <p-button icon="pi pi-trash" severity="danger" size="small"
                            (onClick)="confirmDelete(exp)" />
                        </div>
                      </td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
        <app-paginator [page]="currentPage" [limit]="20" [total]="total()" (pageChange)="onPageChange($event)" />
      }
    </div>

    <!-- Dialog create/edit -->
    <p-dialog [(visible)]="dialogVisible" [header]="editingId() ? ('expenses.edit' | translate) : ('expenses.new' | translate)"
      [modal]="true" [style]="{ width: '480px' }" [closable]="true">
      <div class="flex flex-col gap-4 pt-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700">{{ 'expenses.label' | translate }} *</label>
          <input pInputText [(ngModel)]="form.label" class="w-full" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700">{{ 'common.description' | translate }}</label>
          <textarea pTextarea [(ngModel)]="form.description" rows="3" class="w-full"></textarea>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-gray-700">{{ 'expenses.category' | translate }}</label>
            <p-select [options]="categoryOptions" [(ngModel)]="form.category" optionLabel="label" optionValue="value" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-gray-700">{{ 'common.amount' | translate }} *</label>
            <input pInputText type="number" [(ngModel)]="form.amount" class="w-full" />
          </div>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium text-gray-700">{{ 'common.date' | translate }} *</label>
          <input type="date" [(ngModel)]="form.date" class="border border-gray-200 rounded-lg px-3 py-2 text-sm w-full" />
        </div>
      </div>
      <ng-template pTemplate="footer">
        <p-button [label]="'common.cancel' | translate" severity="secondary" (onClick)="dialogVisible = false" />
        <p-button [label]="'common.save' | translate" (onClick)="save()" [loading]="saving()" />
      </ng-template>
    </p-dialog>
  `,
})
export class ExpensesPage implements OnInit {
  private readonly api     = inject(FinancesApiService);
  private readonly auth    = inject(AuthStore);
  private readonly confirm = inject(ConfirmationService);
  private readonly toast   = inject(MessageService);
  private readonly tr      = inject(TranslateService);

  readonly expenses  = signal<ExpenseDto[]>([]);
  readonly stats     = signal<ExpenseStatsDto | null>(null);
  readonly total     = signal(0);
  readonly loading   = signal(false);
  readonly saving    = signal(false);
  readonly actingId  = signal<string | null>(null);

  readonly isStaff = signal(false);

  filterStatus: string | null = null;
  filterCategory: string | null = null;
  currentPage = 1;
  dialogVisible = false;
  editingId = signal<string | null>(null);

  form: CreateExpenseRequest = { label: '', category: 'other', amount: 0, currency: 'EUR', date: '' };

  get statusOptions() {
    return ['pending', 'validated', 'rejected'].map(v => ({
      label: this.tr.instant('expenses.status_' + v), value: v,
    }));
  }

  get categoryOptions() {
    return ['travel', 'supplies', 'communication', 'event', 'other'].map(v => ({
      label: this.tr.instant('expenses.cat_' + v), value: v,
    }));
  }

  ngOnInit(): void {
    this.isStaff.set(this.auth.hasAnyRole(STAFF_ROLES));
    this.loadAll();
  }

  loadAll(): void {
    this.api.expenseStats().subscribe(s => this.stats.set(s));
    this.loadExpenses();
  }

  loadExpenses(): void {
    this.loading.set(true);
    this.api.listExpenses(
      this.currentPage, 20,
      this.filterStatus ?? undefined,
      this.filterCategory ?? undefined,
    ).subscribe({
      next: res => {
        this.expenses.set(res.data);
        this.total.set(res.pagination.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPageChange(e: PageChangeEvent): void {
    this.currentPage = e.page;
    this.loadExpenses();
  }

  openCreate(): void {
    this.editingId.set(null);
    this.form = { label: '', category: 'other', amount: 0, currency: 'EUR', date: new Date().toISOString().slice(0, 10) };
    this.dialogVisible = true;
  }

  openEdit(exp: ExpenseDto): void {
    this.editingId.set(exp.id);
    this.form = { label: exp.label, description: exp.description, category: exp.category, amount: exp.amount, currency: exp.currency, date: exp.date };
    this.dialogVisible = true;
  }

  save(): void {
    if (!this.form.label || !this.form.amount || !this.form.date) return;
    this.saving.set(true);
    const obs = this.editingId()
      ? this.api.updateExpense(this.editingId()!, this.form)
      : this.api.createExpense(this.form);
    obs.subscribe({
      next: () => {
        this.dialogVisible = false;
        this.saving.set(false);
        this.loadAll();
        this.toast.add({ severity: 'success', summary: this.tr.instant('common.saved'), life: 3000 });
      },
      error: () => this.saving.set(false),
    });
  }

  validateExpense(exp: ExpenseDto): void {
    this.actingId.set(exp.id);
    this.api.validateExpense(exp.id).subscribe({
      next: () => { this.actingId.set(null); this.loadAll(); },
      error: () => this.actingId.set(null),
    });
  }

  rejectExpense(exp: ExpenseDto): void {
    this.actingId.set(exp.id);
    this.api.rejectExpense(exp.id).subscribe({
      next: () => { this.actingId.set(null); this.loadAll(); },
      error: () => this.actingId.set(null),
    });
  }

  confirmDelete(exp: ExpenseDto): void {
    this.confirm.confirm({
      message: `${this.tr.instant('expenses.delete_confirm')} "${exp.label}" ?`,
      accept: () => {
        this.api.deleteExpense(exp.id).subscribe({
          next: () => this.loadAll(),
        });
      },
    });
  }
}
