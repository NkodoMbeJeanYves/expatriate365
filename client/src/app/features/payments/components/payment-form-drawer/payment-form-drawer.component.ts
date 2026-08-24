import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { Payment, PAYMENT_METHODS } from '@models/payment.model';
import { PaymentsApiService } from '../../services/payments-api.service';
import { ContributionsApiService } from '@contributions/services/contributions-api.service';
import { ContributionCharge } from '@models/contribution.model';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthStore } from '@core/auth/auth.store';
import { ToastService } from '@service/toast.service';

@Component({
  selector: 'app-payment-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DrawerModule, FormsModule, InputNumberModule, SelectModule, InputTextModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [header]="'payments.new' | translate" position="right" styleClass="!w-full sm:!w-[480px]">
      <div class="p-4 space-y-5">

        <!-- Member display (auto-filled when connected as member) -->
        @if (memberName()) {
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'common.member' | translate }}</label>
            <input pInputText [value]="memberName()" [disabled]="true"
              class="w-full bg-gray-50 text-gray-500 cursor-not-allowed" />
          </div>
        }

        <!-- Charge selector -->
        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'contributions.title' | translate }} <span class="text-red-500">*</span></label>
          <p-select
            [options]="chargeOptions()"
            [(ngModel)]="form.charge_id"
            optionLabel="label"
            optionValue="value"
            [placeholder]="'common.none' | translate"
            [filter]="true"
            filterBy="label"
            styleClass="w-full" />
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'common.amount' | translate }} <span class="text-red-500">*</span></label>
            <p-inputnumber [(ngModel)]="form.amount" [min]="1" styleClass="w-full" />
          </div>
          <div class="space-y-2">
            <label class="block text-sm font-medium text-gray-700">{{ 'common.date' | translate }} <span class="text-red-500">*</span></label>
            <input type="date" [(ngModel)]="form.payment_date"
              class="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          </div>
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'payments.method' | translate }} <span class="text-red-500">*</span></label>
          <p-select [options]="paymentMethods" [(ngModel)]="form.payment_method" optionLabel="label" optionValue="value" styleClass="w-full" />
        </div>

        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700">{{ 'common.notes' | translate }}</label>
          <input pInputText [(ngModel)]="form.notes" placeholder="Référence, commentaire…" class="w-full" />
        </div>

        @if (error()) {
          <div class="text-red-600 text-sm bg-red-50 rounded-lg p-3">{{ error() }}</div>
        }
      </div>

      <div class="absolute bottom-0 left-0 right-0 p-4 bg-white border-t border-gray-200 flex gap-3">
        <button (click)="drawerRef()?.close($event)" class="flex-1 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50">
          {{ 'common.cancel' | translate }}
        </button>
        <button (click)="submit()" [disabled]="saving()"
          class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
          {{ saving() ? ('common.loading' | translate) : ('common.save' | translate) }}
        </button>
      </div>
    </p-drawer>
  `,
})
export class PaymentFormDrawerComponent {
  private readonly api = inject(PaymentsApiService);
  private readonly contributionsApi = inject(ContributionsApiService);
  private readonly authStore = inject(AuthStore);
  private readonly toast = inject(ToastService);
  protected readonly drawerRef = viewChild<Drawer>('drawerEl');
  private readonly cdr = inject(ChangeDetectorRef);

  saved = output<Payment>();

  visible = false;
  saving = signal(false);
  error = signal<string | null>(null);
  charges = signal<ContributionCharge[]>([]);
  paymentMethods = [...PAYMENT_METHODS];
  memberName = signal<string | null>(null);

  form = { charge_id: '', amount: 0, payment_method: 'cash', payment_date: '', notes: '' };

  chargeOptions() {
    return this.charges().map(c => ({
      label: `${c.contribution_type_name} (solde: ${c.balance.toLocaleString()} FCFA)`,
      value: c.id,
    }));
  }

  open(preselectedCharge?: { id: string; balance: number; label: string }) {
    const user = this.authStore.currentUser();
    const isMember = user?.entity_type === 'member' && !!user?.entity_id;
    const memberId = isMember ? user!.entity_id : undefined;

    this.memberName.set(isMember ? user!.full_name : null);
    this.form = {
      charge_id: preselectedCharge?.id ?? '',
      amount: preselectedCharge?.balance ?? 0,
      payment_method: 'cash',
      payment_date: new Date().toISOString().split('T')[0],
      notes: '',
    };
    this.error.set(null);
    this.visible = true;
    this.cdr.detectChanges();
    this.contributionsApi.getCharges(1, 100, memberId, undefined, 'pending').subscribe({
      next: (res) => {
        this.charges.set(res.data);
        // If pre-selected charge not in pending list (e.g. partially paid), inject it
        if (preselectedCharge && !res.data.find(c => c.id === preselectedCharge.id)) {
          this.contributionsApi.getCharges(1, 100, memberId).subscribe({
            next: (all) => this.charges.set(all.data),
          });
        }
      },
    });
  }

  submit() {
    if (!this.form.charge_id || !this.form.amount || !this.form.payment_date) {
      this.error.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api.record(this.form).subscribe({
      next: (payment) => {
        this.saving.set(false);
        this.toast.success('Paiement enregistré.');
        this.drawerRef()?.close(new MouseEvent('click'));
        this.saved.emit(payment);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error('Erreur lors de l\'enregistrement.');
        this.error.set(err?.error?.error ?? 'Une erreur est survenue.');
      },
    });
  }
}
