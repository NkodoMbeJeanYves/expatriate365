import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NotificationsStore } from './notifications.store';
import { NotificationsApiService } from '@service/notifications-api.service';
import { AuthStore } from '@core/auth/auth.store';
import { AppNotification } from '@models/notification.model';

const makeNotif = (id: string, is_read = false): AppNotification => ({
  id,
  type: 'charge_generated',
  title: `Notif ${id}`,
  body: 'body',
  is_read,
  created_at: new Date().toISOString(),
});

describe('NotificationsStore', () => {
  let store: NotificationsStore;
  let markReadSpy: ReturnType<typeof vi.fn>;
  let markAllReadSpy: ReturnType<typeof vi.fn>;
  let getSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    markReadSpy = vi.fn().mockReturnValue(of(undefined));
    markAllReadSpy = vi.fn().mockReturnValue(of({ marked: 2 }));
    getSpy = vi.fn().mockReturnValue(of({ data: [], unread_count: 0, pagination: { page: 1, limit: 30, total: 0 } }));

    TestBed.configureTestingModule({
      providers: [
        NotificationsStore,
        { provide: NotificationsApiService, useValue: { get: getSpy, markRead: markReadSpy, markAllRead: markAllReadSpy } },
        { provide: AuthStore, useValue: { isAuthenticated: () => true, currentUser: () => null } },
      ],
    });
    store = TestBed.inject(NotificationsStore);
  });

  it('markRead updates the notification to is_read=true', () => {
    store.notifications.set([makeNotif('n1', false), makeNotif('n2', false)]);
    store.unreadCount.set(2);
    store.markRead('n1');
    expect(store.notifications().find(n => n.id === 'n1')?.is_read).toBe(true);
    expect(store.notifications().find(n => n.id === 'n2')?.is_read).toBe(false);
  });

  it('markRead decrements unreadCount', () => {
    store.notifications.set([makeNotif('n1', false)]);
    store.unreadCount.set(3);
    store.markRead('n1');
    expect(store.unreadCount()).toBe(2);
  });

  it('markRead does not decrement below 0', () => {
    store.notifications.set([makeNotif('n1', false)]);
    store.unreadCount.set(0);
    store.markRead('n1');
    expect(store.unreadCount()).toBe(0);
  });

  it('markAllRead sets all notifications to is_read=true', () => {
    store.notifications.set([makeNotif('n1', false), makeNotif('n2', false)]);
    store.unreadCount.set(2);
    store.markAllRead();
    expect(store.notifications().every(n => n.is_read)).toBe(true);
  });

  it('markAllRead resets unreadCount to 0', () => {
    store.unreadCount.set(5);
    store.markAllRead();
    expect(store.unreadCount()).toBe(0);
  });

  it('hasUnread is true when unreadCount > 0', () => {
    store.unreadCount.set(1);
    expect(store.hasUnread()).toBe(true);
  });

  it('hasUnread is false when unreadCount is 0', () => {
    store.unreadCount.set(0);
    expect(store.hasUnread()).toBe(false);
  });

  it('badgeLabel returns string number when <= 99', () => {
    store.unreadCount.set(7);
    expect(store.badgeLabel()).toBe('7');
  });

  it('badgeLabel returns "99+" when > 99', () => {
    store.unreadCount.set(150);
    expect(store.badgeLabel()).toBe('99+');
  });

  it('badgeLabel returns empty string when 0', () => {
    store.unreadCount.set(0);
    expect(store.badgeLabel()).toBe('');
  });
});
