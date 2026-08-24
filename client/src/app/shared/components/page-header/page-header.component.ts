import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { TenantStore } from '@core/tenant/tenant.store';

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div class="flex items-center gap-3 mb-6">
      @if (tenantStore.logoUrl()) {
        <img
          [src]="tenantStore.logoUrl()!"
          alt="logo"
          class="w-10 h-10 rounded-lg object-contain bg-white border border-gray-100 flex-shrink-0" />
      }
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="text-sm text-gray-500 mt-0.5">{{ subtitle() }}</p>
        }
      </div>
    </div>
  `,
})
export class PageHeaderComponent {
  readonly tenantStore = inject(TenantStore);
  readonly title    = input.required<string>();
  readonly subtitle = input<string>();
}
