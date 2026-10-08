import { PaginationMeta } from '../api/api-types';

export interface AppNotification {
  id: string;
  type: string;
  title: string;
  body: string;
  is_read: boolean;
  read_at?: string;
  created_at: string;
  entity_type?: string;
  entity_id?: string;
  data?: Record<string, unknown>;
}

export interface NotificationsResponse {
  data: AppNotification[];
  unread_count: number;
  pagination: PaginationMeta;
}
