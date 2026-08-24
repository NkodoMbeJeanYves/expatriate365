import {
  ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { DocumentDto, DOCUMENT_CATEGORIES, DOCUMENT_TYPES } from '@models/document.model';
import { DocumentsApiService } from '../../services/documents-api.service';
import { DocumentsStore } from '../../store/documents.store';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-document-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, DrawerModule, ButtonModule,
    InputTextModule, SelectModule, TextareaModule, CheckboxModule, TranslatePipe,
  ],
  template: `
    <p-drawer [visible]="visible()" [header]="drawerTitle()"
      position="right" styleClass="!w-full md:!w-[520px]"
      (visibleChange)="onClose()">

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 p-2">

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Titre</label>
          <input pInputText formControlName="title" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'common.type' | translate }}</label>
            <p-select formControlName="type" [options]="typeOptions" optionLabel="label" optionValue="value" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Catégorie</label>
            <p-select formControlName="category" [options]="categoryOptions" optionLabel="label" optionValue="value" />
          </div>
        </div>

        @if (!editItem()) {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">Fichier <span class="text-red-500">*</span></label>
            <div class="border-2 border-dashed border-gray-200 rounded-lg p-4 text-center cursor-pointer hover:border-emerald-400 transition-colors"
                 (click)="fileInput.click()"
                 [class.border-emerald-500]="selectedFile()">
              @if (selectedFile()) {
                <div class="flex items-center gap-2 justify-center text-sm text-emerald-700">
                  <i class="pi pi-file text-lg"></i>
                  <span class="font-medium">{{ selectedFile()!.name }}</span>
                  <span class="text-gray-400">({{ formatSize(selectedFile()!.size) }})</span>
                </div>
              } @else {
                <div class="text-gray-400 text-sm">
                  <i class="pi pi-upload text-2xl block mb-1"></i>
                  Cliquez ou glissez un fichier ici
                  <p class="text-xs mt-1">PDF, Word, Excel, images — max 20 Mo</p>
                </div>
              }
            </div>
            <input #fileInput type="file" class="hidden"
                   accept=".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.gif,.webp"
                   (change)="onFileSelected($event)" />
            @if (uploadError()) {
              <p class="text-red-500 text-xs">{{ uploadError() }}</p>
            }
          </div>
        }

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.description' | translate }}</label>
          <textarea pTextarea formControlName="description" rows="3" class="w-full"></textarea>
        </div>

        <div class="flex items-center gap-2">
          <p-checkbox formControlName="is_public" [binary]="true" inputId="is_public" />
          <label for="is_public" class="text-sm">{{ 'documents.is_public' | translate }}</label>
        </div>

        @if (error()) {
          <p class="text-red-500 text-sm">{{ error() }}</p>
        }

        <div class="flex justify-end gap-2 pt-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate" (click)="onClose()" />
          <p-button type="submit" [label]="'common.save' | translate" [loading]="saving()" [disabled]="form.invalid" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class DocumentFormDrawerComponent implements OnInit {
  private readonly api = inject(DocumentsApiService);
  private readonly store = inject(DocumentsStore);
  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  readonly visible = input.required<boolean>();
  readonly editItem = input<DocumentDto | null>(null);
  readonly closed = output<void>();

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly uploadError = signal<string | null>(null);

  readonly drawerTitle = computed(() =>
    this.editItem()
      ? this.translate.instant('documents.edit_document')
      : this.translate.instant('documents.new')
  );

  readonly typeOptions = DOCUMENT_TYPES.map(t => ({ label: t, value: t }));
  readonly categoryOptions = DOCUMENT_CATEGORIES.map(c => ({ label: c, value: c }));

  readonly form = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    type: ['other', Validators.required],
    category: ['general', Validators.required],
    file_url: [''],
    file_name: [''],
    mime_type: ['application/pdf'],
    file_size_bytes: [0],
    is_public: [true],
  });

  ngOnInit(): void {
    const item = this.editItem();
    if (item) {
      this.form.patchValue({
        title: item.title,
        description: item.description ?? '',
        type: item.type,
        category: item.category,
        is_public: item.is_public,
      });
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.selectedFile.set(file);
      this.uploadError.set(null);
    }
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} Mo`;
  }

  onSubmit(): void {
    const v = this.form.value;
    const item = this.editItem();

    if (!item && !this.selectedFile()) {
      this.uploadError.set('Veuillez sélectionner un fichier.');
      return;
    }
    if (this.form.invalid) return;

    this.saving.set(true);
    this.error.set(null);

    if (item) {
      this.api.update(item.id, {
        title: v.title!,
        description: v.description || undefined,
        type: v.type!,
        category: v.category!,
        is_public: v.is_public!,
      }).subscribe({
        next: doc => { this.store.upsert(doc); this.store.loadStats(); this.saving.set(false); this.closed.emit(); },
        error: () => { this.error.set('Une erreur est survenue.'); this.saving.set(false); },
      });
    } else {
      const formData = new FormData();
      formData.append('file', this.selectedFile()!);
      this.http.post<{ file_url: string; file_name: string; file_size_bytes: number; mime_type: string }>(
        `${this.config.apiUrl}/api/v1/upload`, formData
      ).subscribe({
        next: upload => {
          this.api.create({
            title: v.title!,
            description: v.description || undefined,
            type: v.type!,
            category: v.category!,
            file_url: upload.file_url,
            file_name: upload.file_name,
            file_size_bytes: upload.file_size_bytes,
            mime_type: upload.mime_type,
            is_public: v.is_public!,
          }).subscribe({
            next: doc => { this.store.upsert(doc); this.store.loadStats(); this.saving.set(false); this.closed.emit(); },
            error: () => { this.error.set('Une erreur est survenue.'); this.saving.set(false); },
          });
        },
        error: () => { this.uploadError.set('Échec de l\'envoi du fichier.'); this.saving.set(false); },
      });
    }
  }

  onClose(): void { this.closed.emit(); }
}
