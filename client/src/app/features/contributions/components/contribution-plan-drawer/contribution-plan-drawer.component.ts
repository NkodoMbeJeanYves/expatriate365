import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { ContributionType, CreateContributionTypeRequest, UpdateContributionTypeRequest } from '@models/contribution.model';
import { ContributionsApiService } from '../../services/contributions-api.service';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ToastService } from '@service/toast.service';

@Component({
  selector: 'app-contribution-plan-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DrawerModule, FormsModule, InputTextModule, InputNumberModule, SelectModule, ToggleSwitchModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [visible]="visible" (visibleChange)="visible = $event" [header]="editingPlan ? ('contributions.edit_type' | translate) : ('contributions.new_type' | translate)" position="right" styleClass="!w-full sm:!w-[480px]">
      <div class="p-4 space-y-5">
        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.name' | translate }} <span class="text-red-500">*</span></label>
          <input pInputText [(ngModel)]="form.name" placeholder="Ex: Cotisation mensuelle" class="w-full" />
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.description' | translate }}</label>
          <input pInputText [(ngModel)]="form.description" placeholder="Description optionnelle" class="w-full" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.frequency' | translate }} <span class="text-red-500">*</span></label>
            <p-select [options]="frequencies" [(ngModel)]="form.frequency" optionLabel="label" optionValue="value" styleClass="w-full" />
          </div>
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.base_amount' | translate }} <span class="text-red-500">*</span></label>
            <p-inputnumber [(ngModel)]="form.base_amount" [min]="0" styleClass="w-full" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.penalty_rate' | translate }}</label>
            <p-inputnumber [(ngModel)]="form.late_penalty_rate" [min]="0" [max]="100" styleClass="w-full" />
          </div>
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.grace_period' | translate }}</label>
            <p-inputnumber [(ngModel)]="form.grace_period_days" [min]="0" styleClass="w-full" />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.effective_from' | translate }} <span class="text-red-500">*</span></label>
            <input type="date" [(ngModel)]="form.effective_from" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          </div>
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'contributions.effective_to' | translate }} <span class="text-red-500">*</span></label>
            <input type="date" [(ngModel)]="form.effective_to" class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          </div>
        </div>

        @if (editingPlan) {
          <div class="flex items-center gap-3">
            <p-toggleswitch [(ngModel)]="form.is_active" />
            <span class="text-sm text-gray-700">{{ 'common.active' | translate }}</span>
          </div>
        }

        @if (error()) {
          <div class="text-red-600 text-sm bg-red-50 rounded-lg p-3">{{ error() }}</div>
        }
      </div>

      <div class="absolute bottom-0 left-0 right-0 p-4 bg-white border-t border-gray-200 flex gap-3">
        <button (click)="close()" class="flex-1 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50">
          {{ 'common.cancel' | translate }}
        </button>
        <button (click)="submit()" [disabled]="saving()"
          class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
          {{ saving() ? ('common.loading' | translate) : (editingPlan ? ('common.save' | translate) : ('contributions.new_type' | translate)) }}
        </button>
      </div>
    </p-drawer>
  `,
})
export class ContributionPlanDrawerComponent {
  private readonly api = inject(ContributionsApiService);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly toast = inject(ToastService);
  private readonly drawerRef = viewChild<Drawer>('drawerEl');

  saved = output<ContributionType>();

  visible = false;
  editingPlan: ContributionType | null = null;
  saving = signal(false);
  error = signal<string | null>(null);

  get frequencies() {
    return [
      { label: this.translate.instant('contributions.frequency_monthly'), value: 'monthly' },
      { label: this.translate.instant('contributions.frequency_quarterly'), value: 'quarterly' },
      { label: this.translate.instant('contributions.frequency_yearly'), value: 'annual' },
      { label: this.translate.instant('contributions.frequency_one_time'), value: 'one_time' },
    ];
  }

  form: any = this.emptyForm();

  open(plan?: ContributionType) {
    this.editingPlan = plan ?? null;
    this.form = plan ? {
      name: plan.name,
      description: plan.description ?? '',
      frequency: plan.frequency,
      base_amount: plan.base_amount,
      late_penalty_rate: plan.late_penalty_rate,
      grace_period_days: plan.grace_period_days,
      effective_from: plan.effective_from,
      effective_to: plan.effective_to ?? '',
      is_active: plan.is_active,
    } : this.emptyForm();
    this.error.set(null);
    this.visible = true;
    this.cdr.detectChanges();
  }

  close() {
    this.drawerRef()?.close(new MouseEvent('click'));
  }

  private emptyForm() {
    return { name: '', description: '', frequency: 'monthly', base_amount: 0, late_penalty_rate: 0, grace_period_days: 0, effective_from: '', effective_to: '', is_active: true };
  }

  submit() {
    if (!this.form.name || !this.form.frequency || !this.form.effective_from || !this.form.effective_to) {
      this.error.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);

    const obs = this.editingPlan
      ? this.api.updateType(this.editingPlan.id, this.form as UpdateContributionTypeRequest)
      : this.api.createType(this.form as CreateContributionTypeRequest);

    obs.subscribe({
      next: (type) => {
        this.saving.set(false);
        if (this.editingPlan) {
          this.toast.success('Plan mis à jour.');
        } else {
          this.toast.success('Plan créé avec succès.');
        }
        this.drawerRef()?.close(new MouseEvent('click'));
        this.saved.emit(type);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error('Erreur lors de l\'enregistrement.');
        this.error.set(err?.error?.error ?? 'Une erreur est survenue.');
      },
    });
  }
}
