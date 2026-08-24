import {
  ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal, viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CommunicationDto, COMMUNICATION_AUDIENCES, COMMUNICATION_CHANNELS, COMMUNICATION_TYPES } from '@models/communication.model';
import { CommunicationsApiService } from '../../services/communications-api.service';
import { CommunicationsStore } from '../../store/communications.store';

@Component({
  selector: 'app-communication-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, DrawerModule, ButtonModule,
    InputTextModule, SelectModule, TextareaModule, TranslatePipe,
  ],
  template: `
    <p-drawer #drawerEl [visible]="visible()" [header]="drawerTitle()"
      position="right" styleClass="!w-full md:!w-[520px]"
      (visibleChange)="onVisibleChange($event)">

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4 p-2">

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'communications.title' | translate }}</label>
          <input pInputText formControlName="title" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'communications.type' | translate }}</label>
            <p-select formControlName="type" [options]="typeOptions" optionLabel="label" optionValue="value" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'communications.channel' | translate }}</label>
            <p-select formControlName="channel" [options]="channelOptions" optionLabel="label" optionValue="value" />
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'communications.audience' | translate }}</label>
          <p-select formControlName="audience" [options]="audienceOptions" optionLabel="label" optionValue="value"
            (onChange)="onAudienceChange()" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'communications.content' | translate }}</label>
          <textarea pTextarea formControlName="content" rows="6" class="w-full"></textarea>
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
export class CommunicationFormDrawerComponent implements OnInit {
  private readonly api = inject(CommunicationsApiService);
  private readonly store = inject(CommunicationsStore);
  private readonly fb = inject(FormBuilder);
  private readonly t = inject(TranslateService);
  private readonly drawerRef = viewChild<Drawer>('drawerEl');

  readonly visible = input.required<boolean>();
  readonly editItem = input<CommunicationDto | null>(null);
  readonly closed = output<void>();

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly drawerTitle = computed(() =>
    this.editItem() ? this.t.instant('communications.edit_comm') : this.t.instant('communications.new'));

  get typeOptions() {
    return COMMUNICATION_TYPES.map(t => ({ label: this.t.instant(`communications.type_${t}`), value: t }));
  }
  get channelOptions() {
    return COMMUNICATION_CHANNELS.map(c => ({ label: this.t.instant(`communications.channel_${c}`), value: c }));
  }
  get audienceOptions() {
    return COMMUNICATION_AUDIENCES.map(a => ({ label: this.t.instant(`communications.audience_${a}`), value: a }));
  }

  readonly form = this.fb.group({
    title: ['', Validators.required],
    content: ['', Validators.required],
    type: ['announcement', Validators.required],
    channel: ['app', Validators.required],
    audience: ['all', Validators.required],
    category_id: [''],
    target_member_id: [''],
  });

  ngOnInit(): void {
    const item = this.editItem();
    if (item) {
      this.form.patchValue({
        title: item.title,
        content: item.content,
        type: item.type,
        channel: item.channel,
        audience: item.audience,
        category_id: item.category_id ?? '',
        target_member_id: item.target_member_id ?? '',
      });
    }
  }

  onAudienceChange(): void {
    this.form.patchValue({ category_id: '', target_member_id: '' });
  }

  onVisibleChange(v: boolean): void {
    if (!v) this.closed.emit();
  }

  onClose(): void {
    this.drawerRef()?.close(new MouseEvent('click'));
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);

    const v = this.form.value;
    const payload = {
      title: v.title!,
      content: v.content!,
      type: v.type!,
      channel: v.channel!,
      audience: v.audience!,
      category_id: v.category_id || undefined,
      target_member_id: v.target_member_id || undefined,
    };

    const item = this.editItem();
    const req$ = item
      ? this.api.update(item.id, payload)
      : this.api.create(payload);

    req$.subscribe({
      next: c => {
        this.store.upsert(c);
        this.store.loadStats();
        this.saving.set(false);
        this.drawerRef()?.close(new MouseEvent('click'));
      },
      error: () => {
        this.error.set('Une erreur est survenue.');
        this.saving.set(false);
      },
    });
  }
}
