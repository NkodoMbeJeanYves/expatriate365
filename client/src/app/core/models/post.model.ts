import { PaginationMeta } from '@shared/models/pagination.model';

export interface PostAttachmentDto {
  id: string;
  post_id: string;
  file_url: string;
  file_name: string;
  mime_type: string;
  file_size_bytes: number;
  attachment_type: 'photo' | 'document';
  created_at: string;
}

export interface PostDto {
  id: string;
  tenant_id: string;
  author_id: string;
  author_name: string;
  title: string;
  content: string;
  status: 'draft' | 'published' | 'rejected';
  published_at?: string;
  created_at: string;
  updated_at?: string;
  attachments: PostAttachmentDto[];
}

export interface PostSummaryDto {
  id: string;
  author_id: string;
  author_name: string;
  title: string;
  content_preview: string;
  status: 'draft' | 'published' | 'rejected';
  published_at?: string;
  created_at: string;
  attachment_count: number;
}

export interface PostPagedResult {
  data: PostSummaryDto[];
  pagination: PaginationMeta;
}

export interface CreatePostRequest {
  title: string;
  content: string;
}

export interface UpdatePostRequest {
  title: string;
  content: string;
}

export interface AddAttachmentRequest {
  file_url: string;
  file_name: string;
  mime_type: string;
  file_size_bytes: number;
  attachment_type: 'photo' | 'document';
}
