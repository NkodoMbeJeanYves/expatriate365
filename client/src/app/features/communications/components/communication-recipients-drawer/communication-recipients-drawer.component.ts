import {
  ChangeDetectionStrategy, Component, input, output, signal,
} from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';
import { RecipientDto } from '@models/communication.model';

@Component({
  selector: 'app-communication-recipients-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DrawerModule, ButtonModule, TagModule, TranslatePipe],
  template: `
    <p-drawer [visible]="visible()" [header]="'communications.recipients' | translate"
      position="right" styleClass="!w-full md:!w-[480px]"
      (visibleChange)="closed.emit()">

      <div class="flex flex-col gap-2 p-2">
        <p class="text-sm text-surface-500">{{ recipients().length }} destinataire(s)</p>

        @for (r of recipients(); track r.id) {
          <div class="flex items-center justify-between p-3 rounded-lg border border-surface-200 dark:border-surface-700">
            <div>
              <p class="font-medium text-sm">{{ r.member_name }}</p>
              <p class="text-xs text-surface-500">{{ r.membership_number }}</p>
            </div>
            <p-tag [value]="'communications.' + r.status | translate"
              [severity]="r.status === 'read' ? 'success' : 'secondary'" />
          </div>
        }

        @if (recipients().length === 0) {
          <p class="text-center text-surface-400 py-8">{{ 'common.no_data' | translate }}</p>
        }
      </div>
    </p-drawer>
  `,
})
export class CommunicationRecipientsDrawerComponent {
  readonly visible = input.required<boolean>();
  readonly recipients = input<RecipientDto[]>([]);
  readonly closed = output<void>();
}
