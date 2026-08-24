import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { MeetingDto, MeetingAttendanceDto, ATTENDANCE_STATUSES } from '@models/meeting.model';
import { MeetingsApiService } from '../../services/meetings-api.service';

@Component({
  selector: 'app-meeting-attendance-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DrawerModule, ButtonModule, TagModule, SelectModule, InputTextModule, TranslatePipe],
  template: `
    <p-drawer [(visible)]="visible" [position]="'right'" [style]="{ width: '700px' }" [header]="meeting()?.title + ' — Présences'">
      <div class="p-4 flex flex-col gap-4">
        <div class="flex items-center gap-4 text-sm text-gray-600">
          <span><strong>{{ presentCount() }}</strong> présent(s)</span>
          <span><strong>{{ attendances().length }}</strong> enregistré(s)</span>
          @if (meeting()?.quorum_required) {
            <span class="font-medium" [class.text-green-600]="presentCount() >= meeting()!.quorum_required!" [class.text-red-500]="presentCount() < meeting()!.quorum_required!">
              Quorum : {{ meeting()!.quorum_required }}
            </span>
          }
        </div>

        <div class="overflow-auto max-h-[420px] border border-gray-200 rounded-lg">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider sticky top-0">
              <tr>
                <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th>
                <th class="text-left px-4 py-3">N° adhérent</th>
                <th class="text-left px-4 py-3" style="width:160px">{{ 'common.status' | translate }}</th>
                <th class="text-left px-4 py-3">Mandataire</th>
                <th class="px-4 py-3" style="width:60px"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 bg-white">
              @if (loading()) {
                @for (i of [1,2,3,4]; track i) {
                  <tr><td colspan="5" class="px-4 py-3"><div class="h-8 bg-gray-100 rounded animate-pulse"></div></td></tr>
                }
              } @else if (!attendances().length) {
                <tr><td colspan="5" class="text-center px-4 py-6 text-gray-400">{{ 'common.no_data' | translate }}</td></tr>
              } @else {
                @for (a of attendances(); track a.member_id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3 font-medium text-sm">{{ a.member_name }}</td>
                    <td class="px-4 py-3 font-mono text-xs text-gray-500">{{ a.membership_number }}</td>
                    <td class="px-4 py-3">
                      <p-select
                        [options]="statusOptions"
                        optionLabel="label" optionValue="value"
                        [(ngModel)]="a.status"
                        (ngModelChange)="markDirty(a)"
                        styleClass="w-full" />
                    </td>
                    <td class="px-4 py-3">
                      @if (a.status === 'proxy') {
                        <input pInputText [(ngModel)]="a.proxy_name" placeholder="Nom du mandataire" (ngModelChange)="markDirty(a)" class="w-full" />
                      }
                    </td>
                    <td class="px-4 py-3">
                      <p-tag [value]="a.status" [severity]="statusSeverity(a.status)" />
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>

        @if (dirty().size > 0) {
          <div class="flex justify-end">
            <p-button [label]="'common.save' | translate" icon="pi pi-check" [loading]="saving()" (onClick)="saveAll()" />
          </div>
        }
        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }
      </div>
    </p-drawer>
  `,
})
export class MeetingAttendanceDrawerComponent {
  private readonly api = inject(MeetingsApiService);

  protected visible = false;
  protected readonly meeting = signal<MeetingDto | null>(null);
  protected readonly attendances = signal<MeetingAttendanceDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly dirty = signal<Set<string>>(new Set());

  protected readonly presentCount = () =>
    this.attendances().filter(a => a.status === 'present' || a.status === 'proxy').length;

  protected readonly statusOptions = [...ATTENDANCE_STATUSES].map(s => ({
    label: s === 'present' ? 'Présent' : s === 'absent' ? 'Absent' : s === 'excused' ? 'Excusé' : 'Représenté',
    value: s,
  }));

  open(m: MeetingDto): void {
    this.meeting.set(m);
    this.dirty.set(new Set());
    this.error.set(null);
    this.visible = true;
    this.loading.set(true);
    this.api.getById(m.id).subscribe({
      next: res => { this.attendances.set(res.attendances.map(a => ({ ...a }))); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Erreur lors du chargement.'); },
    });
  }

  protected markDirty(a: MeetingAttendanceDto): void {
    this.dirty.update(s => { const n = new Set(s); n.add(a.member_id); return n; });
  }

  protected statusSeverity(s: string): 'success' | 'warn' | 'danger' | 'secondary' {
    return s === 'present' ? 'success' : s === 'proxy' ? 'warn' : s === 'excused' ? 'secondary' : 'danger';
  }

  protected saveAll(): void {
    const entries = this.attendances()
      .filter(a => this.dirty().has(a.member_id))
      .map(a => ({ member_id: a.member_id, status: a.status, proxy_name: a.proxy_name }));
    if (!entries.length || !this.meeting()) return;

    this.saving.set(true);
    this.api.recordAttendance(this.meeting()!.id, { entries }).subscribe({
      next: () => { this.saving.set(false); this.dirty.set(new Set()); },
      error: () => { this.saving.set(false); this.error.set('Erreur lors de l\'enregistrement.'); },
    });
  }
}
