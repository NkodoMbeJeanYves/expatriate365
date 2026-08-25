import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { AuthStore } from '@core/auth/auth.store';
import { TenantStore } from '@core/tenant/tenant.store';
import { PERMISSIONS } from '@core/auth/models/permission.model';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { PaymentsStore } from '../../store/payments.store';
import { PaymentsApiService } from '../../services/payments-api.service';
import { PaymentFormDrawerComponent } from '../../components/payment-form-drawer/payment-form-drawer.component';
import { PaymentReceiptComponent } from '../../components/payment-receipt/payment-receipt.component';
import { Payment, PaymentStats, PAYMENT_METHODS } from '@models/payment.model';
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
        <div class="flex gap-2">
          @if (isBoardMember()) {
            <button (click)="printReport()" [disabled]="printing()"
              class="inline-flex items-center gap-2 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50 bg-white shadow-sm disabled:opacity-50">
              <i [class]="printing() ? 'pi pi-spin pi-spinner text-sm' : 'pi pi-print text-sm'"></i>
              <span class="hidden sm:inline">{{ 'payments.print_report' | translate }}</span>
            </button>
          }
          <button (click)="openForm()"
            class="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-sm font-medium shadow-sm transition-colors">
            <i class="pi pi-plus text-sm"></i>
            {{ 'payments.new' | translate }}
          </button>
        </div>
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
        <div class="p-4 border-b border-gray-100 flex flex-col sm:flex-row gap-3 flex-wrap">
          <p-select [options]="statusOptions" [(ngModel)]="selectedStatus" (onChange)="onFilterChange()"
            optionLabel="label" optionValue="value" placeholder="Tous les statuts" styleClass="w-full sm:w-48" />
          <input type="date" [(ngModel)]="filterFrom" (change)="onFilterChange()"
            class="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-400 w-full sm:w-40" />
          <input type="date" [(ngModel)]="filterTo" (change)="onFilterChange()"
            class="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-700 focus:outline-none focus:ring-2 focus:ring-emerald-400 w-full sm:w-40" />
          @if (filterFrom || filterTo) {
            <button (click)="clearDates()" class="text-xs text-gray-400 hover:text-gray-600 underline self-center">Réinitialiser</button>
          }
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
  private readonly tenant = inject(TenantStore);

  readonly isStaff = computed(() => this.authStore.hasPermission(PERMISSIONS.PAYMENTS_READ));
  readonly isBoardMember = computed(() => this.authStore.user()?.entity_type === 'board_member');
  readonly printing = signal(false);

  formDrawer = viewChild.required<PaymentFormDrawerComponent>('formDrawer');

  selectedPayment = signal<Payment | null>(null);
  selectedStatus = '';
  filterFrom = '';
  filterTo = '';
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

  private currentFilters() {
    return {
      status: this.selectedStatus || undefined,
      from: this.filterFrom || undefined,
      to: this.filterTo || undefined,
    };
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

  onFilterChange() {
    this.store.setStatusFilter(this.selectedStatus);
    this.store.loadPayments({ page: 1, ...this.currentFilters() });
  }

  clearDates() {
    this.filterFrom = '';
    this.filterTo = '';
    this.onFilterChange();
  }

  changePage(page: number) {
    this.store.loadPayments({ page, ...this.currentFilters() });
  }

  printReport(): void {
    this.printing.set(true);
    const f = this.currentFilters();
    const memberId = this.store['_ownMemberId']?.();
    this.api.getPayments(1, 10000, memberId, f.status, f.from, f.to).subscribe({
      next: (res) => { this.printing.set(false); this.openPrintWindow(res.data, this.store.stats()); },
      error: () => this.printing.set(false),
    });
  }

  private openPrintWindow(payments: Payment[], stats: PaymentStats | null): void {
    const assoc = this.tenant.name() || 'Expatriate365';
    const logoUrl = this.tenant.logoUrl();
    const symbol = this.tenant.symbol() || 'FCFA';
    const now = new Date().toLocaleDateString('fr-FR', { year: 'numeric', month: 'long', day: 'numeric' });
    const fmt = (n: number) => n.toLocaleString('fr-FR') + ' ' + symbol;

    const filterParts: string[] = [];
    if (this.selectedStatus) filterParts.push(`Statut : <strong>${this.statusLabel(this.selectedStatus)}</strong>`);
    if (this.filterFrom) filterParts.push(`Du : <strong>${this.filterFrom}</strong>`);
    if (this.filterTo) filterParts.push(`Au : <strong>${this.filterTo}</strong>`);

    const statusBadge = (s: string) => {
      const map: Record<string, [string, string]> = {
        confirmed: ['#d1fae5', '#065f46'],
        pending:   ['#fef3c7', '#92400e'],
        reversed:  ['#fee2e2', '#991b1b'],
      };
      const [bg, color] = map[s] ?? ['#f3f4f6', '#374151'];
      const labels: Record<string, string> = {
        confirmed: this.translate.instant('payments.status_confirmed'),
        pending:   this.translate.instant('payments.status_pending'),
        reversed:  this.translate.instant('payments.status_reversed'),
      };
      return `<span style="background:${bg};color:${color};padding:2px 8px;border-radius:9999px;font-size:11px;font-weight:600">${labels[s] ?? s}</span>`;
    };

    const rows = payments.map(p => `
      <tr>
        <td><strong>${p.receipt_number}</strong><br><small style="color:#6b7280">${p.payment_date}</small></td>
        <td>${p.member_name}<br><small style="color:#6b7280">${p.membership_number}</small></td>
        <td>${p.contribution_type_name}</td>
        <td>${this.methodLabel(p.payment_method)}</td>
        <td class="num">${fmt(p.amount)}</td>
        <td style="text-align:center">${statusBadge(p.status)}</td>
      </tr>`).join('');

    const html = `<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<title>Rapport de paiements — ${assoc}</title>
<style>
  * { box-sizing: border-box; margin: 0; padding: 0; }
  body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #111827; padding: 24px; }
  .header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #059669; padding-bottom: 12px; margin-bottom: 20px; }
  .header h1 { font-size: 20px; font-weight: 700; color: #059669; }
  .header .meta { text-align: right; font-size: 11px; color: #6b7280; }
  .filters { font-size: 11px; color: #6b7280; margin-bottom: 16px; }
  .stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; margin-bottom: 20px; }
  .stat { border: 1px solid #e5e7eb; border-radius: 8px; padding: 10px 14px; }
  .stat .label { font-size: 10px; text-transform: uppercase; letter-spacing: .05em; color: #6b7280; }
  .stat .value { font-size: 16px; font-weight: 700; margin-top: 2px; }
  table { width: 100%; border-collapse: collapse; }
  thead th { background: #f9fafb; padding: 8px 10px; text-align: left; font-size: 10px; text-transform: uppercase; letter-spacing: .05em; color: #6b7280; border-bottom: 1px solid #e5e7eb; }
  tbody tr { border-bottom: 1px solid #f3f4f6; }
  tbody tr:nth-child(even) { background: #fafafa; }
  tbody td { padding: 8px 10px; font-size: 11px; vertical-align: middle; }
  .num { text-align: right; font-variant-numeric: tabular-nums; }
  .footer { margin-top: 24px; font-size: 10px; color: #9ca3af; text-align: center; border-top: 1px solid #e5e7eb; padding-top: 10px; }
  @media print { body { padding: 0; } @page { margin: 1.5cm; } }
</style>
</head>
<body>
<div class="header">
  <div style="display:flex;align-items:center;gap:12px">
    ${logoUrl ? `<img src="${logoUrl}" alt="logo" style="height:48px;width:auto;object-fit:contain;border-radius:6px" />` : ''}
    <div>
      <h1>${assoc}</h1>
      <div style="font-size:14px;font-weight:600;margin-top:4px">Rapport de paiements</div>
    </div>
  </div>
  <div class="meta">
    <div>Généré le ${now}</div>
    <div style="margin-top:4px">${payments.length} enregistrement(s)</div>
  </div>
</div>

${filterParts.length ? `<div class="filters">${filterParts.join(' &nbsp;·&nbsp; ')}</div>` : ''}

${stats ? `
<div class="stats">
  <div class="stat"><div class="label">Confirmés</div><div class="value" style="color:#059669">${fmt(stats.total_confirmed)}</div></div>
  <div class="stat"><div class="label">En attente</div><div class="value" style="color:#d97706">${fmt(stats.total_pending)}</div></div>
  <div class="stat"><div class="label">Nb transactions</div><div class="value" style="color:#111827">${stats.total_count}</div></div>
  <div class="stat"><div class="label">Annulés</div><div class="value" style="color:#dc2626">${stats.reversed_count}</div></div>
</div>` : ''}

<table>
  <thead>
    <tr>
      <th>Reçu / Date</th>
      <th>Membre</th>
      <th>Plan de cotisation</th>
      <th>Mode</th>
      <th class="num">Montant</th>
      <th style="text-align:center">Statut</th>
    </tr>
  </thead>
  <tbody>${rows || '<tr><td colspan="6" style="text-align:center;padding:20px;color:#9ca3af">Aucun paiement</td></tr>'}</tbody>
</table>

<div class="footer">Rapport généré par Expatriate365 — ${now}</div>
</body>
</html>`;

    const w = window.open('', '_blank', 'width=900,height=700');
    if (!w) return;
    w.document.write(html);
    w.document.close();
    w.focus();
    setTimeout(() => { w.print(); }, 400);
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
