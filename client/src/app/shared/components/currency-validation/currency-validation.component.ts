import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CurrencyValidationResponse } from '@models/currency-validation.model';
import { CurrencyValidationService } from '@service/currency-validation.service';
import { switchMap, timer } from 'rxjs';

@Component({
  selector: 'app-currency-validation',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, DatePipe],
  template: `
    <div class="text-sm flex flex-col gap-1" role="status" aria-live="polite">
      @if (loading()) {
        <span>{{ 'settings.currency_checking' | translate }}</span>
      } @else if (result(); as validation) {
        @if (validation.is_consistent) {
          <span class="text-emerald-600">{{ 'settings.currency_consistent' | translate }}</span>
          <span [class.text-amber-600]="validation.coverage_status !== 'covered'">
            {{ coverageKey() | translate }}
          </span>
          @if (validation.checked_at) {
            <span>{{ validation.source }} · {{ validation.checked_at | date:'medium' }}</span>
          }
        } @else {
          <span class="text-red-600">{{ 'errors.tenant.currency_mismatch' | translate }}</span>
          @if (validation.allowed_currencies.length) {
            <span>{{ 'settings.currency_allowed' | translate }} {{ allowedCurrencies() }}</span>
          }
        }
      } @else if (failed()) {
        <span class="text-amber-600">{{ 'settings.currency_check_failed' | translate }}</span>
      }
      <button type="button" class="text-blue-600 underline self-start" (click)="retry()">
        {{ 'settings.currency_verify' | translate }}
      </button>
    </div>
  `,
})
export class CurrencyValidationComponent {
  readonly country = input.required<string | null>();
  readonly currency = input.required<string | null>();
  readonly superAdmin = input(false);
  private readonly service = inject(CurrencyValidationService);
  private readonly attempt = signal(0);
  readonly result = signal<CurrencyValidationResponse | null>(null);
  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly coverageKey = computed(() => `settings.currency_${this.result()?.coverage_status ?? 'unavailable'}`);
  readonly allowedCurrencies = computed(() => this.result()?.allowed_currencies.join(', ') ?? '');

  constructor() {
    effect(onCleanup => {
      const country = this.country()?.trim().toUpperCase() ?? '';
      const currency = this.currency()?.trim().toUpperCase() ?? '';
      const superAdmin = this.superAdmin();
      this.attempt();
      this.result.set(null);
      this.failed.set(false);
      this.loading.set(false);
      if (!/^[A-Z]{2}$/.test(country) || !/^[A-Z]{3}$/.test(currency)) return;
      this.loading.set(true);
      const subscription = timer(300).pipe(
        switchMap(() => this.service.validate(country, currency, superAdmin)),
      ).subscribe({
        next: result => { this.result.set(result); this.loading.set(false); },
        error: () => { this.failed.set(true); this.loading.set(false); },
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  retry(): void { this.attempt.update(value => value + 1); }
}
