import { ChangeDetectionStrategy, Component, OnInit, inject, signal, WritableSignal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ToastService } from '@service/toast.service';
import { ButtonModule } from 'primeng/button';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { NotificationsStore } from '@core/stores/notifications.store';
import { NotificationsApiService, NotificationPreferenceDto } from '@core/services/notifications-api.service';
import { AppNotification } from '@models/notification.model';
import { AppPaginatorComponent, PageChangeEvent } from '@shared/components/paginator/app-paginator.component';

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, FormsModule, ToggleSwitchModule, AppPaginatorComponent, TranslatePipe],
  template: `
    <div class="p-6 max-w-3xl mx-auto flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'notifications.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'notifications.subtitle' | translate }}</p>
        </div>
        @if (unreadCount() > 0) {
          <p-button severity="secondary" size="small" icon="pi pi-check-circle"
            [label]="'notifications.mark_all_read' | translate"
            (onClick)="markAllRead()" />
        }
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20"><i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i></div>
      } @else if (notifications().length === 0) {
        <div class="text-center py-20 text-gray-400">
          <i class="pi pi-bell text-5xl mb-4 block"></i>
          <p>{{ 'notifications.empty' | translate }}</p>
        </div>
      } @else {
        <div class="bg-white rounded-2xl border border-gray-100 shadow-sm divide-y divide-gray-50">
          @for (n of notifications(); track n.id) {
            <div class="flex items-start gap-4 p-4 transition-colors"
              [class]="[n.is_read ? 'hover:bg-gray-50' : 'bg-emerald-50 hover:bg-emerald-100', getRoute(n.entity_type, n.entity_id) ? 'cursor-pointer' : 'cursor-default'].join(' ')"
              (click)="markRead(n)">
              <div class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center text-white"
                [class]="iconBg(n.type)">
                <i [class]="'pi ' + icon(n.type) + ' text-sm'"></i>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-gray-800">{{ n.title }}</p>
                <p class="text-sm text-gray-500 mt-0.5">{{ n.body }}</p>
                <p class="text-xs text-gray-400 mt-1">{{ formatDate(n.created_at) }}</p>
              </div>
              @if (!n.is_read) {
                <div class="w-2.5 h-2.5 bg-emerald-500 rounded-full mt-1.5 flex-shrink-0"></div>
              }
            </div>
          }
        </div>

        <app-paginator [page]="currentPage" [limit]="30" [total]="total()" (pageChange)="onPageChange($event)" />
      }

      <!-- Preferences -->
      @if (preferences().length > 0) {
        <div class="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 flex flex-col gap-4">
          <h2 class="font-semibold text-gray-800">{{ 'notifications.preferences_title' | translate }}</h2>
          <div class="divide-y divide-gray-50">
            @for (pref of preferences(); track pref.type) {
              <div class="flex items-center justify-between py-3">
                <span class="text-sm text-gray-700">{{ ('notifications.type_' + pref.type) | translate }}</span>
                <p-toggleswitch [(ngModel)]="pref.enabled" (onChange)="savePreferences()" />
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class NotificationsPage implements OnInit {
  private readonly api       = inject(NotificationsApiService);
  private readonly store     = inject(NotificationsStore);
  private readonly router    = inject(Router);
  private readonly toast     = inject(ToastService);
  private readonly translate = inject(TranslateService);

  readonly notifications = signal<AppNotification[]>([]);
  readonly total         = signal(0);
  readonly loading       = signal(true);
  readonly unreadCount   = this.store.unreadCount;
  readonly preferences   = signal<NotificationPreferenceDto[]>([]);

  currentPage = 1;

  ngOnInit(): void {
    this.load();
    this.loadPreferences();
  }

  load(): void {
    this.loading.set(true);
    this.api.get(this.currentPage, 30).subscribe({
      next: res => {
        this.notifications.set(res.data);
        this.total.set(res.pagination.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onPageChange(e: PageChangeEvent): void {
    this.currentPage = e.page;
    this.load();
  }

  markRead(n: AppNotification): void {
    if (!n.is_read) {
      this.store.markRead(n.id);
      this.notifications.update(list => list.map(x => x.id === n.id ? { ...x, is_read: true } : x));
    }
    const route = this.getRoute(n.entity_type, n.entity_id);
    if (route) this.router.navigate([route]);
  }

  getRoute(entityType?: string, entityId?: string): string | null {
    if (!entityType || !entityId) return null;
    const map: Record<string, string> = {
      communication: `/admin/communications/${entityId}`,
      charge: `/contributions/charges/${entityId}`,
      payment: `/payments/${entityId}`,
      welfare: `/welfare/${entityId}`,
      event: `/events/${entityId}`,
      election: `/elections/${entityId}`,
      contribution_type: `/contributions`,
    };
    return map[entityType] ?? null;
  }

  markAllRead(): void {
    this.store.markAllRead();
    this.notifications.update(list => list.map(n => ({ ...n, is_read: true })));
  }

  loadPreferences(): void {
    this.api.getPreferences().subscribe({ next: prefs => this.preferences.set(prefs) });
  }

  savePreferences(): void {
    this.api.updatePreferences(this.preferences()).subscribe({
      next: () => this.toast.success(this.translate.instant('notifications.preferences_saved')),
      error: () => this.toast.error(this.translate.instant('common.generic_error')),
    });
  }

  icon(type: string): string {
    const map: Record<string, string> = {
      charge_generated: 'pi-euro', payment_confirmed: 'pi-check-circle',
      payment_recorded: 'pi-wallet', event_invite: 'pi-calendar', welfare_update: 'pi-heart',
    };
    return map[type] ?? 'pi-bell';
  }

  iconBg(type: string): string {
    const map: Record<string, string> = {
      charge_generated: 'bg-amber-500', payment_confirmed: 'bg-emerald-500',
      payment_recorded: 'bg-blue-500', event_invite: 'bg-indigo-500', welfare_update: 'bg-pink-500',
    };
    return map[type] ?? 'bg-gray-400';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }
}
