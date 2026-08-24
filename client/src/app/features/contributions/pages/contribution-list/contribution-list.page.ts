import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { TenantStore } from '@core/tenant/tenant.store';
import { AuthStore } from '@core/auth/auth.store';
import { STAFF_ROLES } from '@core/auth/models/role.model';
import { ButtonModule } from 'primeng/button';
import { TabsModule } from 'primeng/tabs';
import { SelectModule } from 'primeng/select';
import { ContributionsStore } from '../../store/contributions.store';
import { ContributionsApiService } from '../../services/contributions-api.service';
import { ContributionStatusBadgeComponent } from '../../components/contribution-status-badge/contribution-status-badge.component';
import { ChargeActionDrawerComponent } from '../../components/charge-action-drawer/charge-action-drawer.component';
import { ContributionPlanDrawerComponent } from '../../components/contribution-plan-drawer/contribution-plan-drawer.component';
import { ContributionCharge, ContributionStats, ContributionType } from '@models/contribution.model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { PaymentFormDrawerComponent } from '@payments/components/payment-form-drawer/payment-form-drawer.component';
import { AppPaginatorComponent } from '@shared/components/paginator/app-paginator.component';
import { PageChangeEvent } from '@shared/components/paginator/app-paginator.component';

@Component({
  selector: 'app-contribution-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppCurrencyPipe,
    FormsModule,
    ButtonModule,

    TabsModule,
    SelectModule,
    ContributionStatusBadgeComponent,
    ChargeActionDrawerComponent,
    ContributionPlanDrawerComponent,
    PaymentFormDrawerComponent,
    AppPaginatorComponent,
    TranslatePipe,
  ],
  template: `
    <div class="min-h-full bg-gray-50 p-4 md:p-8 space-y-6">

      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ 'contributions.title' | translate }}</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ 'contributions.subtitle' | translate }}</p>
        </div>
        @if (isStaff()) {
          <div class="flex gap-2">
            <button (click)="printReport()" [disabled]="printing()"
              class="inline-flex items-center gap-2 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50 bg-white shadow-sm disabled:opacity-50">
              <i [class]="printing() ? 'pi pi-spin pi-spinner text-sm' : 'pi pi-print text-sm'"></i>
              <span class="hidden sm:inline">{{ 'contributions.print_report' | translate }}</span>
            </button>
            <button (click)="exportCsv()" [disabled]="exporting()"
              class="inline-flex items-center gap-2 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50 bg-white shadow-sm disabled:opacity-50">
              <i [class]="exporting() ? 'pi pi-spin pi-spinner text-sm' : 'pi pi-download text-sm'"></i>
              <span class="hidden sm:inline">Export CSV</span>
            </button>
            <button (click)="openPlan()" class="inline-flex items-center gap-2 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50 bg-white shadow-sm">
              <i class="pi pi-cog text-sm"></i>
              <span class="hidden sm:inline">Plans</span>
            </button>
          </div>
        }
      </div>

      <!-- Stats -->
      @if (store.stats()) {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'contributions.total_due' | translate }}</p>
            <p class="text-xl font-bold text-gray-900 mt-1">{{ store.stats()!.total_expected | appCurrency }}</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'finances.collected' | translate }}</p>
            <p class="text-xl font-bold text-green-600 mt-1">{{ store.stats()!.total_collected | appCurrency }}</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'contributions.status_pending' | translate }}</p>
            <p class="text-xl font-bold text-yellow-600 mt-1">{{ store.stats()!.pending_count }}</p>
            <p class="text-xs text-gray-400 mt-1">cotisations</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'contributions.status_overdue' | translate }}</p>
            <p class="text-xl font-bold text-red-600 mt-1">{{ store.stats()!.overdue_count }}</p>
            <p class="text-xs text-gray-400 mt-1">cotisations</p>
          </div>
        </div>
      }

      <!-- Tabs -->
      <p-tabs value="charges" (activeValueChange)="onTabChange($event)">
        <p-tablist>
          <p-tab value="charges">{{ 'contributions.title' | translate }}</p-tab>
          @if (isStaff()) { <p-tab value="plans">Plans</p-tab> }
          @if (isStaff()) { <p-tab value="rapport">Rapport</p-tab> }
        </p-tablist>

        <p-tabpanels>

          <!-- Charges tab -->
          <p-tabpanel value="charges">
            <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden mt-4">
              <!-- Filters -->
              <div class="p-4 border-b border-gray-100 flex flex-col sm:flex-row gap-3">
                <p-select [options]="statusOptions" [(ngModel)]="selectedStatus" (onChange)="onStatusChange()" optionLabel="label" optionValue="value" placeholder="Tous les statuts" styleClass="w-full sm:w-52" />
                <p-select [options]="typeOptions()" [(ngModel)]="selectedType" (onChange)="onTypeChange()" optionLabel="label" optionValue="value" placeholder="Tous les plans" styleClass="w-full sm:w-52" />
              </div>

              <!-- Table (desktop) -->
              <div class="hidden md:block overflow-x-auto">
                <table class="w-full text-sm">
                  <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                    <tr>
                      @if (isStaff()) { <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th> }
                      <th class="text-left px-4 py-3">Plan</th>
                      <th class="text-left px-4 py-3">{{ 'contributions.due_date' | translate }}</th>
                      <th class="text-right px-4 py-3">{{ 'common.amount' | translate }}</th>
                      <th class="text-right px-4 py-3">{{ 'contributions.amount_paid' | translate }}</th>
                      <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
                      @if (isStaff()) { <th class="px-4 py-3"></th> }
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100 bg-white">
                    @if (store.loading()) {
                      @for (i of [1,2,3,4,5]; track i) {
                        <tr><td colspan="7" class="px-4 py-3"><div class="h-8 bg-gray-100 rounded animate-pulse"></div></td></tr>
                      }
                    } @else if (!store.charges().length) {
                      <tr><td colspan="7" class="text-center px-4 py-12 text-gray-400 text-sm">{{ 'contributions.no_charges' | translate }}</td></tr>
                    } @else {
                      @for (charge of store.charges(); track charge.id) {
                        <tr class="hover:bg-gray-50">
                          @if (isStaff()) {
                            <td class="px-4 py-3">
                              <p class="text-sm font-medium text-gray-900">{{ charge.member_name }}</p>
                              <p class="text-xs text-gray-400">{{ charge.membership_number }}</p>
                            </td>
                          }
                          <td class="px-4 py-3 text-sm text-gray-600">{{ charge.contribution_type_name }}</td>
                          <td class="px-4 py-3 text-sm text-gray-600">{{ charge.due_date }}</td>
                          <td class="px-4 py-3 text-right text-sm font-medium text-gray-900">{{ charge.total_due | appCurrency }}</td>
                          <td class="px-4 py-3 text-right text-sm text-gray-600">{{ charge.amount_paid | appCurrency }}</td>
                          <td class="px-4 py-3"><app-contribution-status-badge [status]="charge.status" /></td>
                          <td class="px-4 py-3 text-right">
                            @if (charge.status !== 'paid' && charge.status !== 'waived') {
                              <div class="flex gap-2 justify-end">
                                <button (click)="openPayment(charge)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 font-medium">Payer</button>
                                @if (isStaff()) {
                                  <button (click)="openWaive(charge)" class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 hover:bg-gray-200 font-medium">Exonérer</button>
                                }
                              </div>
                            }
                          </td>
                        </tr>
                      }
                    }
                  </tbody>
                </table>
              </div>

              <!-- Mobile list -->
              <div class="md:hidden divide-y divide-gray-100">
                @for (charge of store.charges(); track charge.id) {
                  <div class="p-4 space-y-3">
                    <div class="flex items-start justify-between gap-2">
                      <div>
                        @if (isStaff()) { <p class="text-sm font-medium text-gray-900">{{ charge.member_name }}</p> }
                        <p class="text-xs text-gray-400">{{ charge.contribution_type_name }} · {{ charge.due_date }}</p>
                      </div>
                      <app-contribution-status-badge [status]="charge.status" />
                    </div>
                    <div class="flex items-center justify-between">
                      <div class="text-sm">
                        <span class="text-gray-500">{{ 'contributions.balance' | translate }}: </span>
                        <span class="font-semibold text-gray-900">{{ charge.balance | appCurrency }}</span>
                      </div>
                      @if (charge.status !== 'paid' && charge.status !== 'waived') {
                        <div class="flex gap-2">
                          <button (click)="openPayment(charge)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 font-medium">Payer</button>
                          @if (isStaff()) {
                            <button (click)="openWaive(charge)" class="text-xs px-2.5 py-1.5 rounded-lg bg-gray-100 text-gray-600 font-medium">Exonérer</button>
                          }
                        </div>
                      }
                    </div>
                  </div>
                } @empty {
                  <p class="text-center py-12 text-gray-400 text-sm">{{ 'contributions.no_charges' | translate }}</p>
                }
              </div>

              <!-- Pagination -->
              @if (store.total() > store.limit()) {
                <app-paginator
                  [page]="store.page()"
                  [limit]="store.limit()"
                  [total]="store.total()"
                  (pageChange)="changePage($event)" />
              }
            </div>
          </p-tabpanel>

          <!-- Plans tab (staff only) -->
          @if (isStaff()) {
          <p-tabpanel value="plans">
            <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              @for (plan of store.types(); track plan.id) {
                <div class="bg-white rounded-2xl p-5 shadow-sm border border-gray-100 space-y-3">
                  <div class="flex items-start justify-between gap-2">
                    <div>
                      <p class="font-semibold text-gray-900">{{ plan.name }}</p>
                      @if (plan.description) {
                        <p class="text-xs text-gray-400 mt-0.5">{{ plan.description }}</p>
                      }
                    </div>
                    <span [class]="plan.is_active ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'"
                      class="text-xs font-medium px-2 py-0.5 rounded-full flex-shrink-0">
                      {{ plan.is_active ? ('common.active_label' | translate) : ('common.inactive_label' | translate) }}
                    </span>
                  </div>
                  <div class="flex items-baseline gap-1">
                    <span class="text-2xl font-bold text-gray-900">{{ plan.base_amount | appCurrency }}</span>
                    <span class="text-sm text-gray-400">/ {{ frequencyLabel(plan.frequency) }}</span>
                  </div>
                  <div class="flex items-center justify-between text-xs text-gray-400">
                    <span>Depuis le {{ plan.effective_from }}</span>
                    <button (click)="openPlan(plan)" class="text-emerald-600 hover:text-emerald-700 font-medium">{{ 'common.edit' | translate }}</button>
                  </div>
                </div>
              } @empty {
                <div class="col-span-full text-center py-12 text-gray-400">{{ 'contributions.no_types' | translate }}</div>
              }
            </div>
          </p-tabpanel>
          } <!-- end @if isStaff plans -->

          <!-- Rapport tab -->
          @if (isStaff()) {
          <p-tabpanel value="rapport">
            <div class="mt-4 space-y-6">
              @if (reportLoading()) {
                <div class="flex justify-center py-12"><i class="pi pi-spin pi-spinner text-2xl text-gray-400"></i></div>
              } @else {
                <!-- KPIs globaux -->
                @if (reportTotal()) {
                  <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
                    <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
                      <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Types de plan</p>
                      <p class="text-2xl font-bold text-gray-900 mt-1">{{ reportByType().length }}</p>
                    </div>
                    <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
                      <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Total attendu</p>
                      <p class="text-2xl font-bold text-gray-900 mt-1">{{ reportTotal()!.expected | appCurrency }}</p>
                    </div>
                    <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
                      <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Collecté</p>
                      <p class="text-2xl font-bold text-green-600 mt-1">{{ reportTotal()!.paid | appCurrency }}</p>
                    </div>
                    <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
                      <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">Taux global</p>
                      <p class="text-2xl font-bold text-emerald-600 mt-1">{{ reportTotal()!.rate }}%</p>
                    </div>
                  </div>
                }
                <!-- Tableau par type -->
                <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
                  <div class="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
                    <h3 class="text-sm font-semibold text-gray-700">Détail par plan</h3>
                  </div>
                  <div class="overflow-x-auto">
                    <table class="w-full text-sm">
                      <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                        <tr>
                          <th class="text-left px-4 py-3">Plan</th>
                          <th class="text-right px-4 py-3">Cotisants</th>
                          <th class="text-right px-4 py-3">Attendu</th>
                          <th class="text-right px-4 py-3">Collecté</th>
                          <th class="text-right px-4 py-3">Solde</th>
                          <th class="text-right px-4 py-3">Taux</th>
                        </tr>
                      </thead>
                      <tbody class="divide-y divide-gray-100">
                        @for (row of reportByType(); track row.typeId) {
                          <tr class="hover:bg-gray-50">
                            <td class="px-4 py-3 font-medium text-gray-900">{{ row.typeName }}</td>
                            <td class="px-4 py-3 text-right text-gray-600">{{ row.count }}</td>
                            <td class="px-4 py-3 text-right text-gray-600">{{ row.expected | appCurrency }}</td>
                            <td class="px-4 py-3 text-right text-green-600 font-medium">{{ row.paid | appCurrency }}</td>
                            <td class="px-4 py-3 text-right text-red-500">{{ row.balance | appCurrency }}</td>
                            <td class="px-4 py-3 text-right">
                              <span [class]="row.rate >= 80 ? 'text-green-600 font-semibold' : row.rate >= 50 ? 'text-yellow-600' : 'text-red-500'">
                                {{ row.rate }}%
                              </span>
                            </td>
                          </tr>
                        } @empty {
                          <tr><td colspan="6" class="text-center py-12 text-gray-400">Aucune donnée disponible</td></tr>
                        }
                      </tbody>
                    </table>
                  </div>
                </div>
              }
            </div>
          </p-tabpanel>
          } <!-- end @if isStaff rapport -->

        </p-tabpanels>
      </p-tabs>
    </div>

    <!-- Drawers -->
    <app-charge-action-drawer
      #actionDrawer
      [charge]="selectedCharge()"
      [action]="drawerAction()"
      (saved)="onChargeSaved()" />

    <app-contribution-plan-drawer
      #planDrawer
      (saved)="onPlanSaved()" />

    <app-payment-form-drawer
      #paymentDrawer
      (saved)="onChargeSaved()" />
  `,
})
export class ContributionListPageComponent implements OnInit {
  readonly store = inject(ContributionsStore);
  private readonly translate = inject(TranslateService);
  private readonly api = inject(ContributionsApiService);
  private readonly tenant = inject(TenantStore);
  private readonly authStore = inject(AuthStore);

