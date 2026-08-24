import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '@core/auth/auth.store';
import { TenantStore } from '@core/tenant/tenant.store';
import { NAV_GROUPS, NavItem } from '../nav.config';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  template: `
    <aside class="flex flex-col h-full bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800
                  transition-all duration-300 flex-shrink-0"
           [class.w-60]="!collapsed()" [class.w-16]="collapsed()">

      <!-- Logo -->
      <div class="flex items-center gap-3 px-4 py-5 border-b border-gray-200 dark:border-gray-700/50">
        @if (tenantStore.logoUrl()) {
          <img [src]="tenantStore.logoUrl()!" alt="logo"
               class="flex-shrink-0 w-8 h-8 rounded-lg object-contain bg-gray-100 dark:bg-white/10" />
        } @else {
          <div class="flex-shrink-0 w-8 h-8 rounded-lg bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center shadow">
            <span class="text-sm font-bold text-white">{{ tenantStore.name() ? tenantStore.name()[0].toUpperCase() : 'E' }}</span>
          </div>
        }
        @if (!collapsed()) {
          <span class="font-semibold text-gray-900 dark:text-white text-sm tracking-wide">{{ tenantStore.name() || 'Expatriate365' }}</span>
        }
      </div>

      <!-- Nav -->
      <nav class="flex-1 overflow-y-auto py-4 px-2 space-y-0.5">
        @for (group of visibleGroups(); track group.titleKey) {
          @if (group.titleKey && !collapsed()) {
            <p class="px-3 pt-4 pb-1 text-[10px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider">{{ group.titleKey }}</p>
          }
          @for (item of group.items; track item.route) {
            <a [routerLink]="item.route"
               routerLinkActive="bg-emerald-50 dark:bg-emerald-600/20 text-emerald-700 dark:text-emerald-400"
               [routerLinkActiveOptions]="{ exact: false }"
               class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-gray-500 dark:text-gray-400
                      hover:bg-gray-100 dark:hover:bg-gray-800 hover:text-gray-900 dark:hover:text-gray-100
                      transition-colors group"
               [title]="collapsed() ? (item.labelKey | translate) : ''">
              <i [class]="item.icon + ' text-base flex-shrink-0'"></i>
              @if (!collapsed()) {
                <span class="text-sm font-medium truncate">{{ item.labelKey | translate }}</span>
              }
            </a>
          }
          @if (!collapsed() && !$last) {
            <div class="my-2 border-t border-gray-100 dark:border-gray-700/30"></div>
          }
        }
      </nav>

      <!-- User section -->
      @if (store.user(); as user) {
        <div class="border-t border-gray-200 dark:border-gray-700/50 px-3 py-3 flex items-center gap-3 min-w-0">
          <div class="flex-shrink-0 w-8 h-8 rounded-full bg-emerald-600 flex items-center justify-center text-white text-sm font-semibold">
            {{ user.full_name ? user.full_name[0].toUpperCase() : 'U' }}
          </div>
          @if (!collapsed()) {
            <div class="min-w-0 flex flex-col">
              <span class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ user.full_name }}</span>
              @if (user.roles && user.roles.length > 0) {
                <span class="text-xs text-emerald-600 dark:text-emerald-400 truncate">{{ ('roles.label_' + user.roles[0]) | translate }}</span>
              }
            </div>
          }
        </div>
      }
    </aside>
  `,
})
export class AppSidebarComponent {
  readonly collapsed = input(false);
  readonly store = inject(AuthStore);
  readonly tenantStore = inject(TenantStore);
  readonly groups = NAV_GROUPS;

  visibleGroups() {
    return this.groups
      .map((g) => ({ ...g, items: g.items.filter((item) => this.isVisible(item)) }))
      .filter((g) => g.items.length > 0);
  }

  private isVisible(item: NavItem): boolean {
    if (!item.roles || item.roles.length === 0) return true;
    return this.store.hasAnyRole(item.roles);
  }
}
