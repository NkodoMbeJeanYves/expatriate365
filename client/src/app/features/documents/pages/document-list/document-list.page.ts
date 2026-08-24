import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { DocumentDto, DOCUMENT_CATEGORIES, DOCUMENT_TYPES } from '@models/document.model';
import { DocumentsStore } from '../../store/documents.store';
import { DocumentsApiService } from '../../services/documents-api.service';
import { DocumentFormDrawerComponent } from '../../components/document-form-drawer/document-form-drawer.component';

@Component({
  selector: 'app-document-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    FormsModule, ButtonModule, TagModule, SelectModule, InputTextModule,
    ProgressSpinnerModule, TooltipModule, ConfirmDialog, TranslatePipe,
    DocumentFormDrawerComponent,
  ],
  template: `
    <p-confirmdialog />

    <div class="p-6 flex flex-col gap-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'documents.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'documents.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'documents.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-3 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total }}</div>
            <div class="text-xs text-gray-500 mt-1">Total</div>
          </div>
          <div class="bg-blue-50 rounded-xl p-4 border border-blue-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-blue-700">{{ s.public }}</div>
            <div class="text-xs text-blue-600 mt-1">Publics</div>
          </div>
          <div class="bg-orange-50 rounded-xl p-4 border border-orange-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-orange-700">{{ s.private }}</div>
            <div class="text-xs text-orange-600 mt-1">Privés</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <input pInputText [(ngModel)]="searchTerm" [placeholder]="'common.search' | translate" class="w-48"
          (input)="applyFilters()" />
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          placeholder="Tous les types" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="categoryOptions" [(ngModel)]="filterCategory" optionLabel="label" optionValue="value"
          placeholder="Toutes les catégories" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Liste -->
      @if (store.loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.documents().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-file text-4xl mb-3 block"></i>
          <p>{{ 'documents.no_documents' | translate }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (doc of store.documents(); track doc.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex flex-col gap-3">
              <div class="flex items-start gap-3">
                <div class="p-2 rounded-lg bg-blue-50 text-blue-600 shrink-0">
                  <i [class]="fileIcon(doc.mime_type) + ' text-xl'"></i>
                </div>
                <div class="min-w-0 flex-1">
                  <h3 class="font-semibold text-gray-800 text-sm truncate">{{ doc.title }}</h3>
                  <p class="text-xs text-gray-400">{{ doc.uploader_name }}</p>
                </div>
              </div>

              @if (doc.description) {
                <p class="text-xs text-gray-500 line-clamp-2">{{ doc.description }}</p>
              }

              <div class="flex gap-2 flex-wrap">
                <p-tag [value]="doc.type" severity="info" />
                <p-tag [value]="doc.category" severity="secondary" />
                @if (!doc.is_public) {
                  <p-tag value="Privé" severity="warn" />
                }
              </div>

              <div class="text-xs text-gray-400">{{ formatSize(doc.file_size_bytes) }}</div>

              <div class="flex items-center gap-2 pt-1 border-t border-gray-50">
                <a [href]="doc.file_url" target="_blank" rel="noopener">
                  <p-button size="small" icon="pi pi-download" [pTooltip]="'documents.download' | translate" severity="secondary" />
                </a>
                <p-button size="small" icon="pi pi-pencil" [pTooltip]="'common.edit' | translate" severity="secondary"
                  (onClick)="openForm(doc)" />
                <p-button size="small" icon="pi pi-trash" [pTooltip]="'common.delete' | translate" severity="danger"
                  (onClick)="confirmDelete(doc)" />
              </div>
            </div>
          }
        </div>
      }
    </div>

    <app-document-form-drawer
      [visible]="showForm()"
      [editItem]="editItem()"
      (closed)="closeForm()" />
  `,
})
export class DocumentListPage implements OnInit {
  protected readonly store = inject(DocumentsStore);
  private readonly api = inject(DocumentsApiService);
  private readonly confirm = inject(ConfirmationService);

  readonly showForm = signal(false);
  readonly editItem = signal<DocumentDto | null>(null);

  searchTerm = '';
  filterType: string | null = null;
  filterCategory: string | null = null;

  readonly typeOptions = DOCUMENT_TYPES.map(t => ({ label: t, value: t }));
  readonly categoryOptions = DOCUMENT_CATEGORIES.map(c => ({ label: c, value: c }));

  ngOnInit(): void {
    this.store.load();
    this.store.loadStats();
  }

  applyFilters(): void {
    this.store.load({
      page: 1,
      type: this.filterType ?? undefined,
      category: this.filterCategory ?? undefined,
      search: this.searchTerm || undefined,
    });
  }

  openForm(item?: DocumentDto): void {
    this.editItem.set(item ?? null);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editItem.set(null);
  }

  confirmDelete(doc: DocumentDto): void {
    this.confirm.confirm({
      message: `Supprimer "${doc.title}" ?`,
      header: 'Confirmation',
      icon: 'pi pi-trash',
      accept: () => {
        this.api.delete(doc.id).subscribe(() => {
          this.store.remove(doc.id);
          this.store.loadStats();
        });
      },
    });
  }

  fileIcon(mimeType: string): string {
    if (mimeType.includes('pdf')) return 'pi pi-file-pdf';
    if (mimeType.includes('image')) return 'pi pi-image';
    if (mimeType.includes('word') || mimeType.includes('document')) return 'pi pi-file-word';
    if (mimeType.includes('sheet') || mimeType.includes('excel')) return 'pi pi-file-excel';
    return 'pi pi-file';
  }

  formatSize(bytes: number): string {
    if (bytes === 0) return '–';
    if (bytes < 1024) return `${bytes} o`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} Ko`;
    return `${(bytes / 1024 / 1024).toFixed(1)} Mo`;
  }
}
