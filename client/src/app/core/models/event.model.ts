import { PaginationMeta } from '@shared/models/pagination.model';

export interface EventDto {
  id: string;
  tenant_id: string;
  title: string;
  description?: string;
  type: string;
  status: string;
  location?: string;
  start_date: string;
  end_date: string;
  max_capacity?: number;
  registered_count: number;
  attended_count: number;
  is_public: boolean;
  created_at: string;
  updated_at?: string;
}

export interface EventRegistrationDto {
  id: string;
  event_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  status: string;
  attended_at?: string;
  created_at: string;
}

export interface EventStatsDto {
  total_count: number;
  draft_count: number;
  published_count: number;
  completed_count: number;
  cancelled_count: number;
  total_registrations: number;
  total_attended: number;
}

export interface CreateEventRequest {
  title: string;
  description?: string;
  type: string;
  location?: string;
  start_date: string;
  end_date: string;
  max_capacity?: number;
  is_public: boolean;
}

export interface UpdateEventRequest extends CreateEventRequest {}

export interface RegisterToEventRequest {
  member_id: string;
}

export interface AttendanceEntry {
  registration_id: string;
  status: string;
}

export interface MarkAttendanceRequest {
  entries: AttendanceEntry[];
}

export interface EventFilters {
  page: number;
  limit: number;
  status?: string;
  type?: string;
}

export const EVENT_TYPES = ['general', 'training', 'social', 'cultural', 'sports', 'administrative'] as const;
export const EVENT_STATUSES = ['draft', 'published', 'completed', 'cancelled'] as const;
