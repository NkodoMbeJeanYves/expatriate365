import { Injectable, computed, inject, signal } from '@angular/core';
import { take } from 'rxjs';
import { NotificationsApiService } from '@service/notifications-api.service';
import { AuthStore } from '@core/auth/auth.store';
import { AppNotification } from '@models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationsStore {
  private readonly api = inject(NotificationsApiService);
  private readonly auth = inject(AuthStore);

  readonly notifications = signal<AppNotification[]>([]);
  readonly unreadCount = signal(0);
  readonly loading = signal(false);

  readonly hasUnread = computed(() => this.unreadCount() > 0);
  readonly badgeLabel = computed(() => {
    const n = this.unreadCount();
    if (n === 0) return '';
    return n > 99 ? '99+' : String(n);
  });

  load(): void {
    if (!this.auth.isAuthenticated()) return;
    this.loading.set(true);
    this.api.get(1, 30).pipe(take(1)).subscribe({
      next: (res) => {
        this.notifications.set(res.data);
        this.unreadCount.set(res.unread_count);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  markRead(id: string): void {
    this.api.markRead(id).subscribe(() => {
      this.notifications.update(list =>
        list.map(n => n.id === id ? { ...n, is_read: true } : n)
      );
      this.unreadCount.update(n => Math.max(0, n - 1));
    });
  }

  markAllRead(): void {
    this.api.markAllRead().subscribe(() => {
      this.notifications.update(list => list.map(n => ({ ...n, is_read: true })));
      this.unreadCount.set(0);
    });
  }
}
