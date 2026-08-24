import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { AuthStore } from '@core/auth/auth.store';
import { PERMISSIONS } from '@core/auth/models/permission.model';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { PaymentsStore } from '../../store/payments.store';
import { PaymentsApiService } from '../../services/payments-api.service';
import { PaymentFormDrawerComponent } from '../../components/payment-form-drawer/payment-form-drawer.component';
import { PaymentReceiptComponent } from '../../components/payment-receipt/payment-receipt.component';
import { Payment, PAYMENT_METHODS } from '@models/payment.model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-payment-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppCurrencyPipe, FormsModule,
    DialogModule, SelectModule,
    PaymentFormDrawerComponent, PaymentReceiptComponent, TranslatePipe,
  ],
  template: `
    <div class="min-h-full bg-gray-50 p-4 md:p-8 space-y-6">

      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ 'payments.title' | translate }}</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ 'payments.subtitle' | translate }}</p>
        </div>
        <button (click)="openForm()"
          class="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-sm font-medium shadow-sm transition-colors">
          <i class="pi pi-plus text-sm"></i>
          {{ 'payments.new' | translate }}
        </button>
      </div>

      <!-- Stats -->
      @if (store.stats()) {
        <div class="grid grid-cols-2 lg:grid-cols-3 gap-4">
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'payments.status_confirmed' | translate }}</p>
            <p class="text-xl font-bold text-green-600 mt-1">{{ store.stats()!.total_confirmed | appCurrency }}</p>
            <p class="text-xs text-gray-400 mt-1">· {{ store.stats()!.confirmed_count }} paiements</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'payments.status_pending' | translate }}</p>
            <p class="text-xl font-bold text-yellow-600 mt-1">{{ store.stats()!.total_pending | appCurrency }}</p>
            <p class="text-xs text-gray-400 mt-1">· {{ store.stats()!.pending_count }} paiements</p>
          </div>
          <div class="col-span-2 lg:col-span-1 bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Total transactions</p>
            <p class="text-xl font-bold text-gray-900 mt-1">{{ store.stats()!.total_count }}</p>
            <p class="text-xs text-gray-400 mt-1">{{ store.stats()!.reversed_count }} annulés</p>
          </div>
        </div>
      }

      <!-- Table -->
      <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <!-- Filters -->
        <div class="p-4 border-b border-gray-100 flex flex-col sm:flex-row gap-3">
          <p-select [options]="statusOptions" [(ngModel)]="selectedStatus" (onChange)="onStatusChange()"
            optionLabel="label" optionValue="value" placeholder="Tous les statuts" styleClass="w-full sm:w-48" />
        </div>

        <!-- Desktop table -->
        <div class="hidden md:block overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider">
              <tr>
                <th class="text-left px-4 py-3">{{ 'payments.receipt_number' | translate }}</th>
                @if (isBoardMember()) { <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th> }
                <th class="text-left px-4 py-3">{{ 'contributions.title' | translate }}</th>
                <th class="text-left px-4 py-3">{{ 'payments.method' | translate }}</th>
                <th class="text-right px-4 py-3">{{ 'common.amount' | translate }}</th>
                <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 bg-white">
              @if (store.loading()) {
                @for (i of [1,2,3,4,5]; track i) {
                  <tr><td colspan="7" class="px-4 py-3"><div class="h-8 bg-gray-100 rounded animate-pulse"></div></td></tr>
                }
              } @else if (!store.payments().length) {
                <tr><td colspan="7" class="text-center px-4 py-12 text-gray-400 text-sm">{{ 'payments.no_payments' | translate }}</td></tr>
              } @else {
                @for (p of store.payments(); track p.id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3">
                      <p class="text-sm font-mono font-medium text-gray-900">{{ p.receipt_number }}</p>
                      <p class="text-xs text-gray-400">{{ p.payment_date }}</p>
                    </td>
                    @if (isBoardMember()) {
                      <td class="px-4 py-3">
                        <p class="text-sm font-medium text-gray-900">{{ p.member_name }}</p>
                        <p class="text-xs text-gray-400">{{ p.membership_number }}</p>
                      </td>
                    }
                    <td class="px-4 py-3 text-sm text-gray-600">{{ p.contribution_type_name }}</td>
                    <td class="px-4 py-3 text-sm text-gray-600">{{ methodLabel(p.payment_method) }}</td>
                    <td class="px-4 py-3 text-right text-sm font-semibold text-gray-900">{{ p.amount | appCurrency }}</td>
                    <td class="px-4 py-3"><span [class]="statusClass(p.status)">{{ statusLabel(p.status) }}</span></td>
                    <td class="px-4 py-3 text-right">
                      <div class="flex gap-2 justify-end">
                        <button (click)="viewReceipt(p)" class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 hover:bg-gray-200 font-medium">
                          <i class="pi pi-file-pdf text-xs"></i>
                        </button>
                        @if (p.receipt_file_url) {
                          <a [href]="p.receipt_file_url" target="_blank" class="text-xs px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-600 hover:bg-blue-100 font-medium">
                            <i class="pi pi-paperclip text-xs"></i>
                          </a>
                        } @else {
                          <label class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 hover:bg-gray-200 font-medium cursor-pointer" [title]="'Uploader un reçu'">
                            <i class="pi pi-upload text-xs"></i>
                            <input type="file" class="hidden" accept=".pdf,.jpg,.jpeg,.png"
                              (change)="onReceiptFileSelected($event, p)" />
                          </label>
                        }
                        @if (p.status === 'pending' && isStaff()) {
                          <button (click)="confirm(p)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 font-medium">{{ 'common.confirm' | translate }}</button>
                          <button (click)="openReverse(p)" class="text-xs px-2.5 py-1.5 rounded-lg bg-red-50 text-red-600 hover:bg-red-100 font-medium">{{ 'common.cancel' | translate }}</button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        <!-- Mobile list -->
        <div class="md:hidden divide-y divide-gray-100">
          @for (p of store.payments(); track p.id) {
            <div class="p-4 space-y-2">
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="text-sm font-medium text-gray-900">{{ p.member_name }}</p>
                  <p class="text-xs text-gray-400 font-mono">{{ p.receipt_number }} · {{ p.payment_date }}</p>
                </div>
                <span [class]="statusClass(p.status)">{{ statusLabel(p.status) }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-sm font-semibold text-gray-900">{{ p.amount | appCurrency }}</span>
                <div class="flex gap-2">
                  <button (click)="viewReceipt(p)" class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 font-medium">Reçu</button>
                  @if (p.status === 'pending' && isStaff()) {
                    <button (click)="confirm(p)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 font-medium">{{ 'common.confirm' | translate }}</button>
                  }
                </div>
              </div>
            </div>
          } @empty {
            <p class="text-center py-12 text-gray-400 text-sm">{{ 'payments.no_payments' | translate }}</p>
          }
        </div>

        <!-- Pagination -->
        @if (store.total() > store.limit()) {
          <div class="p-4 border-t border-gray-100 flex items-center justify-between text-sm text-gray-500">
            <span>{{ store.total() }} paiements</span>
            <div class="flex gap-2">
              <button [disabled]="store.page() <= 1" (click)="changePage(store.page() - 1)"
                class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">Précédent</button>
              <span class="px-3 py-1.5">{{ store.page() }} / {{ store.totalPages() }}</span>
              <button [disabled]="store.page() >= store.totalPages()" (click)="changePage(store.page() + 1)"
                class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">Suivant</button>
            </div>
          </div>
        }
      </div>
    </div>

    <!-- Payment form drawer -->
    <app-payment-form-drawer #formDrawer (saved)="onSaved()" />

    <!-- Receipt dialog -->
    <p-dialog [(visible)]="receiptVisible" header="Reçu de paiement" [modal]="true" [style]="{ width: '480px' }" [closable]="true">
      <app-payment-receipt [payment]="selectedPayment()" />
      <ng-template pTemplate="footer">
        <button (click)="printReceipt()" class="inline-flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-xl text-sm font-medium hover:bg-gray-200">
          <i class="pi pi-print text-sm"></i> Imprimer
        </button>
      </ng-template>
    </p-dialog>

    <!-- Reverse dialog -->
    <p-dialog [(visible)]="reverseVisible" header="Annuler le paiement" [modal]="true" [style]="{ width: '400px' }">
      <div class="space-y-3 p-2">
        <p class="text-sm text-gray-600">Motif d'annulation <span class="text-red-500">*</span></p>
        <input [(ngModel)]="reverseReason" placeholder="Indiquez le motif…"
          class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-400" />
      </div>
      <ng-template pTemplate="footer">
        <div class="flex gap-2">
          <button (click)="reverseVisible = false" class="px-4 py-2 border border-gray-300 rounded-xl text-sm">{{ 'common.cancel' | translate }}</button>
          <button (click)="submitReverse()" class="px-4 py-2 bg-red-600 text-white rounded-xl text-sm hover:bg-red-700">{{ 'common.confirm' | translate }}</button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class PaymentListPageComponent implements OnInit {
  readonly store = inject(PaymentsStore);
  private readonly api = inject(PaymentsApiService);
  private readonly translate = inject(TranslateService);
  private readonly authStore = inject(AuthStore);

  readonly isStaff = computed(() => this.authStore.hasPermission(PERMISSIONS.PAYMENTS_READ));
  readonly isBoardMember = computed(() => this.authStore.user()?.entity_type === 'board_member');

  formDrawer = viewChild.required<PaymentFormDrawerComponent>('formDrawer');

  selectedPayment = signal<Payment | null>(null);
  selectedStatus = '';
  receiptVisible = false;
  reverseVisible = false;
  reverseReason = '';

  get statusOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      { label: this.translate.instant('payments.status_pending'), value: 'pending' },
      { label: this.translate.instant('payments.status_confirmed'), value: 'confirmed' },
      { label: this.translate.instant('payments.status_reversed'), value: 'reversed' },
    ];
  }

  ngOnInit() {
    this.store.loadPayments({});
    this.store.loadStats();
  }

  methodLabel(method: string) {
    return PAYMENT_METHODS.find(m => m.value === method)?.label ?? method;
  }

  statusLabel(status: string) {
    const map: Record<string, string> = {
      pending: this.translate.instant('payments.status_pending'),
      confirmed: this.translate.instant('payments.status_confirmed'),
      reversed: this.translate.instant('payments.status_reversed'),
    };
    return map[status] ?? status;
  }

  statusClass(status: string) {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const map: Record<string, string> = {
      pending: 'bg-yellow-100 text-yellow-800',
      confirmed: 'bg-green-100 text-green-800',
      reversed: 'bg-red-100 text-red-700',
    };
    return `${base} ${map[status] ?? 'bg-gray-100 text-gray-600'}`;
  }

  onStatusChange() {
    this.store.setStatusFilter(this.selectedStatus);
    this.store.loadPayments({ status: this.selectedStatus, page: 1 });
  }

  changePage(page: number) {
    this.store.loadPayments({ page });
  }

  openForm() {
    this.formDrawer().open();
  }

  onSaved() {
    this.store.loadPayments({});
    this.store.loadStats();
  }

  viewReceipt(p: Payment) {
    this.selectedPayment.set(p);
    this.receiptVisible = true;
  }

  printReceipt() {
    window.print();
  }

  confirm(p: Payment) {
    this.api.confirm(p.id).subscribe({
      next: () => { this.store.loadPayments({}); this.store.loadStats(); },
    });
  }

  openReverse(p: Payment) {
    this.selectedPayment.set(p);
    this.reverseReason = '';
    this.reverseVisible = true;
  }

  submitReverse() {
    const p = this.selectedPayment();
    if (!p || !this.reverseReason.trim()) return;
    this.api.reverse(p.id, { reason: this.reverseReason }).subscribe({
      next: () => {
        this.reverseVisible = false;
        this.store.loadPayments({});
        this.store.loadStats();
      },
    });
  }

  onReceiptFileSelected(event: Event, p: Payment) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.api.uploadReceipt(p.id, file).subscribe({
      next: () => this.store.loadPayments({}),
    });
  }
}
