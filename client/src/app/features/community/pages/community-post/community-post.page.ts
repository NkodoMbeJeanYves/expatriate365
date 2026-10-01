import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { FormsModule } from '@angular/forms';
import { PostAttachmentDto, PostCommentDto, PostDto, PostReactionCountDto } from '@models/post.model';
import { CommunityApiService } from '../../services/community-api.service';
import { AuthStore } from '@core/auth/auth.store';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-community-post',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, ProgressSpinnerModule, TranslatePipe, DatePipe, FormsModule],
  template: `
    <div class="p-6 max-w-3xl mx-auto flex flex-col gap-6">

      <a routerLink="/community" class="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-800 w-fit">
        <i class="pi pi-arrow-left"></i> {{ 'community.back_to_feed' | translate }}
      </a>

      @if (loading()) {
        <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
      } @else if (post()) {
        <article class="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 flex flex-col gap-5">

          <!-- Author -->
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-full bg-emerald-100 flex items-center justify-center text-emerald-700 font-bold">
              {{ post()!.author_name[0]?.toUpperCase() }}
            </div>
            <div>
              <p class="font-medium text-gray-800">{{ post()!.author_name }}</p>
              <p class="text-xs text-gray-400">
                {{ (post()!.published_at ?? post()!.created_at) | date:'longDate' }}
              </p>
            </div>
          </div>

          <!-- Title -->
          <h1 class="text-2xl font-bold text-gray-900">{{ post()!.title }}</h1>

          <!-- Content -->
          <div class="text-gray-700 leading-relaxed whitespace-pre-wrap">{{ post()!.content }}</div>

          <!-- Attachments -->
          @if (post()!.attachments.length > 0) {
            <div class="border-t border-gray-100 pt-4 flex flex-col gap-3">
              <h2 class="text-sm font-semibold text-gray-500 uppercase tracking-wide">
                {{ 'community.attachments' | translate }}
              </h2>

              <!-- Photos grid -->
              @if (photos().length > 0) {
                <div class="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  @for (photo of photos(); track photo.id) {
                    <a [href]="photo.file_url" target="_blank" rel="noopener">
                      <img [src]="photo.file_url" [alt]="photo.file_name"
                        class="w-full h-32 object-cover rounded-lg border border-gray-100 hover:opacity-90 transition-opacity" />
                    </a>
                  }
                </div>
              }

              <!-- Documents list -->
              @for (doc of documents(); track doc.id) {
                <a [href]="doc.file_url" [download]="doc.file_name" target="_blank" rel="noopener"
                  class="flex items-center gap-3 p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                  <i class="pi pi-file-pdf text-red-500 text-xl"></i>
                  <span class="text-sm text-gray-700 truncate flex-1">{{ doc.file_name }}</span>
                  <i class="pi pi-download text-gray-400 text-sm"></i>
                </a>
              }
            </div>
          }
        </article>

        <!-- Reactions -->
        <div class="flex items-center gap-2 flex-wrap">
          @for (r of reactions(); track r.reaction_type) {
            <button
              class="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm border transition-colors"
              [class]="r.user_reacted ? 'bg-indigo-50 border-indigo-300 text-indigo-700 font-semibold' : 'bg-gray-50 border-gray-200 text-gray-600 hover:bg-gray-100'"
              [disabled]="reacting()"
              (click)="toggleReaction(r.reaction_type)">
              <span>{{ reactionEmoji(r.reaction_type) }}</span>
              <span>{{ r.count }}</span>
            </button>
          }
          @if (!reactions().some(r => r.reaction_type === 'like')) {
            <button
              class="flex items-center gap-1.5 px-3 py-1.5 rounded-full text-sm border border-gray-200 bg-gray-50 text-gray-500 hover:bg-gray-100 transition-colors"
              [disabled]="reacting()"
              (click)="toggleReaction('like')">
              <span>👍</span><span class="text-xs">Réagir</span>
            </button>
          }
        </div>

        <!-- Comments -->
        <div class="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 flex flex-col gap-4">
          <h2 class="text-sm font-semibold text-gray-500 uppercase tracking-wide">
            Commentaires ({{ comments().length }})
          </h2>

          @for (c of comments(); track c.id) {
            <div class="flex gap-3">
              <div class="w-8 h-8 rounded-full bg-indigo-100 text-indigo-700 flex items-center justify-center text-xs font-bold shrink-0">
                {{ c.author_name[0]?.toUpperCase() }}
              </div>
              <div class="flex-1 bg-gray-50 rounded-xl px-4 py-3">
                <div class="flex items-center justify-between gap-2 mb-1">
                  <span class="text-xs font-semibold text-gray-700">{{ c.author_name }}</span>
                  <div class="flex items-center gap-1">
                    <span class="text-xs text-gray-400">{{ c.created_at | date:'d MMM y' }}</span>
                    <button class="text-gray-300 hover:text-red-500 transition-colors ml-1"
                      (click)="removeComment(c.id)" title="Supprimer">
                      <i class="pi pi-times text-xs"></i>
                    </button>
                  </div>
                </div>
                <p class="text-sm text-gray-700 whitespace-pre-wrap">{{ c.content }}</p>
              </div>
            </div>
          }

          @if (comments().length === 0) {
            <p class="text-sm text-gray-400 text-center py-2">Aucun commentaire pour l'instant.</p>
          }

          <div class="flex gap-3 pt-2 border-t border-gray-100">
            <div class="flex-1 flex gap-2">
              <textarea
                [(ngModel)]="newComment"
                rows="2"
                placeholder="Écrire un commentaire..."
                class="flex-1 text-sm border border-gray-200 rounded-xl px-3 py-2 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-200 bg-gray-50">
              </textarea>
              <button
                class="px-3 py-2 bg-indigo-600 text-white rounded-xl text-sm font-medium hover:bg-indigo-700 disabled:opacity-50 transition-colors"
                [disabled]="!newComment.trim() || commenting()"
                (click)="submitComment()">
                <i [class]="commenting() ? 'pi pi-spin pi-spinner' : 'pi pi-send'"></i>
              </button>
            </div>
          </div>
        </div>

      } @else {
        <p class="text-gray-400 text-center py-16">{{ 'community.post_not_found' | translate }}</p>
      }
    </div>
  `,
})
export class CommunityPostPage implements OnInit {
  private readonly api       = inject(CommunityApiService);
  private readonly route     = inject(ActivatedRoute);
  private readonly authStore = inject(AuthStore);

