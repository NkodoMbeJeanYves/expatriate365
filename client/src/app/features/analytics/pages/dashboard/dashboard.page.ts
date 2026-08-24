import {
  ChangeDetectionStrategy, Component, inject, signal, computed,
} from '@angular/core';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval, startWith, switchMap, forkJoin, catchError, of } from 'rxjs';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '@core/auth/auth.store';
import { AnalyticsApiService } from '../../services/analytics-api.service';
import { AnalyticsOverviewDto } from '@models/analytics.model';
import { environment } from '../../../../../environments/environment';
import { PagedResult } from '@shared/models/pagination.model';

interface RecentMember { id: string; first_name: string; last_name: string; status: string; membership_number: string; }
interface UpcomingEvent { id: string; title: string; start_date: string; location?: string; registration_count: number; }
interface PendingWelfare { id: string; member_name: string; type: string; amount_requested: number; }

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, AppCurrencyPipe, SkeletonModule, TagModule, TranslatePipe],
  template: `
    <div class="flex flex-col gap-6 p-2">

      <!-- Header -->
      <div class="flex items-start justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ 'dashboard.greeting' | translate:{name: firstName()} }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ todayLabel }} · {{ 'dashboard.auto_refresh' | translate }}</p>
        </div>
        <button (click)="refresh()"
          class="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700
                 text-xs text-gray-500 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
          <i class="pi pi-refresh" [class.animate-spin]="loading()"></i> {{ 'common.refresh' | translate }}
        </button>
      </div>

      <!-- KPI Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-9 h-9 rounded-lg bg-emerald-500 flex items-center justify-center shrink-0">
              <i class="pi pi-users text-white text-sm"></i>
            </div>
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ 'dashboard.active_members' | translate }}</span>
          </div>
          @if (loading()) { <p-skeleton height="2rem" styleClass="mb-1" /><p-skeleton height="0.75rem" width="60%" /> }
          @else {
            <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ overview()?.active_members ?? '—' }}</div>
            <div class="text-xs text-gray-400 mt-1">{{ 'dashboard.new_this_month' | translate:{count: overview()?.new_members_this_month ?? 0} }}</div>
          }
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-9 h-9 rounded-lg bg-amber-500 flex items-center justify-center shrink-0">
              <i class="pi pi-credit-card text-white text-sm"></i>
            </div>
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ 'dashboard.pending_amount' | translate }}</span>
          </div>
          @if (loading()) { <p-skeleton height="2rem" styleClass="mb-1" /><p-skeleton height="0.75rem" width="60%" /> }
          @else {
            <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ (overview()?.pending_amount ?? 0) | appCurrency }}</div>
            <div class="text-xs mt-1" [class]="collectionRateClass()">{{ 'dashboard.collected_rate' | translate:{rate: collectionRate()} }}</div>
          }
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-9 h-9 rounded-lg bg-blue-500 flex items-center justify-center shrink-0">
              <i class="pi pi-calendar text-white text-sm"></i>
            </div>
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ 'dashboard.upcoming_events' | translate }}</span>
          </div>
          @if (loading()) { <p-skeleton height="2rem" styleClass="mb-1" /><p-skeleton height="0.75rem" width="60%" /> }
          @else {
            <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ overview()?.upcoming_events ?? '—' }}</div>
            <div class="text-xs text-gray-400 mt-1">{{ 'dashboard.total_events' | translate:{count: overview()?.total_events ?? 0} }}</div>
          }
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-9 h-9 rounded-lg bg-purple-500 flex items-center justify-center shrink-0">
              <i class="pi pi-wallet text-white text-sm"></i>
            </div>
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ 'dashboard.collected' | translate }}</span>
          </div>
          @if (loading()) { <p-skeleton height="2rem" styleClass="mb-1" /><p-skeleton height="0.75rem" width="60%" /> }
          @else {
            <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ (overview()?.total_collected ?? 0) | appCurrency }}</div>
            <div class="text-xs text-gray-400 mt-1">{{ 'dashboard.expected' | translate:{amount: (totalExpected() | appCurrency)} }}</div>
          }
        </div>
      </div>

      <!-- Progress bar -->
      @if (!loading() && overview()) {
        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 px-5 py-4">
          <div class="flex justify-between text-sm mb-2">
            <span class="text-gray-600 dark:text-gray-300 font-medium">{{ 'dashboard.collection_progress' | translate }}</span>
            <span class="font-bold" [class]="collectionRateClass()">{{ collectionRate() }}%</span>
          </div>
          <div class="h-2 bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
            <div class="h-full rounded-full transition-all duration-700"
              [class]="collectionRate() >= 75 ? 'bg-emerald-500' : collectionRate() >= 50 ? 'bg-amber-400' : 'bg-red-400'"
              [style.width.%]="collectionRate()"></div>
          </div>
          <div class="flex justify-between text-xs text-gray-400 mt-1">
            <span>{{ (overview()?.total_collected ?? 0) | appCurrency }} collectés</span>
            <span>{{ (overview()?.pending_amount ?? 0) | appCurrency }} en attente</span>
          </div>
        </div>
      }

      <!-- Activity widgets -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300">{{ 'dashboard.recent_members' | translate }}</h3>
            <a routerLink="/members" class="text-xs text-emerald-600 hover:underline">{{ 'common.see_all' | translate }}</a>
          </div>
          @if (loading()) {
            @for (_ of ph5; track $index) {
              <div class="flex items-center gap-3 py-2">
                <p-skeleton shape="circle" size="2rem" />
                <div class="flex-1"><p-skeleton height="0.75rem" styleClass="mb-1" /><p-skeleton height="0.6rem" width="60%" /></div>
              </div>
            }
          } @else if (recentMembers().length === 0) {
            <p class="text-sm text-gray-400 text-center py-4">{{ 'dashboard.no_members' | translate }}</p>
          } @else {
            @for (m of recentMembers(); track m.id) {
              <div class="flex items-center gap-3 py-2 border-b border-gray-50 dark:border-gray-700 last:border-0">
                <div class="w-8 h-8 rounded-full bg-emerald-100 dark:bg-emerald-900 text-emerald-700 dark:text-emerald-300
                            flex items-center justify-center text-xs font-bold shrink-0">{{ initials(m.first_name + ' ' + m.last_name) }}</div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ m.first_name }} {{ m.last_name }}</p>
                  <p class="text-xs text-gray-400">{{ m.membership_number }}</p>
                </div>
                <p-tag [value]="m.status"
                  [severity]="m.status === 'active' ? 'success' : m.status === 'pending' ? 'warn' : 'secondary'" />
              </div>
            }
          }
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300">{{ 'dashboard.upcoming_events_widget' | translate }}</h3>
            <a routerLink="/events" class="text-xs text-blue-600 hover:underline">{{ 'common.see_all' | translate }}</a>
          </div>
          @if (loading()) {
            @for (_ of ph3; track $index) {
              <div class="py-3 border-b border-gray-50 dark:border-gray-700">
                <p-skeleton height="0.75rem" styleClass="mb-1" /><p-skeleton height="0.6rem" width="50%" />
              </div>
            }
          } @else if (upcomingEvents().length === 0) {
            <p class="text-sm text-gray-400 text-center py-4">{{ 'dashboard.no_events' | translate }}</p>
          } @else {
            @for (e of upcomingEvents(); track e.id) {
              <div class="py-3 border-b border-gray-50 dark:border-gray-700 last:border-0">
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ e.title }}</p>
                <div class="flex items-center gap-3 mt-1">
                  <span class="text-xs text-blue-500"><i class="pi pi-calendar mr-1"></i>{{ e.start_date.slice(0, 10) }}</span>
                  @if (e.location) {
                    <span class="text-xs text-gray-400 truncate"><i class="pi pi-map-marker mr-1"></i>{{ e.location }}</span>
                  }
                </div>
                <p class="text-xs text-gray-400 mt-0.5">{{ 'dashboard.registrants' | translate:{count: e.registration_count} }}</p>
              </div>
            }
          }
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300">{{ 'dashboard.welfare_pending' | translate }}</h3>
            <a routerLink="/welfare" class="text-xs text-amber-600 hover:underline">{{ 'common.see_all' | translate }}</a>
          </div>
          @if (loading()) {
            @for (_ of ph3; track $index) {
              <div class="py-3 border-b border-gray-50 dark:border-gray-700">
                <p-skeleton height="0.75rem" styleClass="mb-1" /><p-skeleton height="0.6rem" width="40%" />
              </div>
            }
          } @else if (pendingWelfare().length === 0) {
            <div class="text-center py-6">
              <i class="pi pi-check-circle text-2xl text-emerald-400 mb-2 block"></i>
              <p class="text-sm text-gray-400">{{ 'dashboard.no_welfare' | translate }}</p>
            </div>
          } @else {
            @for (w of pendingWelfare(); track w.id) {
              <div class="py-3 border-b border-gray-50 dark:border-gray-700 last:border-0">
                <div class="flex items-start justify-between gap-2">
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ w.member_name }}</p>
                    <p class="text-xs text-gray-400">{{ w.type }}</p>
                  </div>
                  <span class="text-sm font-bold text-amber-600 shrink-0">{{ w.amount_requested | appCurrency }}</span>
                </div>
              </div>
            }
          }
        </div>
      </div>

      <!-- Module stats -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        @for (mod of moduleTiles; track mod.label) {
          <a [routerLink]="mod.route"
            class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4
                   hover:border-opacity-60 transition-colors" [class]="'hover:border-' + mod.border">
            <div class="flex items-center gap-2 mb-2">
              <i [class]="mod.icon + ' text-lg'"></i>
              <span class="text-xs text-gray-500 dark:text-gray-400">{{ mod.label | translate }}</span>
            </div>
            @if (loading()) { <p-skeleton height="1.5rem" width="50%" /> }
            @else { <div class="text-xl font-bold text-gray-800 dark:text-white">{{ mod.value() }}</div> }
          </a>
        }
      </div>

      <!-- Quick actions -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">{{ 'dashboard.quick_actions' | translate }}</h3>
        <div class="flex flex-wrap gap-3">
          @for (action of quickActions; track action.label) {
            <a [routerLink]="action.route"
              class="flex items-center gap-2 px-4 py-2 rounded-lg border border-gray-200 dark:border-gray-700
                     hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors text-sm font-medium
                     text-gray-700 dark:text-gray-300">
              <i [class]="action.icon + ' text-emerald-600'"></i>{{ action.label | translate }}
            </a>
          }
        </div>
      </div>

    </div>
  `,
})
export class DashboardPageComponent {
  private readonly authStore = inject(AuthStore);
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  readonly loading = signal(true);
  readonly overview = signal<AnalyticsOverviewDto | null>(null);
  readonly recentMembers = signal<RecentMember[]>([]);
  readonly upcomingEvents = signal<UpcomingEvent[]>([]);
  readonly pendingWelfare = signal<PendingWelfare[]>([]);

