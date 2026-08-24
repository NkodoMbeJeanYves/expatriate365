import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Payment, PAYMENT_METHODS } from '@models/payment.model';
import { TranslatePipe } from '@ngx-translate/core';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';

@Component({
  selector: 'app-payment-receipt',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppCurrencyPipe, DatePipe, TranslatePipe],
  template: `
    @if (payment()) {
      <div class="bg-white rounded-2xl shadow-sm border border-gray-100 p-6 max-w-md mx-auto print:shadow-none print:border-0" id="receipt">
        <!-- Header -->
        <div class="flex items-center justify-between mb-6">
          <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 flex items-center justify-center">
            <i class="pi pi-check text-white text-sm"></i>
          </div>
          <div class="text-right">
            <p class="text-xs text-gray-400 uppercase tracking-wider">{{ 'payments.receipt_number' | translate }}</p>
            <p class="text-sm font-bold text-gray-900">{{ payment()!.receipt_number }}</p>
          </div>
        </div>

        <!-- Amount -->
        <div class="text-center py-6 border-t border-b border-dashed border-gray-200 mb-6">
          <p class="text-4xl font-bold text-gray-900">{{ payment()!.amount | appCurrency }}</p>
        </div>

        <!-- Details -->
        <div class="space-y-3 text-sm">
          <div class="flex justify-between">
            <span class="text-gray-500">{{ 'common.member' | translate }}</span>
            <span class="font-medium text-gray-900">{{ payment()!.member_name }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ 'members.membership_number' | translate }}</span>
            <span class="font-medium text-gray-900">{{ payment()!.membership_number }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ 'contributions.title' | translate }}</span>
            <span class="font-medium text-gray-900">{{ payment()!.contribution_type_name }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ 'payments.method' | translate }}</span>
            <span class="font-medium text-gray-900">{{ methodLabel() }}</span>
          </div>
          <div class="flex justify-between">
            <span class="text-gray-500">{{ 'common.date' | translate }}</span>
            <span class="font-medium text-gray-900">{{ payment()!.payment_date }}</span>
          </div>
          @if (payment()!.confirmed_at) {
            <div class="flex justify-between">
              <span class="text-gray-500">{{ 'payments.confirmed_at' | translate }}</span>
              <span class="font-medium text-gray-900">{{ payment()!.confirmed_at | date:'dd/MM/yyyy HH:mm' }}</span>
            </div>
          }
          @if (payment()!.notes) {
            <div class="flex justify-between">
              <span class="text-gray-500">{{ 'common.notes' | translate }}</span>
              <span class="font-medium text-gray-900 text-right max-w-48">{{ payment()!.notes }}</span>
            </div>
          }
        </div>

        <!-- Footer -->
        <div class="mt-6 pt-4 border-t border-gray-100 text-center">
          <p class="text-xs text-gray-400">Document généré le {{ now | date:'dd/MM/yyyy HH:mm' }}</p>
        </div>
      </div>
    }
  `,
})
export class PaymentReceiptComponent {
  payment = input<Payment | null>(null);
  now = new Date();

  methodLabel() {
    return PAYMENT_METHODS.find(m => m.value === this.payment()?.payment_method)?.label ?? this.payment()?.payment_method ?? '';
  }
}