  readonly isStaff = computed(() => this.authStore.hasAnyRole(STAFF_ROLES));
  private get memberEntityId(): string | undefined {
    const u = this.authStore.currentUser();
    return u?.entity_type === 'member' ? u.entity_id : undefined;
  }

  printing = signal(false);
  exporting = signal(false);
  reportLoading = signal(false);
  reportCharges = signal<ContributionCharge[]>([]);

  readonly reportByType = computed(() => {
    const map = new Map<string, { typeId: string; typeName: string; count: number; expected: number; paid: number; balance: number }>();
    for (const c of this.reportCharges()) {
      const key = c.contribution_type_id;
      const existing = map.get(key) ?? { typeId: key, typeName: c.contribution_type_name, count: 0, expected: 0, paid: 0, balance: 0 };
      existing.count++;
      existing.expected += c.total_due;
      existing.paid += c.amount_paid;
      existing.balance += c.balance;
      map.set(key, existing);
    }
    return [...map.values()].map(r => ({ ...r, rate: r.expected > 0 ? Math.round((r.paid / r.expected) * 100) : 0 }));
  });

  readonly reportTotal = computed(() => {
    const rows = this.reportByType();
    if (!rows.length) return null;
    const expected = rows.reduce((s, r) => s + r.expected, 0);
    const paid = rows.reduce((s, r) => s + r.paid, 0);
    return { expected, paid, balance: expected - paid, rate: expected > 0 ? Math.round((paid / expected) * 100) : 0 };
  });

