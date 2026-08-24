import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { EventDto, EVENT_TYPES } from '@models/event.model';
import { EventsApiService } from '../../services/events-api.service';
import { TranslateService, TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-event-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, DrawerModule, ButtonModule,
    InputTextModule, TextareaModule, SelectModule, CheckboxModule,
    DatePickerModule, InputNumberModule, TranslatePipe,
  ],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [position]="'right'" [style]="{ width: '520px' }" [header]="drawerTitle()">
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 p-4">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Titre *</label>
          <input pInputText formControlName="title" placeholder="Titre de l'événement" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.type' | translate }} *</label>
          <p-select formControlName="type" [options]="eventTypes" optionLabel="label" optionValue="value" placeholder="Sélectionner" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.description' | translate }}</label>
          <textarea pTextarea formControlName="description" rows="3" placeholder="Description..."></textarea>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'events.location' | translate }}</label>
          <input pInputText formControlName="location" placeholder="Lieu de l'événement" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'events.start_date' | translate }} *</label>
            <p-datepicker formControlName="start_date" [showTime]="true" [hourFormat]="'24'" dateFormat="yy-mm-dd" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'events.end_date' | translate }} *</label>
            <p-datepicker formControlName="end_date" [showTime]="true" [hourFormat]="'24'" dateFormat="yy-mm-dd" />
          </div>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'events.max_capacity' | translate }}</label>
          <p-inputnumber formControlName="max_capacity" [min]="0" placeholder="Illimité si vide" />
        </div>
        <div class="flex items-center gap-2">
          <p-checkbox formControlName="is_public" [binary]="true" inputId="is_public" />
          <label for="is_public" class="text-sm">{{ 'events.is_public' | translate }}</label>
        </div>
        @if (error()) {
          <p class="text-red-500 text-sm">{{ error() }}</p>
        }
        <div class="flex justify-end gap-2 pt-2">
          <p-button [label]="'common.cancel' | translate" severity="secondary" (onClick)="close()" />
          <p-button [label]="editing() ? 'Enregistrer' : 'Créer'" type="submit" [loading]="saving()" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class EventFormDrawerComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(EventsApiService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly saved = output<EventDto>();

  private readonly drawerRef = viewChild<Drawer>('drawerEl');
  protected visible = false;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal(false);
  protected readonly drawerTitle = computed(() => this.editing() ? this.translate.instant('events.edit_event') : this.translate.instant('events.add_event'));
  private currentId: string | null = null;

  protected readonly eventTypes = [...EVENT_TYPES].map(t => ({ label: t.charAt(0).toUpperCase() + t.slice(1), value: t }));

  protected form = this.fb.group({
    title: ['', Validators.required],
    type: ['general', Validators.required],
    description: [''],
    location: [''],
    start_date: [null as Date | null, Validators.required],
    end_date: [null as Date | null, Validators.required],
    max_capacity: [null as number | null],
    is_public: [true],
  });

  ngOnInit(): void {}

  open(ev?: EventDto): void {
    this.error.set(null);
    this.editing.set(!!ev);
    this.currentId = ev?.id ?? null;
    if (ev) {
      this.form.patchValue({
        title: ev.title,
        type: ev.type,
        description: ev.description ?? '',
        location: ev.location ?? '',
        start_date: new Date(ev.start_date),
        end_date: new Date(ev.end_date),
        max_capacity: ev.max_capacity ?? null,
        is_public: ev.is_public,
      });
    } else {
      this.form.reset({ type: 'general', is_public: true });
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
      description: v.description ?? undefined,
      location: v.location ?? undefined,
      start_date: (v.start_date as Date).toISOString(),
      end_date: (v.end_date as Date).toISOString(),
      max_capacity: v.max_capacity ?? undefined,
      is_public: v.is_public ?? true,
    };

    this.saving.set(true);
    const call = this.currentId
      ? this.api.update(this.currentId, dto)
      : this.api.create(dto);

    call.subscribe({
      next: (ev) => { this.saving.set(false); this.drawerRef()?.close(new MouseEvent('click')); this.saved.emit(ev); },
      error: () => { this.saving.set(false); this.error.set('Une erreur est survenue.'); },
    });
  }
}
