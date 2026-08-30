
export interface AuditLogDto {
  id: string;
  tenant_id?: string;
  tenant_name?: string;
  user_id: string;
  user_name: string;
  action: string;
  entity_type?: string;
  entity_id?: string;
  meta?: string;
  created_at: string;
}

export interface TenantStatsDto {
  id: string;
  name: string;
  slug: string;
  member_count: number;
  posts_published: number;
  posts_draft: number;
  posts_rejected: number;
  last_activity?: string;
}

export interface AnomalyDto {
  type: string;
  description: string;
  severity: 'critical' | 'warning' | 'info';
  tenant_id?: string;
  tenant_name?: string;
  entity_id?: string;
}
