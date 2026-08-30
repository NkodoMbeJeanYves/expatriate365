import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ExploreApiService } from '../../services/explore-api.service';
import { PostDto } from '@models/post.model';

@Component({
  selector: 'app-explore-post',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, ButtonModule, ProgressSpinnerModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- Header -->
      <header class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 px-6 py-4
                     flex items-center justify-between">
        <div class="flex items-center gap-3">
          <a [routerLink]="['/explore', slug()]" class="text-gray-400 hover:text-gray-600 transition-colors">
            <i class="pi pi-arrow-left"></i>
          </a>
          <span class="font-semibold text-gray-700 dark:text-gray-300 text-sm">
            {{ 'explore.back_to_feed' | translate }}
          </span>
        </div>
        <a routerLink="/auth/login">
          <p-button [label]="'auth.login' | translate" icon="pi pi-sign-in" severity="secondary" size="small" />
        </a>
      </header>

      <main class="max-w-3xl mx-auto px-6 py-10">
        @if (loading()) {
          <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
        } @else if (!post()) {
          <div class="text-center py-16 text-gray-400">
            <i class="pi pi-exclamation-circle text-4xl mb-3 block"></i>
            <p>{{ 'community.post_not_found' | translate }}</p>
          </div>
        } @else {
          <article class="bg-white dark:bg-gray-900 rounded-2xl border border-gray-100
                          dark:border-gray-800 shadow-sm p-8 flex flex-col gap-6">

            <!-- Author + date -->
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-full bg-primary-100 dark:bg-primary-900/30
                          flex items-center justify-center">
                <i class="pi pi-user text-primary-600 dark:text-primary-400"></i>
              </div>
              <div>
                <p class="font-medium text-gray-800 dark:text-gray-200">{{ post()!.author_name }}</p>
                <p class="text-xs text-gray-400">
                  {{ (post()!.published_at ?? post()!.created_at) | date:'longDate' }}
                </p>
              </div>
            </div>

            <h1 class="text-2xl font-bold text-gray-900 dark:text-white leading-snug">
              {{ post()!.title }}
            </h1>

            <div class="text-gray-700 dark:text-gray-300 leading-relaxed whitespace-pre-wrap">
              {{ post()!.content }}
            </div>

            <!-- Photos -->
            @if (photos().length > 0) {
              <div class="flex flex-col gap-3">
                <h3 class="text-sm font-semibold text-gray-500 uppercase tracking-wider">
                  {{ 'community.photos' | translate }}
                </h3>
                <div class="grid grid-cols-2 sm:grid-cols-3 gap-3">
                  @for (photo of photos(); track photo.id) {
                    <a [href]="photo.file_url" target="_blank" rel="noopener">
                      <img [src]="photo.file_url" [alt]="photo.file_name"
                           class="w-full h-40 object-cover rounded-lg hover:opacity-90 transition-opacity" />
                    </a>
                  }
                </div>
              </div>
            }

            <!-- Documents -->
            @if (documents().length > 0) {
              <div class="flex flex-col gap-3">
                <h3 class="text-sm font-semibold text-gray-500 uppercase tracking-wider">
                  {{ 'community.documents' | translate }}
                </h3>
                <div class="flex flex-col gap-2">
                  @for (doc of documents(); track doc.id) {
                    <a [href]="doc.file_url" target="_blank" rel="noopener"
                       class="flex items-center gap-3 p-3 rounded-lg border border-gray-200
                              dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800
                              transition-colors no-underline">
                      <i class="pi pi-file-pdf text-red-500 text-xl"></i>
                      <div class="flex-1 min-w-0">
                        <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">
                          {{ doc.file_name }}
                        </p>
                        <p class="text-xs text-gray-400">{{ formatSize(doc.file_size_bytes) }}</p>
                      </div>
                      <i class="pi pi-download text-gray-400"></i>
                    </a>
                  }
                </div>
              </div>
            }

          </article>

          <!-- CTA -->
          <div class="mt-8 bg-primary-50 dark:bg-primary-900/20 border border-primary-200
                      dark:border-primary-800 rounded-xl p-6 text-center">
            <p class="font-semibold text-gray-800 dark:text-gray-200 mb-1">
              {{ 'explore.cta_title' | translate }}
            </p>
            <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
              {{ 'explore.cta_subtitle' | translate }}
            </p>
            <a routerLink="/auth/login">
              <p-button [label]="'explore.cta_button' | translate" icon="pi pi-sign-in" />
            </a>
          </div>
        }
      </main>
    </div>
  `,
})
export class ExplorePostPage implements OnInit {
  private readonly api   = inject(ExploreApiService);
  private readonly route = inject(ActivatedRoute);

  readonly slug    = signal('');
  readonly post    = signal<PostDto | null>(null);
  readonly loading = signal(true);

  readonly photos    = () => this.post()?.attachments.filter(a => a.attachment_type === 'photo') ?? [];
  readonly documents = () => this.post()?.attachments.filter(a => a.attachment_type === 'document') ?? [];

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug') ?? '';
    const id   = this.route.snapshot.paramMap.get('id') ?? '';
    this.slug.set(slug);
    this.api.getPost(slug, id).subscribe({
      next: (p) => { this.post.set(p); this.loading.set(false); },
      error: ()  => { this.post.set(null); this.loading.set(false); },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
