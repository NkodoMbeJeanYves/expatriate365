import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-brand-logo',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <a routerLink="/" class="flex items-center gap-3 no-underline">
      <div
        class="w-9 h-9 rounded-xl flex items-center justify-center shadow font-extrabold text-white text-sm select-none"
        style="background: linear-gradient(135deg, #059669 0%, #0d9488 100%)"
      >
        E3
      </div>
      <span class="font-bold text-gray-900 dark:text-white text-xl tracking-tight">
        Expatriate<span style="color: #059669">365</span>
      </span>
    </a>
  `,
})
export class BrandLogoComponent {}
