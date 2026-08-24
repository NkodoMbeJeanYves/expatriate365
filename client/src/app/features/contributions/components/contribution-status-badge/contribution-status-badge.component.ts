import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-contribution-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, TranslatePipe],
  template: `
    <span [class]="badgeClass()">{{ ('contributions.status_' + status()) | translate }}</span>
  `,
})
export class ContributionStatusBadgeComponent {
  status = input.required<string>();

  badgeClass() {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    const map: Record<string, string> = {
      pending: 'bg-yellow-100 text-yellow-800',
      paid: 'bg-green-100 text-green-800',
      overdue: 'bg-red-100 text-red-800',
      waived: 'bg-gray-100 text-gray-600',
    };
    return `${base} ${map[this.status()] ?? 'bg-gray-100 text-gray-600'}`;
  }
}
