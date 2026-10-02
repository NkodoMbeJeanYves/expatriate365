import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ProgressSpinner } from 'primeng/progressspinner';
import { forkJoin } from 'rxjs';
import { AuthStore } from '@core/auth/auth.store';
import { TenantStore } from '@core/tenant/tenant.store';
import { ContributionStats } from '@models/contribution.model';
import { PaymentStats } from '@models/payment.model';
import { EventDto } from '@models/event.model';
import { AppNotification } from '@models/notification.model';
import { WelfareStats } from '@models/welfare.model';
import { ContributionsApiService } from '../contributions/services/contributions-api.service';
import { PaymentsApiService } from '../payments/services/payments-api.service';
import { EventsApiService } from '../events/services/events-api.service';
import { WelfareApiService } from '../welfare/services/welfare-api.service';
import { NotificationsApiService } from '@core/services/notifications-api.service';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { WelcomeBannerComponent } from '@shared/components/welcome-banner/welcome-banner.component';

@Component({
  selector: 'app-member-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, RouterLink, ProgressSpinner, AppCurrencyPipe, WelcomeBannerComponent, DatePipe],
  template: `
    <div class="p-6 max-w-4xl mx-auto">

      <!-- Onboarding banner -->
      <app-welcome-banner />

      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-2xl font-semibold text-gray-900 dark:text-white">
          {{ 'member_dashboard.title' | translate }}
        </h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {{ authStore.user()?.full_name }}
          &nbsp;·&nbsp;
          {{ tenantStore.name() }}
        </p>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <p-progressspinner strokeWidth="4" styleClass="w-12 h-12" />
        </div>
      } @else {

        <!-- Status banner -->
        @if (contribStats(); as cs) {
          <div [class]="cs.overdue_count > 0
            ? 'flex items-center gap-3 rounded-xl px-4 py-3 mb-6 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300'
            : 'flex items-center gap-3 rounded-xl px-4 py-3 mb-6 bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300'">
            <i [class]="cs.overdue_count > 0 ? 'pi pi-exclamation-triangle text-lg' : 'pi pi-check-circle text-lg'"></i>
            <span class="text-sm font-medium">
              {{ cs.overdue_count > 0
                ? ('member_dashboard.has_overdue' | translate)
                : ('member_dashboard.good_standing' | translate) }}
            </span>
          </div>
        }

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6">

          <!-- Contributions card -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between">
              <div class="flex items-center gap-2">
                <i class="pi pi-wallet text-emerald-600 dark:text-emerald-400"></i>
                <h2 class="text-sm font-semibold text-gray-800 dark:text-white">
                  {{ 'member_dashboard.my_contributions' | translate }}
                </h2>
              </div>
            </div>

            @if (contribStats(); as cs) {
              <div class="p-5 grid grid-cols-2 gap-4">
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.total_expected' | translate }}</div>
                  <div class="text-lg font-semibold text-gray-800 dark:text-white">{{ cs.total_expected | appCurrency }}</div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.total_paid' | translate }}</div>
                  <div class="text-lg font-semibold text-emerald-600 dark:text-emerald-400">{{ cs.total_collected | appCurrency }}</div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.balance' | translate }}</div>
                  <div [class]="cs.total_pending > 0 ? 'text-lg font-semibold text-red-600 dark:text-red-400' : 'text-lg font-semibold text-gray-800 dark:text-white'">
                    {{ cs.total_pending | appCurrency }}
                  </div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.overdue' | translate }}</div>
                  <div [class]="cs.overdue_count > 0 ? 'text-lg font-semibold text-red-600 dark:text-red-400' : 'text-lg font-semibold text-gray-800 dark:text-white'">
                    {{ cs.overdue_count }}
                  </div>
                </div>
              </div>
            }

            <div class="px-5 pb-4">
              <a routerLink="/contributions"
                 class="inline-flex items-center gap-2 text-xs text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 font-medium transition-colors">
                {{ 'member_dashboard.view_contributions' | translate }}
                <i class="pi pi-arrow-right text-xs"></i>
              </a>
            </div>
          </div>

          <!-- Payments card -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center gap-2">
              <i class="pi pi-credit-card text-emerald-600 dark:text-emerald-400"></i>
              <h2 class="text-sm font-semibold text-gray-800 dark:text-white">
                {{ 'member_dashboard.my_payments' | translate }}
              </h2>
            </div>

            @if (paymentStats(); as ps) {
              <div class="p-5 grid grid-cols-2 gap-4">
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.confirmed' | translate }}</div>
                  <div class="text-lg font-semibold text-emerald-600 dark:text-emerald-400">{{ ps.total_confirmed | appCurrency }}</div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.pending' | translate }}</div>
                  <div [class]="ps.total_pending > 0 ? 'text-lg font-semibold text-amber-600 dark:text-amber-400' : 'text-lg font-semibold text-gray-800 dark:text-white'">
                    {{ ps.total_pending | appCurrency }}
                  </div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'payments.status_confirmed' | translate }}</div>
                  <div class="text-lg font-semibold text-gray-800 dark:text-white">{{ ps.confirmed_count }}</div>
                </div>
                <div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.pending' | translate }}</div>
                  <div class="text-lg font-semibold text-gray-800 dark:text-white">{{ ps.pending_count }}</div>
                </div>
              </div>
            }

            <div class="px-5 pb-4">
              <a routerLink="/payments"
                 class="inline-flex items-center gap-2 text-xs text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 dark:hover:text-emerald-300 font-medium transition-colors">
                {{ 'member_dashboard.view_payments' | translate }}
                <i class="pi pi-arrow-right text-xs"></i>
              </a>
            </div>
          </div>

          <!-- Upcoming events card -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center gap-2">
              <i class="pi pi-calendar text-blue-600 dark:text-blue-400"></i>
              <h2 class="text-sm font-semibold text-gray-800 dark:text-white">
                {{ 'member_dashboard.upcoming_events' | translate }}
              </h2>
            </div>

            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              @if (upcomingEvents().length === 0) {
                <p class="px-5 py-4 text-sm text-gray-400 dark:text-gray-500">
                  {{ 'member_dashboard.no_upcoming_events' | translate }}
                </p>
              }
              @for (ev of upcomingEvents(); track ev.id) {
                <div class="px-5 py-3">
                  <div class="text-sm font-medium text-gray-800 dark:text-white truncate">{{ ev.title }}</div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
                    {{ ev.start_date | date:'mediumDate' }}
                    @if (ev.location) { &nbsp;·&nbsp; {{ ev.location }} }
                  </div>
                </div>
              }
            </div>

            <div class="px-5 pb-4 pt-2">
              <a routerLink="/events"
                 class="inline-flex items-center gap-2 text-xs text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 font-medium transition-colors">
                {{ 'member_dashboard.view_events' | translate }}
                <i class="pi pi-arrow-right text-xs"></i>
              </a>
            </div>
          </div>

          <!-- Recent notifications card -->
          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center gap-2">
              <i class="pi pi-bell text-purple-600 dark:text-purple-400"></i>
              <h2 class="text-sm font-semibold text-gray-800 dark:text-white">
                {{ 'member_dashboard.recent_notifications' | translate }}
              </h2>
            </div>

            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              @if (recentNotifications().length === 0) {
                <p class="px-5 py-4 text-sm text-gray-400 dark:text-gray-500">
                  {{ 'member_dashboard.no_notifications' | translate }}
                </p>
              }
              @for (notif of recentNotifications(); track notif.id) {
                <div class="px-5 py-3 flex items-start gap-2">
                  @if (!notif.is_read) {
                    <span class="mt-1.5 w-2 h-2 rounded-full bg-purple-500 shrink-0"></span>
                  } @else {
                    <span class="mt-1.5 w-2 h-2 rounded-full bg-transparent shrink-0"></span>
                  }
                  <div class="min-w-0">
                    <div class="text-sm font-medium text-gray-800 dark:text-white truncate">{{ notif.title }}</div>
                    <div class="text-xs text-gray-400 dark:text-gray-500 line-clamp-1">{{ notif.body }}</div>
                  </div>
                </div>
              }
            </div>

            <div class="px-5 pb-4 pt-2">
              <a routerLink="/notifications"
                 class="inline-flex items-center gap-2 text-xs text-purple-600 dark:text-purple-400 hover:text-purple-800 dark:hover:text-purple-300 font-medium transition-colors">
                {{ 'member_dashboard.view_notifications' | translate }}
                <i class="pi pi-arrow-right text-xs"></i>
              </a>
            </div>
          </div>

          <!-- Welfare summary card -->
          @if (welfareStats(); as ws) {
            @if (ws.pending_count > 0 || ws.approved_count > 0) {
              <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden md:col-span-2">
                <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex items-center gap-2">
                  <i class="pi pi-heart text-rose-600 dark:text-rose-400"></i>
                  <h2 class="text-sm font-semibold text-gray-800 dark:text-white">
                    {{ 'member_dashboard.welfare_summary' | translate }}
                  </h2>
                </div>
                <div class="p-5 grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div>
                    <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.welfare_pending' | translate }}</div>
                    <div [class]="ws.pending_count > 0 ? 'text-lg font-semibold text-amber-600 dark:text-amber-400' : 'text-lg font-semibold text-gray-800 dark:text-white'">
                      {{ ws.pending_count }}
                    </div>
                  </div>
                  <div>
                    <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.welfare_approved' | translate }}</div>
                    <div class="text-lg font-semibold text-emerald-600 dark:text-emerald-400">{{ ws.approved_count }}</div>
                  </div>
                  <div>
                    <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.welfare_approved_amount' | translate }}</div>
                    <div class="text-lg font-semibold text-gray-800 dark:text-white">{{ ws.total_approved | appCurrency }}</div>
                  </div>
                  <div>
                    <div class="text-xs text-gray-400 dark:text-gray-500 mb-1">{{ 'member_dashboard.welfare_paid' | translate }}</div>
                    <div class="text-lg font-semibold text-gray-800 dark:text-white">{{ ws.total_paid | appCurrency }}</div>
                  </div>
                </div>
                <div class="px-5 pb-4">
                  <a routerLink="/welfare"
                     class="inline-flex items-center gap-2 text-xs text-rose-600 dark:text-rose-400 hover:text-rose-800 dark:hover:text-rose-300 font-medium transition-colors">
                    {{ 'member_dashboard.view_welfare' | translate }}
                    <i class="pi pi-arrow-right text-xs"></i>
                  </a>
                </div>
              </div>
            }
          }

        </div>
      }
    </div>
  `,
})
export class MemberDashboardPage implements OnInit {
  readonly authStore   = inject(AuthStore);
  readonly tenantStore = inject(TenantStore);
  private readonly contributionsApi    = inject(ContributionsApiService);
  private readonly paymentsApi         = inject(PaymentsApiService);
  private readonly eventsApi           = inject(EventsApiService);
  private readonly welfareApi          = inject(WelfareApiService);
  private readonly notificationsApi    = inject(NotificationsApiService);

  readonly loading              = signal(true);
  readonly contribStats         = signal<ContributionStats | null>(null);
  readonly paymentStats         = signal<PaymentStats | null>(null);
  readonly upcomingEvents       = signal<EventDto[]>([]);
  readonly recentNotifications  = signal<AppNotification[]>([]);
  readonly welfareStats         = signal<WelfareStats | null>(null);

  ngOnInit(): void {
    forkJoin({
      contributions:  this.contributionsApi.getStats(),
      payments:       this.paymentsApi.getStats(),
      events:         this.eventsApi.list({ page: 1, limit: 3, status: 'published' }),
      notifications:  this.notificationsApi.get(1, 3),
      welfare:        this.welfareApi.getStats(),
    }).subscribe({
      next: ({ contributions, payments, events, notifications, welfare }) => {
        this.contribStats.set(contributions);
        this.paymentStats.set(payments);
        this.upcomingEvents.set(events.data ?? []);
        this.recentNotifications.set(notifications.data ?? []);
        this.welfareStats.set(welfare);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
