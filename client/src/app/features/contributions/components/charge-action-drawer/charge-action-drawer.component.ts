import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input, output, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { ContributionCharge } from '@models/contribution.model';
import { ContributionsApiService } from '../../services/contributions-api.service';
import { TranslatePipe } from '@ngx-translate/core';
import { ToastService } from '@service/toast.service';

@Component({
  selector: 'app-charge-action-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppCurrencyPipe, DrawerModule, ButtonModule, InputNumberModule, FormsModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [header]="action() === 'pay' ? ('payments.new' | translate) : ('contributions.waiver' | translate)" position="right" styleClass="!w-full sm:!w-96">
      @if (charge()) {
        <div class="p-4 space-y-6">
          <div class="bg-gray-50 rounded-xl p-4 space-y-2">
            <p class="text-sm font-medium text-gray-900">{{ charge()!.member_name }}</p>
            <p class="text-xs text-gray-500">{{ charge()!.contribution_type_name }}</p>
            <div class="flex justify-between text-sm mt-2">
              <span class="text-gray-500">{{ 'contributions.balance' | translate }}</span>
              <span class="font-semibold text-gray-900">{{ charge()!.balance | appCurrency }}</span>
            </div>
          </div>

          @if (action() === 'pay') {
            <div class="space-y-2">
              <label class="block text-sm font-medium text-gray-700">{{ 'contributions.amount_paid' | translate }}</label>
              <p-inputnumber [(ngModel)]="amount" [min]="1" [max]="charge()!.balance" mode="currency" currency="XAF" locale="fr-CM" styleClass="w-full" />
            </div>
          } @else {
            <div class="space-y-2">
              <label class="block text-sm font-medium text-gray-700">{{ 'contributions.waiver' | translate }}</label>
              <p-inputnumber [(ngModel)]="amount" [min]="0" [max]="charge()!.balance" mode="currency" currency="XAF" locale="fr-CM" styleClass="w-full" />
              <p class="text-xs text-gray-400">Laisser vide pour exonérer le solde total.</p>
            </div>
          }

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
            {{ saving() ? ('common.loading' | translate) : (action() === 'pay' ? ('payments.confirm_payment' | translate) : ('contributions.waiver' | translate)) }}
          </button>
        </div>
      }
    </p-drawer>
  `,
})
export class ChargeActionDrawerComponent {
  private readonly api = inject(ContributionsApiService);
  protected readonly drawerRef = viewChild<Drawer>('drawerEl');
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly toast = inject(ToastService);

  charge = input<ContributionCharge | null>(null);
  action = input<'pay' | 'waive'>('pay');
  saved = output<ContributionCharge>();

  visible = false;
  amount = signal<number | null>(null);
  saving = signal(false);
  error = signal<string | null>(null);

  open() {
    this.amount.set(null);
    this.error.set(null);
    this.visible = true;
    this.cdr.detectChanges();
  }

  submit() {
    const c = this.charge();
    if (!c) return;
    this.saving.set(true);
    this.error.set(null);

    const obs = this.action() === 'pay'
      ? this.api.markPaid(c.id, { amount_paid: this.amount() ?? c.balance })
      : this.api.waive(c.id, { waiver_amount: this.amount() ?? undefined });

    obs.subscribe({
      next: (updated) => {
        this.saving.set(false);
        if (this.action() === 'pay') {
          this.toast.success('Paiement enregistré.');
        } else {
          this.toast.success('Exonération appliquée.');
        }
        this.drawerRef()?.close(new MouseEvent('click'));
        this.saved.emit(updated);
      },
      error: (err) => {
        this.saving.set(false);
        this.toast.error('Erreur lors de l\'opération.');
        this.error.set(err?.error?.error ?? 'Une erreur est survenue.');
      },
    });
  }
}
