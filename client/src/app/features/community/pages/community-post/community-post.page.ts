import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { PostAttachmentDto, PostDto } from '@models/post.model';
import { CommunityApiService } from '../../services/community-api.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-community-post',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, ProgressSpinnerModule, TranslatePipe, DatePipe],
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
                <a [href]="doc.file_url" target="_blank" rel="noopener"
                  class="flex items-center gap-3 p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition-colors">
                  <i class="pi pi-file-pdf text-red-500 text-xl"></i>
                  <span class="text-sm text-gray-700 truncate flex-1">{{ doc.file_name }}</span>
                  <i class="pi pi-download text-gray-400 text-sm"></i>
                </a>
              }
            </div>
          }
        </article>
      } @else {
        <p class="text-gray-400 text-center py-16">{{ 'community.post_not_found' | translate }}</p>
      }
    </div>
  `,
})
export class CommunityPostPage implements OnInit {
  private readonly api   = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly post    = signal<PostDto | null>(null);

  readonly photos    = () => this.post()?.attachments.filter(a => a.attachment_type === 'photo') ?? [];
  readonly documents = () => this.post()?.attachments.filter(a => a.attachment_type === 'document') ?? [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.get(id).subscribe({
      next:  p => { this.post.set(p); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
