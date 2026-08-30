import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { PostSummaryDto } from '@models/post.model';
import { CommunityApiService } from '../../services/community-api.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-community-moderation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [ButtonModule, TagModule, ProgressSpinnerModule, TooltipModule, ConfirmDialog, TranslatePipe, DatePipe],
  template: `
    <p-confirmdialog />

    <div class="p-6 flex flex-col gap-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-800">{{ 'community.moderation_title' | translate }}</h1>
        <p class="text-gray-500 text-sm">{{ 'community.moderation_subtitle' | translate }}</p>
      </div>

      <!-- Tabs -->
      <div class="flex gap-2">
        @for (tab of tabs; track tab.value) {
          <button
            class="px-4 py-2 rounded-lg text-sm font-medium transition-colors"
            [class]="activeTab() === tab.value ? 'bg-emerald-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'"
            (click)="setTab(tab.value)">
            {{ tab.label }}
            @if (tab.value === 'draft') { <span class="ml-1 text-xs">({{ draftCount() }})</span> }
          </button>
        }
      </div>

      @if (loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (posts().length === 0) {
        <div class="text-center py-12 text-gray-400">
          <i class="pi pi-check-circle text-3xl mb-2 block"></i>
          <p>{{ 'community.no_pending' | translate }}</p>
        </div>
      } @else {
        <div class="flex flex-col gap-3">
          @for (post of posts(); track post.id) {
            <div class="bg-white border border-gray-100 rounded-xl p-4 flex items-start gap-4 shadow-sm">
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <p-tag [value]="post.status | translate" [severity]="statusSeverity(post.status)" />
                  <span class="text-xs text-gray-400">{{ post.author_name }} · {{ post.created_at | date:'mediumDate' }}</span>
                </div>
                <h3 class="font-semibold text-gray-800 truncate">{{ post.title }}</h3>
                <p class="text-sm text-gray-500 line-clamp-2 mt-1">{{ post.content_preview }}</p>
              </div>
              <div class="flex gap-2 shrink-0">
                @if (post.status === 'draft' || post.status === 'rejected') {
                  <p-button size="small" icon="pi pi-check" severity="success"
                    [pTooltip]="'community.publish' | translate"
                    (onClick)="publish(post)" />
                }
                @if (post.status === 'draft') {
                  <p-button size="small" icon="pi pi-times" severity="danger"
                    [pTooltip]="'community.reject' | translate"
                    (onClick)="reject(post)" />
                }
                <p-button size="small" icon="pi pi-trash" severity="secondary"
                  [pTooltip]="'common.delete' | translate"
                  (onClick)="confirmDelete(post)" />
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class CommunityModerationPage implements OnInit {
  private readonly api     = inject(CommunityApiService);
  private readonly confirm = inject(ConfirmationService);

  readonly loading    = signal(true);
  readonly posts      = signal<PostSummaryDto[]>([]);
  readonly draftCount = signal(0);
  readonly activeTab  = signal<string>('draft');

  readonly tabs = [
    { label: 'Brouillons', value: 'draft' },
    { label: 'Rejetés',    value: 'rejected' },
    { label: 'Publiés',    value: 'published' },
  ];

  ngOnInit(): void { this.load(); }

  setTab(tab: string): void {
    this.activeTab.set(tab);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.list({ status: this.activeTab(), limit: 50 }).subscribe({
      next: r => {
        this.posts.set(r.data);
        if (this.activeTab() === 'draft') this.draftCount.set(r.pagination.total);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  publish(post: PostSummaryDto): void {
    this.api.publish(post.id).subscribe(() => this.posts.update(l => l.filter(p => p.id !== post.id)));
  }

  reject(post: PostSummaryDto): void {
    this.api.reject(post.id).subscribe(() => this.posts.update(l => l.filter(p => p.id !== post.id)));
  }

  confirmDelete(post: PostSummaryDto): void {
    this.confirm.confirm({
      message: `Supprimer "${post.title}" ?`,
      header: 'Confirmation',
      icon: 'pi pi-trash',
      accept: () => {
        this.api.delete(post.id).subscribe(() => this.posts.update(l => l.filter(p => p.id !== post.id)));
      },
    });
  }

  statusSeverity(status: string): 'info' | 'warn' | 'success' | 'danger' | 'secondary' {
    return status === 'published' ? 'success' : status === 'rejected' ? 'danger' : 'warn';
  }
}
