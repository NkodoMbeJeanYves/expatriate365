import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, computed, inject, output, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { ElectionDto, ELECTION_TYPES } from '@models/election.model';
import { ElectionsApiService } from '../../services/elections-api.service';

@Component({
  selector: 'app-election-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DrawerModule, ButtonModule, InputTextModule, TextareaModule, SelectModule, DatePickerModule, InputNumberModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [position]="'right'" [style]="{ width: '520px' }" [header]="drawerTitle()">
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 p-4">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.title' | translate }} *</label>
          <input pInputText formControlName="title" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.type' | translate }} *</label>
          <p-select formControlName="type" [options]="typeOptions" optionLabel="label" optionValue="value" placeholder="Sélectionner" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.description' | translate }}</label>
          <textarea pTextarea formControlName="description" rows="3" placeholder="Description de l'élection..."></textarea>
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'elections.start_date' | translate }}</label>
            <p-datepicker formControlName="start_date" [showTime]="true" [hourFormat]="'24'" dateFormat="yy-mm-dd" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'elections.end_date' | translate }}</label>
            <p-datepicker formControlName="end_date" [showTime]="true" [hourFormat]="'24'" dateFormat="yy-mm-dd" />
          </div>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'elections.max_choices' | translate }} *</label>
          <p-inputnumber formControlName="max_choices" [min]="1" [max]="20" />
          <span class="text-xs text-gray-400">{{ 'elections.max_choices_hint' | translate }}</span>
        </div>
        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }
        <div class="flex justify-end gap-2 pt-2">
          <p-button [label]="'common.cancel' | translate" severity="secondary" (onClick)="close()" />
          <p-button [label]="(editing() ? 'common.save' : 'common.create') | translate" type="submit" [loading]="saving()" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class ElectionFormDrawerComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ElectionsApiService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly saved = output<ElectionDto>();

  private readonly drawerRef = viewChild<Drawer>('drawerEl');
  protected visible = false;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly editing = signal(false);
  protected readonly drawerTitle = computed(() => this.editing() ? "Modifier l'élection" : 'Nouvelle élection');
  private currentId: string | null = null;

  protected typeOptions: { label: string; value: string }[] = [];

  protected form = this.fb.group({
    title: ['', Validators.required],
    type: ['custom', Validators.required],
    description: [''],
    start_date: [null as Date | null],
    end_date: [null as Date | null],
    max_choices: [1, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.typeOptions = [...ELECTION_TYPES].map(t => ({
      label: t === 'board' ? this.translate.instant('elections.type_board') : t === 'custom' ? this.translate.instant('elections.type_custom') : t,
      value: t,
    }));
  }

  open(e?: ElectionDto): void {
    this.error.set(null);
    this.editing.set(!!e);
    this.currentId = e?.id ?? null;
    if (e) {
      this.form.patchValue({
        title: e.title, type: e.type, description: e.description ?? '',
        start_date: e.start_date ? new Date(e.start_date) : null,
        end_date: e.end_date ? new Date(e.end_date) : null,
        max_choices: e.max_choices,
      });
    } else {
      this.form.reset({ type: 'custom', max_choices: 1 });
    }
    this.visible = true;
    this.cdr.detectChanges();
  }

  close(): void { this.drawerRef()?.close(new MouseEvent('click')); }

  protected submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const dto = {
      title: v.title!, type: v.type!,
      description: v.description || undefined,
      start_date: v.start_date ? (v.start_date as Date).toISOString() : undefined,
      end_date: v.end_date ? (v.end_date as Date).toISOString() : undefined,
      max_choices: v.max_choices ?? 1,
    };
    this.saving.set(true);
    const call = this.currentId ? this.api.update(this.currentId, dto) : this.api.create(dto);
    call.subscribe({
      next: e => { this.saving.set(false); this.drawerRef()?.close(new MouseEvent('click')); this.saved.emit(e); },
      error: () => { this.saving.set(false); this.error.set('Une erreur est survenue.'); },
    });
  }
}
