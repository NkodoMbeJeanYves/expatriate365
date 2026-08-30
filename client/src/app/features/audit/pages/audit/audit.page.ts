import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { AuditApiService } from '../../services/audit-api.service';
import { AuditLogDto, TenantStatsDto, AnomalyDto } from '@models/audit.model';

@Component({
  selector: 'app-audit',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, ButtonModule, TagModule, ProgressSpinnerModule,
            InputTextModule, SelectModule, TranslatePipe],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-800">{{ 'audit.title' | translate }}</h1>
        <p class="text-gray-500 text-sm">{{ 'audit.subtitle' | translate }}</p>
      </div>

      <!-- Tabs -->
      <div class="flex gap-2 border-b border-gray-100 pb-0">
        @for (tab of tabs; track tab.value) {
          <button class="px-4 py-2 text-sm font-medium border-b-2 transition-colors -mb-px"
            [class]="activeTab() === tab.value
              ? 'border-emerald-600 text-emerald-700'
              : 'border-transparent text-gray-500 hover:text-gray-800'"
            (click)="setTab(tab.value)">
            {{ tab.labelKey | translate }}
          </button>
        }
      </div>

      <!-- ── Activity tab ── -->
      @if (activeTab() === 'activity') {
        <div class="flex flex-wrap gap-3">
          <input pInputText [(ngModel)]="filterAction" [placeholder]="'audit.filter_action' | translate"
            class="w-44" (change)="loadLogs()" />
          <p-button size="small" [label]="'common.refresh' | translate" icon="pi pi-refresh"
            severity="secondary" (onClick)="loadLogs()" />
        </div>

        @if (logsLoading()) {
          <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
        } @else {
          <div class="overflow-x-auto rounded-xl border border-gray-100">
            <table class="w-full text-sm">
              <thead class="bg-gray-50 text-gray-500 text-xs uppercase">
                <tr>
                  <th class="px-4 py-3 text-left">{{ 'audit.col_date' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'audit.col_user' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'audit.col_action' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'audit.col_entity' | translate }}</th>
                  <th class="px-4 py-3 text-left">{{ 'audit.col_tenant' | translate }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-50">
                @for (log of logs(); track log.id) {
                  <tr class="hover:bg-gray-50 transition-colors">
                    <td class="px-4 py-3 text-gray-400 whitespace-nowrap">
                      {{ log.created_at | date:'dd/MM/yy HH:mm' }}
                    </td>
                    <td class="px-4 py-3 font-medium text-gray-800">{{ log.user_name }}</td>
                    <td class="px-4 py-3">
                      <span [class]="actionClass(log.action)"
                        class="px-2 py-0.5 rounded-full text-xs font-medium">
                        {{ log.action }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-gray-500">
                      @if (log.entity_type) {
                        <span>{{ log.entity_type }}</span>
                      }
                    </td>
                    <td class="px-4 py-3 text-gray-500">{{ log.tenant_name ?? '—' }}</td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5" class="px-4 py-12 text-center text-gray-400">
                      {{ 'audit.no_logs' | translate }}
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          @if (logTotal() > logPage() * 20) {
            <div class="flex justify-center">
              <p-button [label]="'common.load_more' | translate" severity="secondary"
                (onClick)="loadMoreLogs()" />
            </div>
          }
        }
      }

      <!-- ── Tenants tab ── -->
      @if (activeTab() === 'tenants') {
        @if (tenantsLoading()) {
          <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
        } @else {
          <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            @for (t of tenants(); track t.id) {
              <div class="bg-white border border-gray-100 rounded-xl p-5 shadow-sm flex flex-col gap-3">
                <div class="flex items-center justify-between">
                  <h3 class="font-semibold text-gray-800">{{ t.name }}</h3>
                  <span class="text-xs text-gray-400">{{ t.slug }}</span>
                </div>
                <div class="grid grid-cols-2 gap-2 text-sm">
                  <div class="bg-gray-50 rounded-lg p-2 text-center">
                    <p class="text-lg font-bold text-gray-800">{{ t.member_count }}</p>
                    <p class="text-xs text-gray-400">{{ 'audit.stat_members' | translate }}</p>
                  </div>
                  <div class="bg-emerald-50 rounded-lg p-2 text-center">
                    <p class="text-lg font-bold text-emerald-700">{{ t.posts_published }}</p>
                    <p class="text-xs text-emerald-600">{{ 'audit.stat_published' | translate }}</p>
                  </div>
                  <div class="bg-yellow-50 rounded-lg p-2 text-center">
                    <p class="text-lg font-bold text-yellow-700">{{ t.posts_draft }}</p>
                    <p class="text-xs text-yellow-600">{{ 'audit.stat_draft' | translate }}</p>
                  </div>
                  <div class="bg-red-50 rounded-lg p-2 text-center">
                    <p class="text-lg font-bold text-red-600">{{ t.posts_rejected }}</p>
                    <p class="text-xs text-red-500">{{ 'audit.stat_rejected' | translate }}</p>
                  </div>
                </div>
                @if (t.last_activity) {
                  <p class="text-xs text-gray-400">
                    {{ 'audit.last_activity' | translate }}:
                    {{ t.last_activity | date:'dd/MM/yy HH:mm' }}
                  </p>
                } @else {
                  <p class="text-xs text-gray-400">{{ 'audit.no_activity' | translate }}</p>
                }
              </div>
            } @empty {
              <div class="col-span-3 text-center py-12 text-gray-400">
                {{ 'audit.no_tenants' | translate }}
              </div>
            }
          </div>
        }
      }

      <!-- ── Anomalies tab ── -->
      @if (activeTab() === 'anomalies') {
        @if (anomaliesLoading()) {
          <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
        } @else if (anomalies().length === 0) {
          <div class="text-center py-12 text-gray-400">
            <i class="pi pi-check-circle text-3xl mb-2 block text-emerald-500"></i>
            <p>{{ 'audit.no_anomalies' | translate }}</p>
          </div>
        } @else {
          <div class="flex flex-col gap-3">
            @for (a of anomalies(); track $index) {
              <div class="flex items-start gap-4 bg-white border rounded-xl p-4 shadow-sm"
                [class]="anomalyBorder(a.severity)">
                <div [class]="anomalyIcon(a.severity)"
                  class="w-8 h-8 rounded-full flex items-center justify-center shrink-0 text-sm">
                  <i [class]="anomalyIconName(a.severity)"></i>
                </div>
                <div class="flex-1">
                  <p class="text-sm font-medium text-gray-800">{{ a.description }}</p>
                  @if (a.tenant_name) {
                    <p class="text-xs text-gray-400 mt-0.5">{{ a.tenant_name }}</p>
                  }
                </div>
                <p-tag [value]="a.severity" [severity]="severityTag(a.severity)" />
              </div>
            }
          </div>
        }
      }
    </div>
  `,
})
export class AuditPage implements OnInit {
  private readonly api = inject(AuditApiService);

  readonly activeTab   = signal<string>('activity');
  readonly logsLoading = signal(true);
  readonly logs        = signal<AuditLogDto[]>([]);
  readonly logTotal    = signal(0);
  readonly logPage     = signal(1);

  readonly tenantsLoading   = signal(false);
  readonly tenants          = signal<TenantStatsDto[]>([]);
  readonly anomaliesLoading = signal(false);
  readonly anomalies        = signal<AnomalyDto[]>([]);

  filterAction = '';

  readonly tabs = [
    { labelKey: 'audit.tab_activity',  value: 'activity'  },
    { labelKey: 'audit.tab_tenants',   value: 'tenants'   },
    { labelKey: 'audit.tab_anomalies', value: 'anomalies' },
  ];

  ngOnInit(): void { this.loadLogs(); }

  setTab(tab: string): void {
    this.activeTab.set(tab);
    if (tab === 'tenants'   && this.tenants().length   === 0) this.loadTenants();
    if (tab === 'anomalies' && this.anomalies().length === 0) this.loadAnomalies();
  }

  loadLogs(page = 1): void {
    this.logsLoading.set(true);
    this.api.getLogs({ page, action: this.filterAction || undefined }).subscribe({
      next: r => {
        this.logs.set(page === 1 ? r.data : [...this.logs(), ...r.data]);
        this.logTotal.set(r.pagination.total);
        this.logPage.set(page);
        this.logsLoading.set(false);
      },
      error: () => this.logsLoading.set(false),
    });
  }

  loadMoreLogs(): void { this.loadLogs(this.logPage() + 1); }

  loadTenants(): void {
    this.tenantsLoading.set(true);
    this.api.getTenants().subscribe({
      next: r  => { this.tenants.set(r); this.tenantsLoading.set(false); },
      error: () => this.tenantsLoading.set(false),
    });
  }

  loadAnomalies(): void {
    this.anomaliesLoading.set(true);
    this.api.getAnomalies().subscribe({
      next: r  => { this.anomalies.set(r); this.anomaliesLoading.set(false); },
      error: () => this.anomaliesLoading.set(false),
    });
  }

  actionClass(action: string): string {
    if (action === 'login')        return 'bg-blue-100 text-blue-700';
    if (action === 'post.publish') return 'bg-emerald-100 text-emerald-700';
    if (action === 'post.reject')  return 'bg-red-100 text-red-600';
    if (action === 'post.create')  return 'bg-yellow-100 text-yellow-700';
    return 'bg-gray-100 text-gray-600';
  }

  anomalyBorder(s: string): string {
    return s === 'critical' ? 'border-red-200' : s === 'warning' ? 'border-yellow-200' : 'border-blue-100';
  }

  anomalyIcon(s: string): string {
    return s === 'critical' ? 'bg-red-100 text-red-600'
         : s === 'warning'  ? 'bg-yellow-100 text-yellow-700'
         : 'bg-blue-50 text-blue-600';
  }

  anomalyIconName(s: string): string {
    return s === 'critical' ? 'pi pi-exclamation-circle'
         : s === 'warning'  ? 'pi pi-exclamation-triangle'
         : 'pi pi-info-circle';
  }

  severityTag(s: string): 'danger' | 'warn' | 'info' {
    return s === 'critical' ? 'danger' : s === 'warning' ? 'warn' : 'info';
  }
}
