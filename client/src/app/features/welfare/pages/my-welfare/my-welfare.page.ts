import { ChangeDetectionStrategy, Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthStore } from '@core/auth/auth.store';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { SelectModule } from 'primeng/select';
import { WelfareApiService } from '../../services/welfare-api.service';
import { WelfareStatusBadgeComponent } from '../../components/welfare-status-badge/welfare-status-badge.component';
import { WelfareRequestDrawerComponent } from '../../components/welfare-request-drawer/welfare-request-drawer.component';
import { WelfareRequest, WELFARE_TYPES } from '@models/welfare.model';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-my-welfare-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, SelectModule,
    AppCurrencyPipe,
    WelfareStatusBadgeComponent,
    WelfareRequestDrawerComponent,
    TranslatePipe,
  ],
  template: `
    <div class="min-h-full bg-gray-50 p-4 md:p-8 space-y-6">

      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ 'welfare.my_requests_title' | translate }}</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ 'welfare.my_requests_subtitle' | translate }}</p>
        </div>
        <button (click)="openDrawer()"
          class="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl text-sm font-medium shadow-sm transition-colors">
          <i class="pi pi-plus text-sm"></i>
          {{ 'welfare.new_request' | translate }}
        </button>
      </div>

      <!-- Filters -->
      <div class="flex gap-3">
        <p-select [options]="statusOptions" [(ngModel)]="selectedStatus" (onChange)="load(1)"
          optionLabel="label" optionValue="value" styleClass="w-full sm:w-48" />
      </div>

      <!-- List -->
      <div class="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">

        @if (loading()) {
          <div class="flex justify-center py-20">
            <i class="pi pi-spin pi-spinner text-3xl text-gray-300"></i>
          </div>
        } @else if (!requests().length) {
          <div class="text-center py-20 text-gray-400">
            <i class="pi pi-heart text-5xl mb-4 block"></i>
            <p class="text-sm">{{ 'welfare.no_requests' | translate }}</p>
          </div>
        } @else {

          <!-- Desktop -->
          <div class="hidden md:block overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                <tr>
                  <th class="text-left px-4 py-3">{{ 'common.type' | translate }}</th>
                  <th class="text-left px-4 py-3">{{ 'common.description' | translate }}</th>
                  <th class="text-right px-4 py-3">{{ 'welfare.amount_requested' | translate }}</th>
                  <th class="text-right px-4 py-3">{{ 'welfare.amount_approved' | translate }}</th>
                  <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
                  <th class="text-left px-4 py-3">{{ 'common.date' | translate }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                @for (r of requests(); track r.id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3 text-sm font-medium text-gray-900">{{ typeLabel(r.type) }}</td>
                    <td class="px-4 py-3 text-sm text-gray-500 max-w-xs truncate">{{ r.description }}</td>
                    <td class="px-4 py-3 text-right text-sm text-gray-900">{{ r.amount_requested | appCurrency }}</td>
                    <td class="px-4 py-3 text-right text-sm font-medium text-blue-700">
                      {{ r.amount_approved != null ? (r.amount_approved | appCurrency) : '—' }}
                    </td>
                    <td class="px-4 py-3"><app-welfare-status-badge [status]="r.status" /></td>
                    <td class="px-4 py-3 text-xs text-gray-400">{{ formatDate(r.created_at) }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Mobile cards -->
          <div class="md:hidden divide-y divide-gray-100">
            @for (r of requests(); track r.id) {
              <div class="p-4 space-y-2">
                <div class="flex items-start justify-between gap-2">
                  <div>
                    <p class="text-sm font-medium text-gray-900">{{ typeLabel(r.type) }}</p>
                    <p class="text-xs text-gray-400 mt-0.5">{{ formatDate(r.created_at) }}</p>
                  </div>
                  <app-welfare-status-badge [status]="r.status" />
                </div>
                <p class="text-sm text-gray-500 line-clamp-2">{{ r.description }}</p>
                <div class="flex items-center gap-4 text-sm">
                  <div>
                    <span class="text-gray-400 text-xs">{{ 'welfare.amount_requested' | translate }} </span>
                    <span class="font-semibold">{{ r.amount_requested | appCurrency }}</span>
                  </div>
                  @if (r.amount_approved != null) {
                    <div>
                      <span class="text-gray-400 text-xs">{{ 'welfare.amount_approved' | translate }} </span>
                      <span class="font-semibold text-blue-700">{{ r.amount_approved | appCurrency }}</span>
                    </div>
                  }
                </div>
                @if (r.rejection_reason) {
                  <p class="text-xs text-red-500 bg-red-50 rounded-lg px-3 py-2">{{ r.rejection_reason }}</p>
                }
              </div>
            }
          </div>

          <!-- Pagination -->
          @if (total() > limit) {
            <div class="p-4 border-t border-gray-100 flex items-center justify-between text-sm text-gray-500">
              <span>{{ total() }} {{ 'common.results' | translate }}</span>
              <div class="flex gap-2">
                <button [disabled]="page() <= 1" (click)="load(page() - 1)"
                  class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
                  {{ 'common.previous' | translate }}
                </button>
                <span class="px-3 py-1.5">{{ page() }} / {{ totalPages() }}</span>
                <button [disabled]="page() >= totalPages()" (click)="load(page() + 1)"
                  class="px-3 py-1.5 rounded-lg border border-gray-200 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed">
                  {{ 'common.next' | translate }}
                </button>
              </div>
            </div>
          }
        }
      </div>
    </div>

    <app-welfare-request-drawer #requestDrawer (saved)="onSaved()" />
  `,
})
export class MyWelfarePage implements OnInit {
  private readonly api    = inject(WelfareApiService);
  private readonly auth   = inject(AuthStore);
  private readonly translate = inject(TranslateService);

  requestDrawer = viewChild.required<WelfareRequestDrawerComponent>('requestDrawer');

  readonly requests = signal<WelfareRequest[]>([]);
  readonly total    = signal(0);
  readonly loading  = signal(true);
  readonly page     = signal(1);
  readonly limit    = 20;

  selectedStatus = '';

  get statusOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      { label: this.translate.instant('welfare.status_pending'), value: 'pending' },
      { label: this.translate.instant('welfare.status_approved'), value: 'approved' },
      { label: this.translate.instant('welfare.status_rejected'), value: 'rejected' },
      { label: this.translate.instant('welfare.status_paid'), value: 'paid' },
    ];
  }

  totalPages() {
    return Math.ceil(this.total() / this.limit);
  }

  ngOnInit(): void {
    this.load(1);
  }

  load(p: number): void {
    this.page.set(p);
    this.loading.set(true);
    this.api.getMyRequests(p, this.limit, this.selectedStatus || undefined).subscribe({
      next: res => {
        this.requests.set(res.data);
        this.total.set(res.pagination.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openDrawer(): void {
    const entityId = this.auth.currentUser()?.entity_id;
    this.requestDrawer().openForMember(entityId ?? '');
  }

  onSaved(): void {
    this.load(1);
  }

  typeLabel(type: string): string {
    return WELFARE_TYPES.find(t => t.value === type)?.label ?? type;
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
