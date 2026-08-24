export interface CommunicationDto {
  id: string;
  tenant_id: string;
  title: string;
  content: string;
  type: string;
  channel: string;
  status: string;
  audience: string;
  category_id?: string;
  target_member_id?: string;
  recipient_count: number;
  read_count: number;
  sent_at?: string;
  created_at: string;
  updated_at?: string;
}

export interface RecipientDto {
  id: string;
  member_id: string;
  member_name: string;
  membership_number: string;
  status: string;
  read_at?: string;
  created_at: string;
}

export interface CommunicationStatsDto {
  total: number;
  draft: number;
  sent: number;
  total_recipients: number;
  total_read: number;
}

export interface CreateCommunicationRequest {
  title: string;
  content: string;
  type: string;
  channel: string;
  audience: string;
  category_id?: string;
  target_member_id?: string;
}

export interface UpdateCommunicationRequest extends CreateCommunicationRequest {}

export interface CommunicationFilters {
  page: number;
  limit: number;
  status?: string;
  type?: string;
  channel?: string;
}

export const COMMUNICATION_TYPES = ['announcement', 'newsletter', 'reminder', 'alert'] as const;
export const COMMUNICATION_CHANNELS = ['app', 'email', 'sms'] as const;
export const COMMUNICATION_AUDIENCES = ['all', 'category', 'individual'] as const;
export const COMMUNICATION_STATUSES = ['draft', 'sent'] as const;
