import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '@core/config/app-config.token';
import {
  AddAttachmentRequest, AddCommentRequest, CreatePostRequest, PostAttachmentDto,
  PostCommentDto, PostDto, PostPagedResult, PostReactionCountDto, ToggleReactionRequest, UpdatePostRequest,
} from '@models/post.model';

@Injectable({ providedIn: 'root' })
export class CommunityApiService {
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private get base() { return `${this.config.apiUrl}/api/v1/posts`; }

  list(params: {
    page?: number; limit?: number;
    status?: string; author_id?: string; search?: string;
  } = {}): Observable<PostPagedResult> {
    let p = new HttpParams();
    if (params.page)      p = p.set('page',      params.page);
    if (params.limit)     p = p.set('limit',     params.limit);
    if (params.status)    p = p.set('status',    params.status);
    if (params.author_id) p = p.set('author_id', params.author_id);
    if (params.search)    p = p.set('search',    params.search);
    return this.http.get<PostPagedResult>(this.base + '/', { params: p });
  }

  get(id: string): Observable<PostDto> {
    return this.http.get<PostDto>(`${this.base}/${id}`);
  }

  create(request: CreatePostRequest): Observable<PostDto> {
    return this.http.post<PostDto>(this.base + '/', request);
  }

  update(id: string, request: UpdatePostRequest): Observable<PostDto> {
    return this.http.put<PostDto>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  publish(id: string): Observable<PostDto> {
    return this.http.post<PostDto>(`${this.base}/${id}/publish`, {});
  }

  reject(id: string): Observable<PostDto> {
    return this.http.post<PostDto>(`${this.base}/${id}/reject`, {});
  }

  addAttachment(postId: string, request: AddAttachmentRequest): Observable<PostAttachmentDto> {
    return this.http.post<PostAttachmentDto>(`${this.base}/${postId}/attachments`, request);
  }

  deleteAttachment(postId: string, attachmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${postId}/attachments/${attachmentId}`);
  }

  listComments(postId: string): Observable<PostCommentDto[]> {
    return this.http.get<PostCommentDto[]>(`${this.base}/${postId}/comments`);
  }

  addComment(postId: string, request: AddCommentRequest): Observable<PostCommentDto> {
    return this.http.post<PostCommentDto>(`${this.base}/${postId}/comments`, request);
  }

  deleteComment(commentId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/comments/${commentId}`);
  }

  getReactions(postId: string): Observable<PostReactionCountDto[]> {
    return this.http.get<PostReactionCountDto[]>(`${this.base}/${postId}/reactions`);
  }

  toggleReaction(postId: string, request: ToggleReactionRequest): Observable<PostReactionCountDto> {
    return this.http.post<PostReactionCountDto>(`${this.base}/${postId}/reactions`, request);
  }
}
