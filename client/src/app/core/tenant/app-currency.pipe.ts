import { Pipe, PipeTransform, inject } from '@angular/core';
import { TenantStore } from './tenant.store';

/**
 * Formats a number using the tenant's configured currency symbol.
 * Usage: {{ amount | appCurrency }}          → "1 500 €"
 *        {{ amount | appCurrency:'symbol' }}  → "€"
 *        {{ amount | appCurrency:'code' }}    → "EUR"
 */
@Pipe({ name: 'appCurrency', standalone: true, pure: false })
export class AppCurrencyPipe implements PipeTransform {
  private readonly tenant = inject(TenantStore);

  transform(value: number | null | undefined, mode: 'full' | 'symbol' | 'code' = 'full'): string {
    if (mode === 'symbol') return this.tenant.symbol();
    if (mode === 'code')   return this.tenant.currency();
    if (value == null)     return `— ${this.tenant.symbol()}`;

    const formatted = new Intl.NumberFormat('fr-FR', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(value);

    return `${formatted} ${this.tenant.symbol()}`;
  }
}
