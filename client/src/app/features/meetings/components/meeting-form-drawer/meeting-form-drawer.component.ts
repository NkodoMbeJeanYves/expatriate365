import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MeetingDto, MEETING_TYPES } from '@models/meeting.model';
import { MeetingsApiService } from '../../services/meetings-api.service';

@Component({
  selector: 'app-meeting-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DrawerModule, ButtonModule, InputTextModule, TextareaModule, SelectModule, DatePickerModule, InputNumberModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [position]="'right'" [style]="{ width: '520px' }" [header]="drawerTitle()">
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 p-4">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Titre *</label>
          <input pInputText formControlName="title" placeholder="Titre de la réunion" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.type' | translate }} *</label>
          <p-select formControlName="type" [options]="meetingTypes" optionLabel="label" optionValue="value" placeholder="Sélectionner" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'meetings.scheduled_at' | translate }} *</label>
          <p-datepicker formControlName="scheduled_at" [showTime]="true" [hourFormat]="'24'" dateFormat="yy-mm-dd" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'meetings.location' | translate }}</label>
          <input pInputText formControlName="location" placeholder="Lieu de la réunion" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'meetings.agenda' | translate }}</label>
          <textarea pTextarea formControlName="agenda" rows="4" placeholder="Points à l'ordre du jour..."></textarea>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'meetings.quorum' | translate }}</label>
          <p-inputnumber formControlName="quorum_required" [min]="0" placeholder="Nombre de membres requis" />
        </div>
        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }
        <div class="flex justify-end gap-2 pt-2">
          <p-button [label]="'common.cancel' | translate" severity="secondary" (onClick)="close()" />
          <p-button [label]="editing() ? 'Enregistrer' : 'Créer'" type="submit" [loading]="saving()" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class MeetingFormDrawerComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(MeetingsApiService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly saved = output<MeetingDto>();

  private readonly drawerRef = viewChild<Drawer>('drawerEl');
  protected visible = false;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal(false);
  protected readonly drawerTitle = computed(() => this.editing() ? this.translate.instant('meetings.edit_meeting') : this.translate.instant('meetings.new'));
  private currentId: string | null = null;

  protected meetingTypes: { label: string; value: string }[] = [];

  protected form = this.fb.group({
    title: ['', Validators.required],
    type: ['general', Validators.required],
    scheduled_at: [null as Date | null, Validators.required],
    location: [''],
    agenda: [''],
    quorum_required: [null as number | null],
  });

  ngOnInit(): void {
    this.meetingTypes = [...MEETING_TYPES].map(t => ({
      label: t === 'general' ? this.translate.instant('meetings.type_general')
        : t === 'board' ? this.translate.instant('meetings.type_board')
        : this.translate.instant('meetings.type_extraordinary'),
      value: t,
    }));
  }

  open(m?: MeetingDto): void {
    this.error.set(null);
    this.editing.set(!!m);
    this.currentId = m?.id ?? null;
    if (m) {
      this.form.patchValue({
        title: m.title, type: m.type,
        scheduled_at: new Date(m.scheduled_at),
        location: m.location ?? '',
        agenda: m.agenda ?? '',
        quorum_required: m.quorum_required ?? null,
      });
    } else {
      this.form.reset({ type: 'general' });
    }
    this.visible = true;
    this.cdr.detectChanges();
  }

  close(): void { this.drawerRef()?.close(new MouseEvent('click')); }

  protected submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const dto = {
      title: v.title!,
      type: v.type!,
      scheduled_at: (v.scheduled_at as Date).toISOString(),
      location: v.location || undefined,
      agenda: v.agenda || undefined,
      quorum_required: v.quorum_required ?? undefined,
    };
    this.saving.set(true);
    const call = this.currentId ? this.api.update(this.currentId, dto) : this.api.create(dto);
    call.subscribe({
      next: m => { this.saving.set(false); this.drawerRef()?.close(new MouseEvent('click')); this.saved.emit(m); },
      error: () => { this.saving.set(false); this.error.set('Une erreur est survenue.'); },
    });
  }
}
