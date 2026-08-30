import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, output, signal, viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { PostDto } from '@models/post.model';
import { CommunityApiService } from '../../services/community-api.service';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-post-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DrawerModule, ButtonModule, InputTextModule, TextareaModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [header]="drawerTitle"
      position="right" styleClass="!w-full md:!w-[600px]">

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 p-2">

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'community.post_title' | translate }} <span class="text-red-500">*</span></label>
          <input pInputText formControlName="title" [placeholder]="'community.post_title_placeholder' | translate" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'community.post_content' | translate }} <span class="text-red-500">*</span></label>
          <textarea pTextarea formControlName="content" rows="10" class="w-full"
            [placeholder]="'community.post_content_placeholder' | translate"></textarea>
        </div>

        <!-- Attachments (photos + docs) -->
        <div class="flex flex-col gap-2">
          <label class="text-sm font-medium">{{ 'community.attachments' | translate }}</label>

          <div class="flex gap-2">
            <label class="flex items-center gap-2 px-3 py-2 border border-gray-200 rounded-lg cursor-pointer hover:border-emerald-400 text-sm transition-colors">
              <i class="pi pi-image text-blue-500"></i>
              {{ 'community.add_photo' | translate }}
              <input type="file" class="hidden" accept="image/*" (change)="onFileSelected($event, 'photo')" [disabled]="uploading()" />
            </label>
            <label class="flex items-center gap-2 px-3 py-2 border border-gray-200 rounded-lg cursor-pointer hover:border-emerald-400 text-sm transition-colors">
              <i class="pi pi-file text-orange-500"></i>
              {{ 'community.add_document' | translate }}
              <input type="file" class="hidden" accept=".pdf,.doc,.docx,.xls,.xlsx" (change)="onFileSelected($event, 'document')" [disabled]="uploading()" />
            </label>
          </div>

          @if (uploading()) {
            <p class="text-sm text-gray-400"><i class="pi pi-spin pi-spinner mr-1"></i>{{ 'community.uploading' | translate }}</p>
          }

          @for (att of pendingAttachments(); track att.file_name) {
            <div class="flex items-center gap-2 bg-gray-50 rounded-lg px-3 py-2 text-sm">
              <i [class]="att.attachment_type === 'photo' ? 'pi pi-image text-blue-500' : 'pi pi-file text-orange-500'"></i>
              <span class="flex-1 truncate">{{ att.file_name }}</span>
              <button type="button" class="text-red-400 hover:text-red-600" (click)="removeAttachment(att)">
                <i class="pi pi-times"></i>
              </button>
            </div>
          }
        </div>

        @if (error()) {
          <p class="text-red-500 text-sm">{{ error() }}</p>
        }

        <div class="flex justify-end gap-2 pt-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate"
            (click)="drawerRef()?.close($event)" />
          <p-button type="submit" [label]="editPost ? ('common.save' | translate) : ('community.submit_draft' | translate)"
            [loading]="saving()" [disabled]="form.invalid || uploading()" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class PostFormDrawerComponent {
  private readonly api    = inject(CommunityApiService);
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly fb     = inject(FormBuilder);
  private readonly cdr    = inject(ChangeDetectorRef);
  private readonly translate = inject(TranslateService);

  readonly drawerRef = viewChild<Drawer>('drawerEl');
  readonly saved     = output<PostDto>();

  visible   = false;
  editPost: PostDto | null = null;

  get drawerTitle(): string {
    return this.editPost
      ? this.translate.instant('community.edit_post')
      : this.translate.instant('community.new_post');
  }

  readonly saving            = signal(false);
  readonly uploading         = signal(false);
  readonly error             = signal<string | null>(null);
  readonly pendingAttachments = signal<{ file_url: string; file_name: string; mime_type: string; file_size_bytes: number; attachment_type: 'photo' | 'document' }[]>([]);

  readonly form = this.fb.group({
    title:   ['', Validators.required],
    content: ['', Validators.required],
  });

  open(post?: PostDto): void {
    this.editPost = post ?? null;
    this.error.set(null);
    this.pendingAttachments.set([]);
    this.form.reset();
    if (post) {
      this.form.patchValue({ title: post.title, content: post.content });
    }
    this.visible = true;
    this.cdr.detectChanges();
  }

  onFileSelected(event: Event, type: 'photo' | 'document'): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.uploading.set(true);
    const folder = type === 'photo' ? 'avatars' : 'docs';
    const fd = new FormData();
    fd.append('file', file);

    this.http.post<{ file_url: string; file_name: string; file_size_bytes: number; mime_type: string }>(
      `${this.config.apiUrl}/api/v1/attachments?folder=${folder}`, fd
    ).subscribe({
      next: r => {
        this.pendingAttachments.update(list => [...list, {
          file_url: r.file_url, file_name: r.file_name,
          mime_type: r.mime_type, file_size_bytes: r.file_size_bytes,
          attachment_type: type,
        }]);
        this.uploading.set(false);
        (event.target as HTMLInputElement).value = '';
      },
      error: () => {
        this.error.set(this.translate.instant('errors.upload_failed'));
        this.uploading.set(false);
      },
    });
  }

  removeAttachment(att: { file_url: string }): void {
    this.pendingAttachments.update(list => list.filter(a => a.file_url !== att.file_url));
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.value;

    const save$ = this.editPost
      ? this.api.update(this.editPost.id, { title: v.title!, content: v.content! })
      : this.api.create({ title: v.title!, content: v.content! });

    save$.subscribe({
      next: post => {
        const attachments = this.pendingAttachments();
        if (attachments.length === 0) {
          this.saving.set(false);
          this.drawerRef()?.close(new MouseEvent('click'));
          this.saved.emit(post);
          return;
        }
        // Link pending attachments sequentially
        let remaining = attachments.length;
        for (const att of attachments) {
          this.api.addAttachment(post.id, att).subscribe({
            next: () => {
              remaining--;
              if (remaining === 0) {
                this.saving.set(false);
                this.drawerRef()?.close(new MouseEvent('click'));
                this.saved.emit(post);
              }
            },
            error: () => {
              remaining--;
              if (remaining === 0) {
                this.saving.set(false);
                this.drawerRef()?.close(new MouseEvent('click'));
                this.saved.emit(post);
              }
            },
          });
        }
      },
      error: () => {
        this.error.set(this.translate.instant('errors.save_error'));
        this.saving.set(false);
      },
    });
  }
}
