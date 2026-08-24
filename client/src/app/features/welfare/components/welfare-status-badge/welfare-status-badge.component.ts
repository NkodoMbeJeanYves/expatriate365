import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-welfare-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  template: `<span [class]="badgeClass()">{{ ('welfare.status_' + status()) | translate }}</span>`,
})
export class WelfareStatusBadgeComponent {
  status = input.required<string>();

  badgeClass() {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const map: Record<string, string> = {
      pending: 'bg-yellow-100 text-yellow-800',
      approved: 'bg-blue-100 text-blue-800',
      rejected: 'bg-red-100 text-red-700',
      paid: 'bg-green-100 text-green-800',
    };
    return `${base} ${map[this.status()] ?? 'bg-gray-100 text-gray-600'}`;
  }
}