  readonly loading   = signal(true);
  readonly post      = signal<PostDto | null>(null);
  readonly comments  = signal<PostCommentDto[]>([]);
  readonly reactions = signal<PostReactionCountDto[]>([]);
  readonly reacting  = signal(false);
  readonly commenting = signal(false);
  newComment = '';

  readonly photos    = () => this.post()?.attachments.filter(a => a.attachment_type === 'photo') ?? [];
  readonly documents = () => this.post()?.attachments.filter(a => a.attachment_type === 'document') ?? [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.get(id).subscribe({
      next: p => {
        this.post.set(p);
        this.loading.set(false);
        this.api.listComments(id).subscribe({ next: c => this.comments.set(c) });
        this.api.getReactions(id).subscribe({ next: r => this.reactions.set(r) });
      },
      error: () => this.loading.set(false),
    });
  }

  toggleReaction(type: string): void {
    const postId = this.post()?.id;
    if (!postId) return;
    this.reacting.set(true);
    this.api.toggleReaction(postId, { reaction_type: type }).subscribe({
      next: updated => {
        this.reactions.update(list => {
          const exists = list.find(r => r.reaction_type === updated.reaction_type);
          if (exists) return list.map(r => r.reaction_type === updated.reaction_type ? updated : r);
          return updated.count > 0 ? [...list, updated] : list.filter(r => r.reaction_type !== updated.reaction_type);
        });
        this.reacting.set(false);
      },
      error: () => this.reacting.set(false),
    });
  }

  submitComment(): void {
    const postId = this.post()?.id;
    const content = this.newComment.trim();
    if (!postId || !content) return;
    this.commenting.set(true);
    this.api.addComment(postId, { content }).subscribe({
      next: c => {
        this.comments.update(list => [...list, c]);
        this.newComment = '';
        this.commenting.set(false);
      },
      error: () => this.commenting.set(false),
    });
  }

  removeComment(commentId: string): void {
    this.api.deleteComment(commentId).subscribe({
      next: () => this.comments.update(list => list.filter(c => c.id !== commentId)),
    });
  }

  reactionEmoji(type: string): string {
    const map: Record<string, string> = { like: '👍', heart: '❤️', clap: '👏', wow: '😮' };
    return map[type] ?? '👍';
  }
}
