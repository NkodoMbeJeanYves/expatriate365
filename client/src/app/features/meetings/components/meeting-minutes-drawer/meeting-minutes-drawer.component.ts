import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { MeetingDto, MeetingMinuteDto } from '@models/meeting.model';
import { MeetingsApiService } from '../../services/meetings-api.service';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-meeting-minutes-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DrawerModule, ButtonModule, TextareaModule, TagModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [position]="'right'" [style]="{ width: '640px' }" [header]="meeting()?.title + ' — Compte-rendu'">
      <div class="p-4 flex flex-col gap-4">
        @if (minute()?.is_approved) {
          <div class="flex items-center gap-2 p-3 bg-green-50 rounded-lg border border-green-200">
            <i class="pi pi-check-circle text-green-600"></i>
            <span class="text-sm text-green-700 font-medium">
              Approuvé le {{ minute()!.approved_at | date:'d MMM y' }}
            </span>
          </div>
        }

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Contenu du compte-rendu *</label>
          <textarea pTextarea [(ngModel)]="content" rows="10" placeholder="Résumé des discussions, points abordés..."
            [disabled]="minute()?.is_approved ?? false" class="w-full"></textarea>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Décisions prises</label>
          <textarea pTextarea [(ngModel)]="decisions" rows="4" placeholder="Résolutions, votes, actions décidées..."
            [disabled]="minute()?.is_approved ?? false" class="w-full"></textarea>
        </div>

        <!-- Document attachment -->
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Document (PDF, Word)</label>
          @if (attachmentUrl()) {
            <div class="flex items-center gap-3 p-3 bg-gray-50 rounded-lg border border-gray-200">
              <i class="pi pi-file text-gray-500 text-lg"></i>
              <a [href]="attachmentUrl()!" target="_blank" rel="noopener"
                class="text-sm text-emerald-600 hover:text-emerald-700 font-medium flex-1 truncate">
                {{ attachmentName() }}
              </a>
              @if (!(minute()?.is_approved)) {
                <button type="button" (click)="removeAttachment()" class="text-red-400 hover:text-red-600">
                  <i class="pi pi-times text-sm"></i>
                </button>
              }
            </div>
          } @else if (!(minute()?.is_approved)) {
            <label class="cursor-pointer inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-dashed border-gray-300 text-sm text-gray-500 hover:bg-gray-50 transition-colors w-fit">
              @if (uploading()) {
                <i class="pi pi-spin pi-spinner text-sm"></i> Envoi en cours…
              } @else {
                <i class="pi pi-upload text-sm"></i> Choisir un document
              }
              <input type="file" class="hidden"
                accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                (change)="onFileSelected($event)" [disabled]="uploading()" />
            </label>
          }
          @if (uploadError()) {
            <p class="text-red-500 text-xs">{{ uploadError() }}</p>
          }
        </div>

        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }

        <div class="flex justify-end gap-2 pt-2">
          @if (!minute()?.is_approved) {
            <p-button [label]="'common.save' | translate" icon="pi pi-save" [loading]="saving()" (onClick)="save()" />
            @if (minute()) {
              <p-button [label]="'common.approve' | translate" severity="success" icon="pi pi-check" [loading]="approving()" (onClick)="approve()" />
            }
          }
        </div>
      </div>
    </p-drawer>
  `,
})
export class MeetingMinutesDrawerComponent {
  private readonly api = inject(MeetingsApiService);
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly drawerRef = viewChild<Drawer>('drawerEl');

  protected visible = false;
  protected readonly meeting = signal<MeetingDto | null>(null);
  protected readonly minute = signal<MeetingMinuteDto | null>(null);
  protected readonly saving = signal(false);
  protected readonly approving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly uploadError = signal<string | null>(null);
  protected readonly attachmentUrl = signal<string | null>(null);
  protected content = '';
  protected decisions = '';

  protected attachmentName(): string {
    const url = this.attachmentUrl();
    return url ? url.split('/').pop() ?? 'document' : '';
  }

  open(m: MeetingDto, minute: MeetingMinuteDto | null): void {
    this.meeting.set(m);
    this.minute.set(minute);
    this.content = minute?.content ?? '';
    this.decisions = minute?.decisions ?? '';
    this.attachmentUrl.set(minute?.attachment_url ?? null);
    this.error.set(null);
    this.uploadError.set(null);
    this.visible = true;
    this.cdr.detectChanges();
  }

  protected onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.uploadError.set(null);
    this.uploading.set(true);
    const fd = new FormData();
    fd.append('file', file);
    this.http.post<{ file_url: string }>(`${this.config.apiUrl}/api/v1/upload`, fd).subscribe({
      next: r => { this.attachmentUrl.set(r.file_url); this.uploading.set(false); },
      error: () => { this.uploadError.set('Échec de l\'envoi du document.'); this.uploading.set(false); },
    });
  }

  protected removeAttachment(): void {
    this.attachmentUrl.set(null);
  }

  protected save(): void {
    if (!this.content.trim() || !this.meeting()) return;
    this.saving.set(true);
    this.api.saveMinutes(this.meeting()!.id, {
      content: this.content,
      decisions: this.decisions || undefined,
      attachment_url: this.attachmentUrl() ?? undefined,
    }).subscribe({
      next: min => { this.minute.set(min); this.saving.set(false); },
      error: () => { this.saving.set(false); this.error.set('Erreur lors de l\'enregistrement.'); },
    });
  }

  protected approve(): void {
    if (!this.meeting()) return;
    this.approving.set(true);
    this.api.approveMinutes(this.meeting()!.id).subscribe({
      next: min => { this.minute.set(min); this.approving.set(false); },
      error: () => { this.approving.set(false); this.error.set('Erreur lors de l\'approbation.'); },
    });
  }
}
