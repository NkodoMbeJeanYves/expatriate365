import {
  ChangeDetectionStrategy, Component, inject, input, signal, output,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ActionItemDto, ACTION_ITEM_STATUSES } from '@models/meeting.model';
import { MeetingsApiService } from '../../services/meetings-api.service';

@Component({
  selector: 'app-meeting-action-items-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, ReactiveFormsModule, DrawerModule, ButtonModule,
    InputTextModule, SelectModule, TagModule, TooltipModule,
  ],
  template: `
    <p-drawer
      [(visible)]="visible"
      position="right"
      styleClass="!w-full sm:!w-[600px]"
      header="Actions de la réunion"
      (onShow)="load()">

      <div class="flex flex-col gap-4 p-2">

        <!-- Add form -->
        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-3 bg-gray-50 rounded-xl p-4 border border-gray-200">
          <h3 class="text-sm font-semibold text-gray-700">Nouvelle action</h3>
          <div class="flex flex-col gap-1">
            <label class="text-xs font-medium text-gray-600">Titre *</label>
            <input pInputText formControlName="title" placeholder="Décrire l'action..." class="w-full" />
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div class="flex flex-col gap-1">
              <label class="text-xs font-medium text-gray-600">Responsable</label>
              <input pInputText formControlName="assigned_to_member_id" placeholder="ID du membre" class="w-full" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-xs font-medium text-gray-600">Échéance</label>
              <input pInputText type="date" formControlName="due_date" class="w-full" />
            </div>
          </div>
          @if (formError()) { <p class="text-red-500 text-xs">{{ formError() }}</p> }
          <div class="flex justify-end">
            <p-button type="submit" label="Ajouter" icon="pi pi-plus" size="small" [loading]="saving()" [disabled]="form.invalid" />
          </div>
        </form>

        <!-- List -->
        @if (loading()) {
          <div class="flex justify-center py-8">
            <i class="pi pi-spin pi-spinner text-2xl text-gray-400"></i>
          </div>
        } @else if (items().length === 0) {
          <div class="text-center py-10 text-gray-400">
            <i class="pi pi-check-square text-3xl block mb-2"></i>
            <p class="text-sm">Aucune action définie pour cette réunion.</p>
          </div>
        } @else {
          <div class="flex flex-col gap-2">
            @for (item of items(); track item.id) {
              <div class="bg-white border border-gray-200 rounded-xl p-3 flex items-start gap-3">
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-800 truncate">{{ item.title }}</p>
                  @if (item.assigned_to_name) {
                    <p class="text-xs text-gray-500 mt-0.5"><i class="pi pi-user mr-1"></i>{{ item.assigned_to_name }}</p>
                  }
                  @if (item.due_date) {
                    <p class="text-xs text-gray-400"><i class="pi pi-calendar mr-1"></i>{{ item.due_date }}</p>
                  }
                </div>
                <div class="flex items-center gap-1 shrink-0">
                  <p-tag [value]="item.status" [severity]="statusSeverity(item.status)" styleClass="text-xs" />
                  @if (item.status !== 'done' && item.status !== 'cancelled') {
                    <p-button icon="pi pi-check" severity="success" [text]="true" size="small"
                      pTooltip="Marquer comme fait"
                      [loading]="toggling() === item.id"
                      (click)="markDone(item)" />
                  }
                  <p-button icon="pi pi-trash" severity="danger" [text]="true" size="small"
                    pTooltip="Supprimer"
                    [loading]="deleting() === item.id"
                    (click)="remove(item)" />
                </div>
              </div>
            }
          </div>
        }
      </div>
    </p-drawer>
  `,
})
export class MeetingActionItemsDrawerComponent {
  readonly meetingId = input.required<string>();
  readonly changed   = output<void>();

  visible = false;

  private readonly api = inject(MeetingsApiService);
  private readonly fb  = inject(FormBuilder);

  readonly items   = signal<ActionItemDto[]>([]);
  readonly loading = signal(false);
  readonly saving  = signal(false);
  readonly toggling = signal<string | null>(null);
  readonly deleting = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  readonly form = this.fb.group({
    title: ['', Validators.required],
    assigned_to_member_id: [''],
    due_date: [''],
  });

  load(): void {
    this.loading.set(true);
    this.api.listActionItems(this.meetingId()).subscribe({
      next: items => { this.items.set(items); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set(null);
    const v = this.form.getRawValue();
    this.api.createActionItem(this.meetingId(), {
      title: v.title!,
      assigned_to_member_id: v.assigned_to_member_id || undefined,
      due_date: v.due_date || undefined,
    }).subscribe({
      next: item => {
        this.items.update(list => [...list, item]);
        this.form.reset();
        this.saving.set(false);
        this.changed.emit();
      },
      error: err => {
        this.formError.set(err?.error?.error ?? 'Erreur lors de la création.');
        this.saving.set(false);
      },
    });
  }

  markDone(item: ActionItemDto): void {
    this.toggling.set(item.id);
    this.api.updateActionItem(item.id, {
      title: item.title,
      description: item.description,
      assigned_to_member_id: item.assigned_to_member_id,
      due_date: item.due_date,
      status: 'done',
    }).subscribe({
      next: updated => {
        this.items.update(list => list.map(i => i.id === updated.id ? updated : i));
        this.toggling.set(null);
        this.changed.emit();
      },
      error: () => this.toggling.set(null),
    });
  }

  remove(item: ActionItemDto): void {
    this.deleting.set(item.id);
    this.api.deleteActionItem(item.id).subscribe({
      next: () => {
        this.items.update(list => list.filter(i => i.id !== item.id));
        this.deleting.set(null);
        this.changed.emit();
      },
      error: () => this.deleting.set(null),
    });
  }

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
      open: 'info',
      in_progress: 'warn',
      done: 'success',
      cancelled: 'secondary',
    };
    return map[status] ?? 'secondary';
  }

  open(): void { this.visible = true; }
}
