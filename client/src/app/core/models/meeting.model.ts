import { PaginationMeta } from '@shared/models/pagination.model';

export interface MeetingDto {
  id: string;
  tenant_id: string;
  title: string;
  type: string;
  status: string;
  scheduled_at: string;
  location?: string;
  agenda?: string;
  quorum_required?: number;
  attendance_count: number;
  present_count: number;
  has_minutes: boolean;
  started_at?: string;
  ended_at?: string;
  created_at: string;
  updated_at?: string;
}

export interface MeetingAttendanceDto {
  id: string;
  meeting_id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  status: string;
  proxy_name?: string;
  created_at: string;
}

export interface MeetingMinuteDto {
  id: string;
  meeting_id: string;
  content: string;
  decisions?: string;
  attachment_url?: string;
  is_approved: boolean;
  approved_at?: string;
  created_at: string;
  updated_at?: string;
}

export interface MeetingStatsDto {
  total: number;
  scheduled: number;
  in_progress: number;
  completed: number;
  cancelled: number;
}

export interface CreateMeetingRequest {
  title: string;
  type: string;
  scheduled_at: string;
  location?: string;
  agenda?: string;
  quorum_required?: number;
}

export interface UpdateMeetingRequest extends CreateMeetingRequest {}

export interface AttendanceEntry {
  member_id: string;
  status: string;
  proxy_name?: string;
}

export interface RecordAttendanceRequest {
  entries: AttendanceEntry[];
}

export interface SaveMinutesRequest {
  content: string;
  decisions?: string;
  attachment_url?: string;
}

export interface MeetingFilters {
  page: number;
  limit: number;
  status?: string;
  type?: string;
}

export const MEETING_TYPES = ['general', 'board', 'extraordinary'] as const;
export const MEETING_STATUSES = ['scheduled', 'in_progress', 'completed', 'cancelled'] as const;
export const ATTENDANCE_STATUSES = ['present', 'absent', 'excused', 'proxy'] as const;
