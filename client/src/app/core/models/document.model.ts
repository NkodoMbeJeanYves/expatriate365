export interface DocumentDto {
  id: string;
  tenant_id: string;
  title: string;
  description?: string;
  type: string;
  category: string;
  file_name: string;
  file_url: string;
  file_size_bytes: number;
  mime_type: string;
  is_public: boolean;
  uploaded_by: string;
  uploader_name: string;
  created_at: string;
  updated_at?: string;
}

export interface DocumentStatsDto {
  total: number;
  public: number;
  private: number;
}

export interface CreateDocumentRequest {
  title: string;
  description?: string;
  type: string;
  category: string;
  file_name: string;
  file_url: string;
  file_size_bytes: number;
  mime_type: string;
  is_public: boolean;
}

export interface UpdateDocumentRequest {
  title: string;
  description?: string;
  type: string;
  category: string;
  is_public: boolean;
}

export interface DocumentFilters {
  page: number;
  limit: number;
  type?: string;
  category?: string;
  search?: string;
}

export const DOCUMENT_TYPES = ['bylaw', 'minutes', 'report', 'form', 'other'] as const;
export const DOCUMENT_CATEGORIES = ['general', 'administrative', 'financial', 'legal'] as const;
