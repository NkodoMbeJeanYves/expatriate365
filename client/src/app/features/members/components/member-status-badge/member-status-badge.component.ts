import { ChangeDetectionStrategy, Component, input, computed } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { TranslatePipe } from '@ngx-translate/core';
import { MemberStatus } from '@core/models/member.model';

@Component({
  selector: 'app-member-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule, TranslatePipe],
  template: `<p-tag [value]="('members.status_' + status()) | translate" [severity]="severity()" />`,
})
export class MemberStatusBadgeComponent {
  readonly status = input.required<MemberStatus>();

  readonly severity = computed((): 'success' | 'warn' | 'danger' | 'secondary' => ({
    active: 'success' as const,
    suspended: 'warn' as const,
    inactive: 'danger' as const,
    pending: 'secondary' as const,
  }[this.status()]));
}