  actionDrawer = viewChild.required<ChargeActionDrawerComponent>('actionDrawer');
  planDrawer = viewChild.required<ContributionPlanDrawerComponent>('planDrawer');
  paymentDrawer = viewChild.required<PaymentFormDrawerComponent>('paymentDrawer');

  selectedCharge = signal<ContributionCharge | null>(null);
  drawerAction = signal<'pay' | 'waive'>('pay');

  selectedStatus = '';
  selectedType = '';

  get statusOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      { label: this.translate.instant('contributions.status_pending'), value: 'pending' },
      { label: this.translate.instant('contributions.status_paid'), value: 'paid' },
      { label: this.translate.instant('contributions.status_overdue'), value: 'overdue' },
      { label: this.translate.instant('contributions.status_waived'), value: 'waived' },
    ];
  }

  typeOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      ...this.store.types().map(t => ({ label: t.name, value: t.id })),
    ];
  }

  frequencyLabel(f: string) {
    const map: Record<string, string> = { monthly: 'mois', quarterly: 'trimestre', annual: 'an', one_time: 'unique' };
    return map[f] ?? f;
  }

  printReport(): void {
    this.printing.set(true);
    const status = this.selectedStatus || undefined;
    const typeId = this.selectedType || undefined;
    this.api.getCharges(1, 1000, undefined, typeId, status).subscribe({
      next: (res) => {
        this.printing.set(false);
        this.openPrintWindow(res.data, this.store.stats());
      },
      error: () => this.printing.set(false),
    });
  }

  private openPrintWindow(charges: ContributionCharge[], stats: ContributionStats | null): void {
    const assoc = this.tenant.name() || 'Expatriate365';
    const symbol = this.tenant.symbol() || 'FCFA';
    const now = new Date().toLocaleDateString('fr-FR', { year: 'numeric', month: 'long', day: 'numeric' });
    const filterLabel = this.selectedStatus
      ? this.translate.instant(`contributions.status_${this.selectedStatus}`)
      : this.translate.instant('common.all');
    const typeName = this.store.types().find(t => t.id === this.selectedType)?.name ?? this.translate.instant('common.all');

    const fmt = (n: number) => n.toLocaleString('fr-FR') + ' ' + symbol;

    const statusBadge = (s: string) => {
      const map: Record<string, [string, string]> = {
        paid: ['#d1fae5', '#065f46'],
        pending: ['#fef3c7', '#92400e'],
        overdue: ['#fee2e2', '#991b1b'],
        waived: ['#f3f4f6', '#6b7280'],
      };
      const [bg, color] = map[s] ?? ['#f3f4f6', '#374151'];
      const labels: Record<string, string> = { paid: 'Payé', pending: 'En attente', overdue: 'En retard', waived: 'Exonéré' };
      return `<span style="background:${bg};color:${color};padding:2px 8px;border-radius:9999px;font-size:11px;font-weight:600">${labels[s] ?? s}</span>`;
    };

    const rows = charges.map(c => `
      <tr>
        <td>${c.member_name}<br><small style="color:#6b7280">${c.membership_number}</small></td>
        <td>${c.contribution_type_name}</td>
        <td>${c.due_date}</td>
        <td class="num">${fmt(c.total_due)}</td>
        <td class="num">${fmt(c.amount_paid)}</td>
        <td class="num" style="color:${c.balance > 0 ? '#dc2626' : '#16a34a'}">${fmt(c.balance)}</td>
        <td style="text-align:center">${statusBadge(c.status)}</td>
      </tr>`).join('');

    const html = `<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<title>Rapport de cotisations — ${assoc}</title>
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
  <div>
    <h1>${assoc}</h1>
    <div style="font-size:14px;font-weight:600;margin-top:4px">Rapport de cotisations</div>
  </div>
  <div class="meta">
    <div>Généré le ${now}</div>
    <div style="margin-top:4px">${charges.length} enregistrement(s)</div>
  </div>
</div>

<div class="filters">
  Statut : <strong>${filterLabel}</strong> &nbsp;·&nbsp; Plan : <strong>${typeName}</strong>
</div>

${stats ? `
<div class="stats">
  <div class="stat"><div class="label">Total attendu</div><div class="value" style="color:#111827">${fmt(stats.total_expected)}</div></div>
  <div class="stat"><div class="label">Collecté</div><div class="value" style="color:#059669">${fmt(stats.total_collected)}</div></div>
  <div class="stat"><div class="label">En attente</div><div class="value" style="color:#d97706">${fmt(stats.total_pending)}</div></div>
  <div class="stat"><div class="label">En retard</div><div class="value" style="color:#dc2626">${stats.overdue_count} cotisation(s)</div></div>
</div>` : ''}

<table>
  <thead>
    <tr>
      <th>Membre</th>
      <th>Plan</th>
      <th>Échéance</th>
      <th class="num">Montant dû</th>
      <th class="num">Payé</th>
      <th class="num">Solde</th>
      <th style="text-align:center">Statut</th>
    </tr>
  </thead>
  <tbody>${rows || '<tr><td colspan="7" style="text-align:center;padding:20px;color:#9ca3af">Aucune cotisation</td></tr>'}</tbody>
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

  ngOnInit() {
    this.store.loadTypes();
    this.store.loadCharges({ memberId: this.memberEntityId });
    this.store.loadStats();
  }

  onStatusChange() {
    this.store.setStatusFilter(this.selectedStatus);
    this.store.loadCharges({ memberId: this.memberEntityId, status: this.selectedStatus, page: 1 });
  }

  onTypeChange() {
    this.store.setTypeFilter(this.selectedType);
    this.store.loadCharges({ memberId: this.memberEntityId, typeId: this.selectedType, page: 1 });
  }

  changePage(event: PageChangeEvent) {
    this.store.loadCharges({ memberId: this.memberEntityId, page: event.page });
  }

  openPay(charge: ContributionCharge) {
    this.selectedCharge.set(charge);
    this.drawerAction.set('pay');
    this.actionDrawer().open();
  }

  openPayment(charge: ContributionCharge) {
    this.paymentDrawer().open({
      id: charge.id,
      balance: charge.balance,
      label: `${charge.contribution_type_name} — solde: ${charge.balance.toLocaleString('fr-FR')}`,
    });
  }

  openWaive(charge: ContributionCharge) {
    this.selectedCharge.set(charge);
    this.drawerAction.set('waive');
    this.actionDrawer().open();
  }

  onChargeSaved() {
    this.store.loadCharges({});
    this.store.loadStats();
  }

  openPlan(plan?: ContributionType) {
    this.planDrawer().open(plan);
  }

  onPlanSaved() {
    this.store.loadTypes();
  }

  onTabChange(event: unknown) {
    if (String(event) === 'rapport' && !this.reportCharges().length) {
      this.loadReport();
    }
  }

  loadReport() {
    this.reportLoading.set(true);
    this.api.getCharges(1, 1000).subscribe({
      next: (res) => { this.reportCharges.set(res.data); this.reportLoading.set(false); },
      error: () => this.reportLoading.set(false),
    });
  }

  exportCsv() {
    this.exporting.set(true);
    this.api.getCharges(1, 10000).subscribe({
      next: (res) => { this.downloadCsv(res.data); this.exporting.set(false); },
      error: () => this.exporting.set(false),
    });
  }

  private downloadCsv(charges: ContributionCharge[]) {
    const headers = ['Membre', 'N° adhérent', 'Plan', 'Échéance', 'Montant dû', 'Payé', 'Solde', 'Statut'];
    const rows = charges.map(c => [
      c.member_name, c.membership_number, c.contribution_type_name, c.due_date,
      c.total_due, c.amount_paid, c.balance, c.status,
    ]);
    const csv = [headers, ...rows].map(r => r.map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(',')).join('\r\n');
    const bom = '﻿';
    const blob = new Blob([bom + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = `cotisations_${new Date().toISOString().slice(0, 10)}.csv`;
    a.click(); URL.revokeObjectURL(url);
  }
}
