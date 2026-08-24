import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { forkJoin } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ChartModule } from 'primeng/chart';
import {
  AnalyticsOverviewDto, MemberAnalyticsDto, FinanceAnalyticsDto, EngagementAnalyticsDto,
} from '@models/analytics.model';
import { AnalyticsApiService } from '../../services/analytics-api.service';

@Component({
  selector: 'app-analytics-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppCurrencyPipe, ButtonModule, ProgressSpinnerModule, ChartModule, TranslatePipe],
  template: `
    <div class="p-6 flex flex-col gap-8">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'analytics.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'analytics.subtitle' | translate }}</p>
        </div>
        <p-button icon="pi pi-refresh" severity="secondary" [label]="'common.refresh' | translate" (onClick)="load()" />
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20"><p-progressspinner strokeWidth="4" /></div>
      } @else {

        <!-- KPIs globaux -->
        @if (overview()) {
          <section>
            <h2 class="text-lg font-semibold text-gray-700 mb-4">{{ 'analytics.overview' | translate }}</h2>
            <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-5 gap-4">
              <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
                <div class="text-3xl font-bold text-indigo-700">{{ overview()!.total_members }}</div>
                <div class="text-xs text-gray-500 mt-1">{{ 'analytics.total_members' | translate }}</div>
              </div>
              <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
                <div class="text-3xl font-bold text-green-700">{{ overview()!.active_members }}</div>
                <div class="text-xs text-green-600 mt-1">{{ 'analytics.active_members' | translate }}</div>
              </div>
              <div class="bg-blue-50 rounded-xl p-4 border border-blue-100 shadow-sm text-center">
                <div class="text-3xl font-bold text-blue-700">+{{ overview()!.new_members_this_month }}</div>
                <div class="text-xs text-blue-600 mt-1">{{ 'analytics.new_this_month' | translate }}</div>
              </div>
              <div class="bg-yellow-50 rounded-xl p-4 border border-yellow-100 shadow-sm text-center">
                <div class="text-3xl font-bold text-yellow-700">{{ overview()!.total_collected | appCurrency }}</div>
                <div class="text-xs text-yellow-600 mt-1">{{ 'analytics.total_collected' | translate }}</div>
              </div>
              <div class="bg-red-50 rounded-xl p-4 border border-red-100 shadow-sm text-center">
                <div class="text-3xl font-bold text-red-600">{{ overview()!.pending_amount | appCurrency }}</div>
                <div class="text-xs text-red-500 mt-1">{{ 'analytics.pending_amount' | translate }}</div>
              </div>
            </div>
          </section>
        }

        <!-- Membres -->
        @if (members()) {
          <section class="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
              <h2 class="text-base font-semibold text-gray-700 mb-4">{{ 'analytics.member_growth' | translate }}</h2>
              <p-chart type="bar" [data]="memberGrowthChartData()" [options]="barOptions" height="220" />
            </div>
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
              <h2 class="text-base font-semibold text-gray-700 mb-4">{{ 'analytics.members_by_status' | translate }}</h2>
              <p-chart type="doughnut" [data]="memberStatusChartData()" [options]="doughnutOptions" height="220" />
            </div>
          </section>
        }

        <!-- Finances -->
        @if (finance()) {
          <section>
            <div class="flex items-center gap-4 mb-4">
              <h2 class="text-lg font-semibold text-gray-700">{{ 'analytics.finance_analytics' | translate }}</h2>
              <span class="text-sm text-gray-500">
                {{ 'analytics.collection_rate' | translate }} : <strong class="text-green-700">{{ finance()!.collection_rate }}%</strong>
              </span>
            </div>
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5">
              <p-chart type="line" [data]="financeChartData()" [options]="lineOptions" height="220" />
            </div>
          </section>
        }

        <!-- Engagement -->
        @if (engagement()) {
          <section>
            <h2 class="text-lg font-semibold text-gray-700 mb-4">{{ 'analytics.engagement' | translate }}</h2>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5 text-center">
                <div class="text-4xl font-bold text-indigo-700 mb-1">{{ engagement()!.meeting_attendance_rate }}%</div>
                <div class="text-sm text-gray-500">{{ 'analytics.meeting_attendance' | translate }}</div>
                <div class="mt-3 h-2 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-indigo-500 rounded-full transition-all"
                    [style.width.%]="engagement()!.meeting_attendance_rate"></div>
                </div>
              </div>
              <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5 text-center">
                <div class="text-4xl font-bold text-purple-700 mb-1">{{ engagement()!.election_participation_rate }}%</div>
                <div class="text-sm text-gray-500">{{ 'analytics.election_participation' | translate }}</div>
                <div class="mt-3 h-2 bg-gray-100 rounded-full overflow-hidden">
                  <div class="h-full bg-purple-500 rounded-full transition-all"
                    [style.width.%]="engagement()!.election_participation_rate"></div>
                </div>
              </div>
              <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-5 text-center">
                <div class="text-4xl font-bold text-teal-700 mb-1">{{ engagement()!.total_event_registrations }}</div>
                <div class="text-sm text-gray-500">{{ 'analytics.event_registrations' | translate }}</div>
              </div>
            </div>
          </section>
        }
      }
    </div>
  `,
})
export class AnalyticsPage implements OnInit {
  private readonly api = inject(AnalyticsApiService);
  private readonly translate = inject(TranslateService);

  readonly loading = signal(true);
  readonly overview = signal<AnalyticsOverviewDto | null>(null);
  readonly members = signal<MemberAnalyticsDto | null>(null);
  readonly finance = signal<FinanceAnalyticsDto | null>(null);
  readonly engagement = signal<EngagementAnalyticsDto | null>(null);

  readonly barOptions = { plugins: { legend: { display: false } }, scales: { y: { beginAtZero: true } } };
  readonly lineOptions = { plugins: { legend: { display: true } } };
  readonly doughnutOptions = { plugins: { legend: { position: 'right' as const } } };

  readonly memberGrowthChartData = () => {
    const m = this.members();
    if (!m) return undefined;
    return {
      labels: m.monthly_growth.map(p => p.month),
      datasets: [{ label: this.translate.instant('analytics.new_this_month'), data: m.monthly_growth.map(p => p.value), backgroundColor: '#6366f1' }],
    };
  };

  readonly memberStatusChartData = () => {
    const m = this.members();
    if (!m) return undefined;
    const colors = ['#22c55e', '#f59e0b', '#ef4444', '#6366f1', '#64748b'];
    return {
      labels: m.by_status.map(s => s.status),
      datasets: [{ data: m.by_status.map(s => s.count), backgroundColor: colors }],
    };
  };

  readonly financeChartData = () => {
    const f = this.finance();
    if (!f) return undefined;
    return {
      labels: f.monthly_collected.map(p => p.month),
      datasets: [
        { label: this.translate.instant('analytics.monthly_collected'), data: f.monthly_collected.map(p => p.value), borderColor: '#22c55e', fill: false, tension: 0.4 },
        { label: this.translate.instant('analytics.monthly_expected'), data: f.monthly_expected.map(p => p.value), borderColor: '#f59e0b', fill: false, tension: 0.4, borderDash: [5, 5] },
      ],
    };
  };

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    forkJoin({
      overview: this.api.overview(),
      members: this.api.members(),
      finance: this.api.finance(),
      engagement: this.api.engagement(),
    }).subscribe({
      next: ({ overview, members, finance, engagement }) => {
        this.overview.set(overview);
        this.members.set(members);
        this.finance.set(finance);
        this.engagement.set(engagement);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
