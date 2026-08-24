import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { WelfareApiService } from '../../services/welfare-api.service';
import { WelfareRequest, WELFARE_TYPES } from '@models/welfare.model';
import { MembersApiService } from '@members/services/members-api.service';
import { MemberListItem } from '@core/models/member.model';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-welfare-request-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DrawerModule, FormsModule, InputNumberModule, SelectModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [header]="'welfare.new_request' | translate" position="right" styleClass="!w-full sm:!w-[480px]">
      <div class="p-4 space-y-5">

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.member' | translate }} <span class="text-red-500">*</span></label>
          <p-select [options]="memberOptions()" [(ngModel)]="form.member_id"
            optionLabel="label" optionValue="value" [filter]="true" filterBy="label"
            [placeholder]="'common.none' | translate" styleClass="w-full" />
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.type' | translate }} <span class="text-red-500">*</span></label>
          <p-select [options]="welfareTypes" [(ngModel)]="form.type"
            optionLabel="label" optionValue="value" styleClass="w-full" />
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.description' | translate }} <span class="text-red-500">*</span></label>
          <textarea [(ngModel)]="form.description" rows="4" placeholder="Décrivez la situation…"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 resize-none"></textarea>
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'welfare.amount_requested' | translate }} <span class="text-red-500">*</span></label>
          <p-inputnumber [(ngModel)]="form.amount_requested" [min]="1" styleClass="w-full" />
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.notes' | translate }}</label>
          <input [(ngModel)]="form.notes" placeholder="Optionnel…"
            class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
        </div>

        @if (error()) {
          <div class="text-red-600 text-sm bg-red-50 rounded-lg p-3">{{ error() }}</div>
        }
      </div>

      <div class="absolute bottom-0 left-0 right-0 p-4 bg-white border-t border-gray-200 flex gap-3">
        <button (click)="drawerRef()?.close($event)" class="flex-1 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50">{{ 'common.cancel' | translate }}</button>
        <button (click)="submit()" [disabled]="saving()"
          class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
          {{ saving() ? ('common.loading' | translate) : ('welfare.new_request' | translate) }}
        </button>
      </div>
    </p-drawer>
  `,
})
export class WelfareRequestDrawerComponent {
  private readonly api = inject(WelfareApiService);
  private readonly membersApi = inject(MembersApiService);
  protected readonly drawerRef = viewChild<Drawer>('drawerEl');
  private readonly cdr = inject(ChangeDetectorRef);

  saved = output<WelfareRequest>();

  visible = false;
  saving = signal(false);
  error = signal<string | null>(null);
  members = signal<MemberListItem[]>([]);
  welfareTypes = [...WELFARE_TYPES];

  form = { member_id: '', type: 'other', description: '', amount_requested: 0, notes: '' };

  memberOptions() {
    return this.members().map(m => ({
      label: `${m.first_name} ${m.last_name} (${m.membership_number})`,
      value: m.id,
    }));
  }

  open() {
    this.form = { member_id: '', type: 'other', description: '', amount_requested: 0, notes: '' };
    this.error.set(null);
    this.visible = true;
    this.cdr.detectChanges();
    this.membersApi.list({ page: 1, limit: 200, status: 'active' }).subscribe({
      next: res => this.members.set(res.data),
    });
  }

  submit() {
    if (!this.form.member_id || !this.form.description || !this.form.amount_requested) {
      this.error.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api.create(this.form).subscribe({
      next: (req) => { this.saving.set(false); this.drawerRef()?.close(new MouseEvent('click')); this.saved.emit(req); },
      error: (err) => { this.saving.set(false); this.error.set(err?.error?.error ?? 'Une erreur est survenue.'); },
    });
  }
}
