import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { AuthStore } from '@core/auth/auth.store';
import { AuthService } from '@core/auth/auth.service';
import { ThemeService } from '@core/theme/theme.service';
import { NotificationsStore } from '@core/stores/notifications.store';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';
import { Button } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { MenuModule } from 'primeng/menu';
import { Popover } from 'primeng/popover';
import { BadgeModule } from 'primeng/badge';
import { MenuItem } from 'primeng/api';

const LANGS = ['fr', 'en'] as const;
type Lang = typeof LANGS[number];

@Component({
  selector: 'app-navbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, AvatarModule, MenuModule, Popover, BadgeModule, TranslatePipe],
  template: `
    <header class="flex items-center justify-between px-4 py-3
                   bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 shadow-sm">

      <p-button icon="pi pi-bars" severity="secondary" [text]="true"
                (click)="toggleSidebar.emit()"
                styleClass="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" />

      <div class="flex items-center gap-2">

        <!-- Language switcher -->
        <div class="flex items-center rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden text-xs font-medium">
          @for (lang of langs; track lang) {
            <button type="button"
              (click)="setLang(lang)"
              [class]="currentLang() === lang
                ? 'px-2.5 py-1.5 bg-emerald-600 text-white'
                : 'px-2.5 py-1.5 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors'">
              {{ lang.toUpperCase() }}
            </button>
          }
        </div>

        <!-- Theme toggle -->
        <p-button [icon]="themeIcon()" severity="secondary" [text]="true"
                  (click)="theme.toggle()"
                  styleClass="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200" />

        <!-- Notification bell -->
        <div class="relative">
          <button type="button"
            (click)="notifPanel.toggle($event)"
            class="relative flex items-center justify-center w-9 h-9 rounded-lg
                   text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200
                   hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors">
            <i class="pi pi-bell text-base"></i>
            @if (notifStore.hasUnread()) {
              <span class="absolute top-1 right-1 min-w-[16px] h-4 px-1 rounded-full
                           bg-red-500 text-white text-[10px] font-bold flex items-center justify-center leading-none">
                {{ notifStore.badgeLabel() }}
              </span>
            }
          </button>
        </div>

        <!-- Notification panel -->
        <p-popover #notifPanel styleClass="!p-0 !w-80 sm:!w-96 !rounded-xl !shadow-xl overflow-hidden">
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-700">
            <span class="text-sm font-semibold text-gray-800 dark:text-gray-200">
              {{ 'notifications.title' | translate }}
              @if (notifStore.unreadCount() > 0) {
                <span class="ml-2 px-2 py-0.5 rounded-full bg-red-100 text-red-600 text-xs font-bold">
                  {{ notifStore.unreadCount() }}
                </span>
              }
            </span>
            @if (notifStore.hasUnread()) {
              <button (click)="markAllRead()" class="text-xs text-emerald-600 hover:underline">
                {{ 'notifications.mark_all_read' | translate }}
              </button>
            }
          </div>

          <div class="max-h-80 overflow-y-auto divide-y divide-gray-50 dark:divide-gray-800">
            @if (notifStore.notifications().length === 0) {
              <div class="text-center py-10">
                <i class="pi pi-bell text-2xl text-gray-300 block mb-2"></i>
                <p class="text-sm text-gray-400">{{ 'notifications.empty' | translate }}</p>
              </div>
            } @else {
              @for (n of notifStore.notifications(); track n.id) {
                <div (click)="onNotifClick(n)"
                  class="flex gap-3 px-4 py-3 cursor-pointer transition-colors"
                  [class]="n.is_read
                    ? 'hover:bg-gray-50 dark:hover:bg-gray-800'
                    : 'bg-emerald-50 dark:bg-emerald-900/20 hover:bg-emerald-100 dark:hover:bg-emerald-900/30'">
                  <div class="shrink-0 mt-0.5">
                    <div class="w-8 h-8 rounded-full flex items-center justify-center"
                      [class]="notifIconBg(n.type)">
                      <i [class]="notifIcon(n.type) + ' text-xs'"></i>
                    </div>
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-xs font-semibold text-gray-800 dark:text-gray-200 truncate">{{ n.title }}</p>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{{ n.body }}</p>
                    <p class="text-[10px] text-gray-400 mt-1">{{ formatDate(n.created_at) }}</p>
                  </div>
                  @if (!n.is_read) {
                    <div class="shrink-0 mt-1.5 w-2 h-2 rounded-full bg-emerald-500"></div>
                  }
                </div>
              }
            }
          </div>
        </p-popover>

        <!-- User menu -->
        <button type="button"
          class="flex items-center gap-2 rounded-lg px-2 py-1.5
                 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
          (click)="menu.toggle($event)">
          <p-avatar [label]="initials()" shape="circle" size="normal"
                    styleClass="bg-emerald-600 text-white text-xs font-semibold" />
          @if (fullName()) {
            <span class="hidden sm:block text-sm font-medium text-gray-700 dark:text-gray-300 max-w-28 truncate">
              {{ fullName() }}
            </span>
          }
          <i class="pi pi-chevron-down text-xs text-gray-400 hidden sm:block"></i>
        </button>

        <p-menu #menu [model]="userMenuItems" [popup]="true" />
      </div>
    </header>
  `,
})
export class AppNavbarComponent {
  readonly toggleSidebar = output<void>();

