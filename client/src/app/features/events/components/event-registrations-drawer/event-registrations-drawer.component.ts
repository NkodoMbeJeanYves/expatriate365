import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { EventDto, EventRegistrationDto } from '@models/event.model';
import { EventsApiService } from '../../services/events-api.service';
import { MembersApiService } from '@members/services/members-api.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-event-registrations-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, DrawerModule, ButtonModule,
    TagModule, SelectModule, TranslatePipe,
  ],
  template: `
    <p-drawer [(visible)]="visible" [position]="'right'" [style]="{ width: '680px' }" [header]="event()?.title + ' — Inscriptions'">
      <div class="p-4 flex flex-col gap-4">
        @if (event()?.status === 'published') {
          <div class="flex gap-2">
            <p-select
              [options]="memberOptions()"
              [(ngModel)]="selectedMemberId"
              optionLabel="label"
              optionValue="value"
              [filter]="true"
              filterBy="label"
              placeholder="Rechercher un membre…"
              styleClass="flex-1" />
            <p-button label="Inscrire" (onClick)="registerMember()" [loading]="registering()" [disabled]="!selectedMemberId" />
          </div>
        }
        <div class="overflow-auto max-h-[500px] border border-gray-200 rounded-lg">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-xs font-semibold text-gray-500 uppercase tracking-wider sticky top-0">
              <tr>
                <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th>
                <th class="text-left px-4 py-3">N° adhérent</th>
                <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
                <th class="text-left px-4 py-3">{{ 'common.created_at' | translate }}</th>
                @if (event()?.status === 'completed') {
                  <th class="text-left px-4 py-3">Présence</th>
                }
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 bg-white">
              @if (loading()) {
                @for (i of [1,2,3,4]; track i) {
                  <tr><td colspan="6" class="px-4 py-3"><div class="h-8 bg-gray-100 rounded animate-pulse"></div></td></tr>
                }
              } @else if (!registrations().length) {
                <tr><td colspan="6" class="text-center px-4 py-6 text-gray-400">{{ 'common.no_data' | translate }}</td></tr>
              } @else {
                @for (reg of registrations(); track reg.id) {
                  <tr class="hover:bg-gray-50">
                    <td class="px-4 py-3">{{ reg.member_name }}</td>
                    <td class="px-4 py-3 font-mono text-xs">{{ reg.membership_number }}</td>
                    <td class="px-4 py-3">
                      <p-tag [value]="reg.status" [severity]="regSeverity(reg.status)" />
                    </td>
                    <td class="px-4 py-3 text-sm text-gray-500">{{ reg.created_at | date:'short' }}</td>
                    @if (event()?.status === 'completed') {
                      <td class="px-4 py-3">
                        @if (reg.status !== 'cancelled') {
                          <p-button
                            [label]="reg.status === 'attended' ? ('meetings.attendance_present' | translate) : ('meetings.attendance_absent' | translate)"
                            [severity]="reg.status === 'attended' ? 'success' : 'secondary'"
                            size="small"
                            (onClick)="toggleAttendance(reg)" />
                        }
                      </td>
                    }
                    <td class="px-4 py-3">
                      @if (reg.status === 'registered') {
                        <p-button icon="pi pi-times" severity="danger" [text]="true" size="small" (onClick)="cancelReg(reg)" />
                      }
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>
        </div>
        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }
      </div>
    </p-drawer>
  `,
})
export class EventRegistrationsDrawerComponent {
  private readonly api = inject(EventsApiService);
  private readonly membersApi = inject(MembersApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  protected visible = false;
  protected readonly event = signal<EventDto | null>(null);
  protected readonly registrations = signal<EventRegistrationDto[]>([]);
  protected readonly members = signal<{ label: string; value: string }[]>([]);
  protected readonly loading = signal(false);
  protected readonly registering = signal(false);
  protected readonly error = signal<string | null>(null);
  protected selectedMemberId = '';

  protected memberOptions() {
    return this.members();
  }

  open(ev: EventDto): void {
    this.event.set(ev);
    this.error.set(null);
    this.selectedMemberId = '';
    this.visible = true;
    this.cdr.detectChanges();
    this.loadRegistrations(ev.id);
    this.membersApi.list({ page: 1, limit: 500, status: 'active' }).subscribe({
      next: res => this.members.set(res.data.map(m => ({
        label: `${m.first_name} ${m.last_name} (${m.membership_number})`,
        value: m.id,
      }))),
    });
  }

  private loadRegistrations(id: string): void {
    this.loading.set(true);
    this.api.getRegistrations(id).subscribe({
      next: regs => { this.registrations.set(regs); this.loading.set(false); },
      error: () => { this.loading.set(false); this.error.set('Erreur lors du chargement.'); },
    });
  }

  protected regSeverity(status: string): 'success' | 'warn' | 'danger' | 'secondary' {
    return status === 'attended' ? 'success' : status === 'no_show' ? 'danger' : status === 'cancelled' ? 'secondary' : 'warn';
  }

  protected registerMember(): void {
    if (!this.selectedMemberId || !this.event()) return;
    this.registering.set(true);
    this.error.set(null);
    this.api.register(this.event()!.id, { member_id: this.selectedMemberId }).subscribe({
      next: (reg) => {
        this.registrations.update(list => [...list, reg]);
        this.selectedMemberId = '';
        this.registering.set(false);
      },
      error: (err) => { this.registering.set(false); this.error.set(err?.error?.error ?? 'Erreur lors de l\'inscription.'); },
    });
  }

  protected cancelReg(reg: EventRegistrationDto): void {
    this.api.cancelRegistration(this.event()!.id, reg.id).subscribe({
      next: (updated) => this.registrations.update(list => list.map(r => r.id === updated.id ? updated : r)),
      error: () => this.error.set('Erreur lors de l\'annulation.'),
    });
  }

  protected toggleAttendance(reg: EventRegistrationDto): void {
    const newStatus = reg.status === 'attended' ? 'no_show' : 'attended';
    this.api.markAttendance(this.event()!.id, { entries: [{ registration_id: reg.id, status: newStatus }] }).subscribe({
      next: () => this.registrations.update(list => list.map(r => r.id === reg.id ? { ...r, status: newStatus } : r)),
      error: () => this.error.set('Erreur lors de la mise à jour.'),
    });
  }
}