  readonly ph5 = Array(5);
  readonly ph3 = Array(3);

  readonly todayLabel = new Date().toLocaleDateString('fr-FR', {
    weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
  });

  readonly quickActions = [
    { label: 'dashboard.new_member',  icon: 'pi pi-user-plus',   route: '/members'        },
    { label: 'nav.contributions',     icon: 'pi pi-credit-card', route: '/contributions'  },
    { label: 'nav.events',            icon: 'pi pi-calendar',    route: '/events'         },
    { label: 'nav.communications',    icon: 'pi pi-envelope',    route: '/communications' },
    { label: 'nav.documents',         icon: 'pi pi-file',        route: '/documents'      },
    { label: 'nav.welfare',           icon: 'pi pi-heart',       route: '/welfare'        },
  ];

  readonly moduleTiles = [
    { label: 'nav.meetings',          icon: 'pi pi-microphone text-indigo-500', border: 'indigo-300', route: '/meetings',  value: () => this.overview()?.total_meetings  ?? '—' },
    { label: 'nav.elections',         icon: 'pi pi-check-square text-purple-500', border: 'purple-300', route: '/elections', value: () => this.overview()?.total_elections ?? '—' },
    { label: 'dashboard.total_members', icon: 'pi pi-users text-emerald-500',   border: 'emerald-300', route: '/members',   value: () => this.overview()?.total_members   ?? '—' },
    { label: 'nav.events',            icon: 'pi pi-calendar text-blue-500',     border: 'blue-300',   route: '/events',    value: () => this.overview()?.total_events    ?? '—' },
  ];

