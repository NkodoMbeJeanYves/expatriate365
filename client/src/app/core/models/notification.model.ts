import { PaginationMeta } from '../api/api-types';

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  body: string;
  is_read: boolean;
  created_at: string;
  data?: Record<string, unknown>;
}

export interface NotificationsResponse {
  data: AppNotification[];
  unread_count: number;
  pagination: PaginationMeta;
}