  readonly store = inject(AuthStore);
  readonly authService = inject(AuthService);
  readonly theme = inject(ThemeService);
  readonly notifStore = inject(NotificationsStore);
  private readonly translate = inject(TranslateService);

  readonly langs: Lang[] = ['fr', 'en'];
  readonly currentLang = signal<Lang>((localStorage.getItem('exp365_lang') ?? 'fr') as Lang);

  setLang(lang: Lang): void {
    this.translate.use(lang);
    this.currentLang.set(lang);
    localStorage.setItem('exp365_lang', lang);
  }

  themeIcon() {
    return this.theme.theme() === 'dark' ? 'pi pi-sun' : 'pi pi-moon';
  }

  initials(): string {
    const name = this.store.currentUser()?.full_name ?? '';
    return name.split(' ').map((n) => n[0]).slice(0, 2).join('').toUpperCase();
  }

  fullName(): string {
    return this.store.currentUser()?.full_name ?? '';
  }

  markAllRead(): void {
    this.notifStore.markAllRead();
  }

  onNotifClick(n: { id: string; is_read: boolean }): void {
    if (!n.is_read) this.notifStore.markRead(n.id);
  }

  notifIcon(type: string): string {
    if (type === 'charge_generated') return 'pi pi-credit-card text-amber-600';
    if (type === 'payment_confirmed') return 'pi pi-check-circle text-emerald-600';
    if (type === 'payment_recorded') return 'pi pi-clock text-blue-600';
    return 'pi pi-bell text-gray-500';
  }

  notifIconBg(type: string): string {
    if (type === 'charge_generated') return 'bg-amber-100 dark:bg-amber-900/30';
    if (type === 'payment_confirmed') return 'bg-emerald-100 dark:bg-emerald-900/30';
    if (type === 'payment_recorded') return 'bg-blue-100 dark:bg-blue-900/30';
    return 'bg-gray-100 dark:bg-gray-800';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('fr-FR', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
  }

  get userMenuItems(): MenuItem[] {
    const user = this.store.currentUser();
    const profileRoute = user?.entity_type === 'member' && user?.entity_id
      ? `/members/${user.entity_id}`
      : null;
    return [
      ...(profileRoute ? [{ label: 'Mon profil', icon: 'pi pi-user', routerLink: profileRoute }] : []),
      { separator: true },
      { label: 'Déconnexion', icon: 'pi pi-sign-out', command: () => this.authService.logout() },
    ];
  }
}