  readonly collectionRate = computed(() => {
    const o = this.overview();
    if (!o) return 0;
    const total = o.total_collected + o.pending_amount;
    return total > 0 ? Math.round(o.total_collected / total * 100) : 0;
  });

  readonly collectionRateClass = computed(() => {
    const r = this.collectionRate();
    return r >= 75 ? 'text-emerald-600' : r >= 50 ? 'text-amber-500' : 'text-red-500';
  });

  readonly totalExpected = computed(() => {
    const o = this.overview();
    return o ? o.total_collected + o.pending_amount : 0;
  });

  constructor() {
    interval(60_000).pipe(
      startWith(0),
      switchMap(() => this.fetchAll()),
      takeUntilDestroyed(),
    ).subscribe(res => this.applyResult(res));
  }

  refresh(): void {
    this.loading.set(true);
    this.fetchAll().subscribe(res => this.applyResult(res));
  }

  firstName(): string {
    const name = this.authStore.currentUser()?.full_name ?? '';
    return name.split(' ')[0] ?? name;
  }

  initials(fullName: string): string {
    return fullName.split(' ').map(n => n[0] ?? '').join('').toUpperCase().slice(0, 2);
  }

  private fetchAll() {
    return forkJoin({
      overview: this.analyticsApi.overview().pipe(catchError(() => of(null))),
      members:  this.http.get<PagedResult<RecentMember>>(`${this.apiUrl}/api/v1/members`,
                  { params: { page: 1, limit: 5 } }).pipe(catchError(() => of(null))),
      events:   this.http.get<PagedResult<UpcomingEvent>>(`${this.apiUrl}/api/v1/events`,
                  { params: { page: 1, limit: 5, status: 'upcoming' } }).pipe(catchError(() => of(null))),
      welfare:  this.http.get<PagedResult<PendingWelfare>>(`${this.apiUrl}/api/v1/welfare-requests`,
                  { params: { page: 1, limit: 5, status: 'pending' } }).pipe(catchError(() => of(null))),
    }).pipe(catchError(() => of(null)));
  }

  private applyResult(res: any): void {
    if (!res) { this.loading.set(false); return; }
    if (res.overview)  this.overview.set(res.overview);
    if (res.members)   this.recentMembers.set(res.members.data ?? []);
    if (res.events)    this.upcomingEvents.set(res.events.data ?? []);
    if (res.welfare)   this.pendingWelfare.set(res.welfare.data ?? []);
    this.loading.set(false);
  }
}
