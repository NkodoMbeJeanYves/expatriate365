import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import {
  CommunicationDto, COMMUNICATION_CHANNELS,
  COMMUNICATION_TYPES, RecipientDto,
} from '@models/communication.model';
import { CommunicationsStore } from '../../store/communications.store';
import { CommunicationsApiService } from '../../services/communications-api.service';
import { CommunicationFormDrawerComponent } from '../../components/communication-form-drawer/communication-form-drawer.component';
import { CommunicationRecipientsDrawerComponent } from '../../components/communication-recipients-drawer/communication-recipients-drawer.component';

@Component({
  selector: 'app-communication-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, ButtonModule, TagModule, SelectModule,
    ProgressSpinnerModule, TooltipModule, TranslatePipe,
    CommunicationFormDrawerComponent, CommunicationRecipientsDrawerComponent,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'communications.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'communications.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'communications.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total }}</div>
            <div class="text-xs text-gray-500 mt-1">Total</div>
          </div>
          <div class="bg-gray-50 rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-600">{{ s.draft }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'communications.status_draft' | translate }}</div>
          </div>
          <div class="bg-blue-50 rounded-xl p-4 border border-blue-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-blue-700">{{ s.sent }}</div>
            <div class="text-xs text-blue-600 mt-1">{{ 'communications.status_sent' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ s.total_read }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'communications.read' | translate }} / {{ s.total_recipients }} {{ 'communications.recipients' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          placeholder="Tous les statuts" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          placeholder="Tous les types" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="channelOptions" [(ngModel)]="filterChannel" optionLabel="label" optionValue="value"
          placeholder="Tous les canaux" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Liste -->
      @if (store.loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.communications().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-envelope text-4xl mb-3 block"></i>
          <p>{{ 'communications.no_communications' | translate }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          @for (comm of store.communications(); track comm.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex flex-col gap-3">
              <div class="flex items-start justify-between gap-2">
                <h3 class="font-semibold text-gray-800 text-sm leading-snug">{{ comm.title }}</h3>
                <p-tag [value]="'communications.status_' + comm.status | translate" [severity]="statusSeverity(comm.status)" class="shrink-0" />
              </div>

              <p class="text-xs text-gray-500 line-clamp-2">{{ comm.content }}</p>

              <div class="flex gap-2 flex-wrap">
                <p-tag [value]="comm.type" severity="info" />
                <p-tag [value]="comm.channel" severity="secondary" />
                <p-tag [value]="comm.audience" severity="secondary" />
              </div>

              @if (comm.status === 'sent') {
                <div class="text-xs text-gray-500">
                  {{ comm.read_count }} / {{ comm.recipient_count }} {{ 'communications.read' | translate }}
                </div>
              }

              <div class="flex items-center gap-2 pt-1 border-t border-gray-50">
                @if (comm.status === 'draft') {
                  <p-button size="small" icon="pi pi-pencil" [pTooltip]="'common.edit' | translate" severity="secondary"
                    (onClick)="openForm(comm)" />
                  <p-button size="small" icon="pi pi-send" [pTooltip]="'communications.send_now' | translate" severity="info"
                    [loading]="sending() === comm.id" (onClick)="send(comm)" />
                }
                @if (comm.status === 'sent') {
                  <p-button size="small" icon="pi pi-users" [pTooltip]="'communications.recipients' | translate" severity="secondary"
                    (onClick)="viewRecipients(comm)" />
                }
              </div>
            </div>
          }
        </div>
      }
    </div>

    <app-communication-form-drawer
      [visible]="showForm()"
      [editItem]="editItem()"
      (closed)="closeForm()" />

    <app-communication-recipients-drawer
      [visible]="showRecipients()"
      [recipients]="currentRecipients()"
      (closed)="showRecipients.set(false)" />
  `,
})
export class CommunicationListPage implements OnInit {
  protected readonly store = inject(CommunicationsStore);
  private readonly api = inject(CommunicationsApiService);
  private readonly translate = inject(TranslateService);

  readonly showForm = signal(false);
  readonly editItem = signal<CommunicationDto | null>(null);
  readonly showRecipients = signal(false);
  readonly currentRecipients = signal<RecipientDto[]>([]);
  readonly sending = signal<string | null>(null);

  filterStatus: string | null = null;
  filterType: string | null = null;
  filterChannel: string | null = null;

  readonly statusOptions = ['draft', 'sent'].map(s => ({ label: this.translate.instant('communications.status_' + s), value: s }));
  readonly typeOptions = COMMUNICATION_TYPES.map(t => ({ label: t, value: t }));
  readonly channelOptions = COMMUNICATION_CHANNELS.map(c => ({ label: c, value: c }));

  ngOnInit(): void {
    this.store.load();
    this.store.loadStats();
  }

  applyFilters(): void {
    this.store.load({
      page: 1,
      status: this.filterStatus ?? undefined,
      type: this.filterType ?? undefined,
      channel: this.filterChannel ?? undefined,
    });
  }

  openForm(item?: CommunicationDto): void {
    this.editItem.set(item ?? null);
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
    this.editItem.set(null);
  }

  send(comm: CommunicationDto): void {
    this.sending.set(comm.id);
    this.api.send(comm.id).subscribe({
      next: updated => {
        this.store.upsert(updated);
        this.store.loadStats();
        this.sending.set(null);
      },
      error: () => this.sending.set(null),
    });
  }

  viewRecipients(comm: CommunicationDto): void {
    this.api.getById(comm.id).subscribe(res => {
      this.currentRecipients.set(res.recipients);
      this.showRecipients.set(true);
    });
  }

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    return status === 'sent' ? 'success' : 'secondary';
  }
}
