import { ChangeDetectionStrategy, Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { WelfareStore } from '../../store/welfare.store';
import { WelfareApiService } from '../../services/welfare-api.service';
import { WelfareStatusBadgeComponent } from '../../components/welfare-status-badge/welfare-status-badge.component';
import { WelfareRequestDrawerComponent } from '../../components/welfare-request-drawer/welfare-request-drawer.component';
import { WelfareRequest, WELFARE_TYPES } from '@models/welfare.model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-welfare-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AppCurrencyPipe, FormsModule,
    DialogModule, SelectModule,
    WelfareStatusBadgeComponent, WelfareRequestDrawerComponent, TranslatePipe,
  ],
  template: `
    <div class="min-h-full bg-gray-50 p-4 md:p-8 space-y-6">

      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ 'welfare.title' | translate }}</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ 'welfare.subtitle' | translate }}</p>
        </div>
        <button (click)="openDrawer()"
          class="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-sm font-medium shadow-sm transition-colors">
          <i class="pi pi-plus text-sm"></i>
          {{ 'welfare.new_request' | translate }}
        </button>
      </div>

      <!-- Stats -->
      @if (store.stats()) {
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'welfare.status_pending' | translate }}</p>
            <p class="text-xl font-bold text-yellow-600 mt-1">{{ store.stats()!.pending_count }}</p>
            <p class="text-xs text-gray-400 mt-1">demandes</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'welfare.status_approved' | translate }}</p>
            <p class="text-xl font-bold text-blue-600 mt-1">{{ store.stats()!.approved_count }}</p>
            <p class="text-xs text-gray-400 mt-1">demandes</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'welfare.amount_paid' | translate }}</p>
            <p class="text-xl font-bold text-green-600 mt-1">{{ store.stats()!.total_paid | appCurrency }}</p>
          </div>
          <div class="bg-white rounded-2xl p-4 shadow-sm border border-gray-100">
            <p class="text-xs font-medium text-gray-500 uppercase tracking-wider">{{ 'welfare.amount_requested' | translate }}</p>
            <p class="text-xl font-bold text-gray-900 mt-1">{{ store.stats()!.total_requested | appCurrency }}</p>
          </div>
        </div>
      }

      <!-- Table -->
      <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div class="p-4 border-b border-gray-100 flex flex-col sm:flex-row gap-3">
          <p-select [options]="statusOptions" [(ngModel)]="selectedStatus" (onChange)="onStatusChange()"
            optionLabel="label" optionValue="value" styleClass="w-full sm:w-48" />
          <p-select [options]="typeOptions" [(ngModel)]="selectedType" (onChange)="onTypeChange()"
            optionLabel="label" optionValue="value" styleClass="w-full sm:w-48" />
        </div>

        <!-- Desktop table -->
        <div class="hidden md:block overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider">
              <tr>
                <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th>
                <th class="text-left px-4 py-3">{{ 'common.type' | translate }}</th>
                <th class="text-right px-4 py-3">{{ 'welfare.amount_requested' | translate }}</th>
                <th class="text-right px-4 py-3">{{ 'welfare.amount_approved' | translate }}</th>
                <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 bg-white">
              @if (store.loading()) {
                @for (i of [1,2,3,4,5]; track i) {
                  <tr><td colspan="6" class="px-4 py-3"><div class="h-8 bg-gray-100 rounded animate-pulse"></div></td></tr>
                }
              } @else if (!store.requests().length) {
                <tr><td colspan="6" class="text-center px-4 py-12 text-gray-400 text-sm">{{ 'welfare.no_requests' | translate }}</td></tr>
              } @else {
                @for (r of store.requests(); track r.id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3">
                      <p class="text-sm font-medium text-gray-900">{{ r.member_name }}</p>
                      <p class="text-xs text-gray-400">{{ r.membership_number }}</p>
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-600">{{ typeLabel(r.type) }}</td>
                    <td class="px-4 py-3 text-right text-sm text-gray-900">{{ r.amount_requested | appCurrency }}</td>
                    <td class="px-4 py-3 text-right text-sm font-medium text-blue-700">
                      {{ r.amount_approved != null ? (r.amount_approved | appCurrency) : '—' }}
                    </td>
                    <td class="px-4 py-3"><app-welfare-status-badge [status]="r.status" /></td>
                    <td class="px-4 py-3 text-right">
                      <div class="flex gap-2 justify-end">
                        @if (r.status === 'pending') {
                          <button (click)="openApprove(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 hover:bg-blue-100 font-medium">{{ 'welfare.approve_request' | translate }}</button>
                          <button (click)="openReject(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-red-50 text-red-600 hover:bg-red-100 font-medium">{{ 'welfare.reject_request' | translate }}</button>
                        }
                        @if (r.status === 'approved') {
                          <button (click)="markPaid(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 hover:bg-emerald-100 font-medium">{{ 'welfare.mark_paid' | translate }}</button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        <!-- Mobile cards -->
        <div class="md:hidden divide-y divide-gray-100">
          @for (r of store.requests(); track r.id) {
            <div class="p-4 space-y-3">
              <div class="flex items-start justify-between gap-2">
                <div>
                  <p class="text-sm font-medium text-gray-900">{{ r.member_name }}</p>
                  <p class="text-xs text-gray-400">{{ typeLabel(r.type) }}</p>
                </div>
                <app-welfare-status-badge [status]="r.status" />
              </div>
              <div class="flex items-center justify-between">
                <div class="text-sm">
                  <span class="text-gray-500">{{ 'welfare.amount_requested' | translate }}: </span>
                  <span class="font-semibold text-gray-900">{{ r.amount_requested | appCurrency }}</span>
                </div>
                <div class="flex gap-2">
                  @if (r.status === 'pending') {
                    <button (click)="openApprove(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-blue-50 text-blue-700 font-medium">{{ 'welfare.approve_request' | translate }}</button>
                    <button (click)="openReject(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-red-50 text-red-600 font-medium">{{ 'welfare.reject_request' | translate }}</button>
                  }
                  @if (r.status === 'approved') {
                    <button (click)="markPaid(r)" class="text-xs px-2.5 py-1.5 rounded-lg bg-emerald-50 text-emerald-700 font-medium">{{ 'welfare.mark_paid' | translate }}</button>
                  }
                </div>
              </div>
            </div>
          } @empty {
            <p class="text-center py-12 text-gray-400 text-sm">{{ 'welfare.no_requests' | translate }}</p>
          }
        </div>

        <!-- Pagination -->
        @if (store.total() > store.limit()) {
          <div class="p-4 border-t border-gray-100 flex items-center justify-between text-sm text-gray-500">
            <span>{{ store.total() }} demandes</span>
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

    <!-- New request drawer -->
    <app-welfare-request-drawer #requestDrawer (saved)="onSaved()" />

    <!-- Approve dialog -->
    <p-dialog [(visible)]="approveVisible" [header]="'welfare.approve_request' | translate" [modal]="true" [style]="{ width: '400px' }">
      @if (selected()) {
        <div class="space-y-4 p-2">
          <div class="bg-gray-50 rounded-xl p-3 text-sm">
            <p class="font-medium text-gray-900">{{ selected()!.member_name }}</p>
            <p class="text-gray-500">{{ 'welfare.amount_requested' | translate }} : {{ selected()!.amount_requested | appCurrency }}</p>
          </div>
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'welfare.amount_approved' | translate }} <span class="text-red-500">*</span></label>
            <input type="number" [(ngModel)]="approveAmount" [max]="selected()!.amount_requested"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          </div>
        </div>
      }
      <ng-template pTemplate="footer">
        <div class="flex gap-2">
          <button (click)="approveVisible = false" class="px-4 py-2 border border-gray-300 rounded-xl text-sm">{{ 'common.cancel' | translate }}</button>
          <button (click)="submitApprove()" class="px-4 py-2 bg-blue-600 text-white rounded-xl text-sm hover:bg-blue-700">{{ 'common.approve' | translate }}</button>
        </div>
      </ng-template>
    </p-dialog>

    <!-- Reject dialog -->
    <p-dialog [(visible)]="rejectVisible" [header]="'welfare.reject_request' | translate" [modal]="true" [style]="{ width: '400px' }">
      <div class="space-y-3 p-2">
        <p class="text-sm text-gray-600">Motif du rejet <span class="text-red-500">*</span></p>
        <textarea [(ngModel)]="rejectReason" rows="3" placeholder="Expliquez la raison du rejet…"
          class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-red-400 resize-none"></textarea>
      </div>
      <ng-template pTemplate="footer">
        <div class="flex gap-2">
          <button (click)="rejectVisible = false" class="px-4 py-2 border border-gray-300 rounded-xl text-sm">{{ 'common.cancel' | translate }}</button>
          <button (click)="submitReject()" class="px-4 py-2 bg-red-600 text-white rounded-xl text-sm hover:bg-red-700">{{ 'common.confirm' | translate }}</button>
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class WelfareListPageComponent implements OnInit {
  readonly store = inject(WelfareStore);
  private readonly api = inject(WelfareApiService);
  private readonly translate = inject(TranslateService);

  requestDrawer = viewChild.required<WelfareRequestDrawerComponent>('requestDrawer');

  selected = signal<WelfareRequest | null>(null);
  selectedStatus = '';
  selectedType = '';
  typeOptions = [{ label: 'Tous les types', value: '' }, ...WELFARE_TYPES];

  get statusOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      { label: this.translate.instant('welfare.status_pending'), value: 'pending' },
      { label: this.translate.instant('welfare.status_approved'), value: 'approved' },
      { label: this.translate.instant('welfare.status_rejected'), value: 'rejected' },
      { label: this.translate.instant('welfare.status_paid'), value: 'paid' },
    ];
  }

  approveVisible = false;
  approveAmount = 0;
  rejectVisible = false;
  rejectReason = '';

  ngOnInit() {
    this.store.loadRequests();
    this.store.loadStats();
  }

  typeLabel(type: string) {
    return WELFARE_TYPES.find(t => t.value === type)?.label ?? type;
  }

  onStatusChange() {
    this.store.setStatusFilter(this.selectedStatus);
    this.store.loadRequests({ status: this.selectedStatus, page: 1 });
  }

  onTypeChange() {
    this.store.setTypeFilter(this.selectedType);
    this.store.loadRequests({ type: this.selectedType, page: 1 });
  }

  changePage(page: number) { this.store.loadRequests({ page }); }

  openDrawer() { this.requestDrawer().open(); }

  onSaved() {
    this.store.loadRequests();
    this.store.loadStats();
  }

  openApprove(r: WelfareRequest) {
    this.selected.set(r);
    this.approveAmount = r.amount_requested;
    this.approveVisible = true;
  }

  submitApprove() {
    const r = this.selected();
    if (!r || !this.approveAmount) return;
    this.api.approve(r.id, { amount_approved: this.approveAmount }).subscribe({
      next: () => { this.approveVisible = false; this.store.loadRequests(); this.store.loadStats(); },
    });
  }

  openReject(r: WelfareRequest) {
    this.selected.set(r);
    this.rejectReason = '';
    this.rejectVisible = true;
  }

  submitReject() {
    const r = this.selected();
    if (!r || !this.rejectReason.trim()) return;
    this.api.reject(r.id, { reason: this.rejectReason }).subscribe({
      next: () => { this.rejectVisible = false; this.store.loadRequests(); this.store.loadStats(); },
    });
  }

  markPaid(r: WelfareRequest) {
    this.api.markPaid(r.id).subscribe({
      next: () => { this.store.loadRequests(); this.store.loadStats(); },
    });
  }
}
