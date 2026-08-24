import { ChangeDetectionStrategy, Component, OnInit, inject, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MeetingDto, MeetingMinuteDto, MEETING_STATUSES, MEETING_TYPES } from '@models/meeting.model';
import { MeetingsStore } from '../../store/meetings.store';
import { MeetingsApiService } from '../../services/meetings-api.service';
import { MeetingFormDrawerComponent } from '../../components/meeting-form-drawer/meeting-form-drawer.component';
import { MeetingAttendanceDrawerComponent } from '../../components/meeting-attendance-drawer/meeting-attendance-drawer.component';
import { MeetingMinutesDrawerComponent } from '../../components/meeting-minutes-drawer/meeting-minutes-drawer.component';

@Component({
  selector: 'app-meeting-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, ButtonModule, CardModule, TagModule,
    SelectModule, ProgressSpinnerModule, TooltipModule, TranslatePipe,
    MeetingFormDrawerComponent, MeetingAttendanceDrawerComponent, MeetingMinutesDrawerComponent,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'meetings.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'meetings.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'meetings.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total }}</div>
            <div class="text-xs text-gray-500 mt-1">Total</div>
          </div>
          <div class="bg-blue-50 rounded-xl p-4 border border-blue-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-blue-700">{{ s.scheduled }}</div>
            <div class="text-xs text-blue-600 mt-1">{{ 'meetings.status_scheduled' | translate }}</div>
          </div>
          <div class="bg-yellow-50 rounded-xl p-4 border border-yellow-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-yellow-700">{{ s.in_progress }}</div>
            <div class="text-xs text-yellow-600 mt-1">{{ 'meetings.status_in_progress' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ s.completed }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'meetings.status_completed' | translate }}</div>
          </div>
          <div class="bg-red-50 rounded-xl p-4 border border-red-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-red-700">{{ s.cancelled }}</div>
            <div class="text-xs text-red-600 mt-1">{{ 'meetings.status_cancelled' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          placeholder="Tous les statuts" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          placeholder="Tous les types" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Liste -->
      @if (store.loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.meetings().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-users text-4xl mb-3 block"></i>
          <p>{{ 'meetings.no_meetings' | translate }}</p>
        </div>
      } @else {
        <div class="flex flex-col gap-3">
          @for (m of store.meetings(); track m.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow p-4">
              <div class="flex items-start justify-between gap-4">
                <div class="flex flex-col gap-1 flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <h3 class="font-semibold text-gray-800">{{ m.title }}</h3>
                    <p-tag [value]="statusLabel(m.status)" [severity]="statusSeverity(m.status)" />
                    <p-tag [value]="typeLabel(m.type)" severity="secondary" />
                  </div>
                  <div class="flex items-center gap-4 text-xs text-gray-500 flex-wrap">
                    <span><i class="pi pi-calendar mr-1"></i>{{ m.scheduled_at | date:'d MMM y, HH:mm' }}</span>
                    @if (m.location) { <span><i class="pi pi-map-marker mr-1"></i>{{ m.location }}</span> }
                    <span><i class="pi pi-users mr-1"></i>{{ m.present_count }}/{{ m.attendance_count }} présents</span>
                    @if (m.quorum_required) {
                      <span [class.text-green-600]="m.present_count >= m.quorum_required" [class.text-red-500]="m.present_count < m.quorum_required">
                        Quorum : {{ m.quorum_required }}
                      </span>
                    }
                    @if (m.has_minutes) { <span class="text-indigo-600"><i class="pi pi-file-edit mr-1"></i>Compte-rendu</span> }
                  </div>
                </div>

                <!-- Actions -->
                <div class="flex gap-1 shrink-0">
                  <p-button icon="pi pi-users" size="small" severity="secondary" [text]="true"
                    [pTooltip]="'meetings.attendances' | translate" (onClick)="openAttendance(m)" />
                  <p-button icon="pi pi-file-edit" size="small" severity="secondary" [text]="true"
                    [pTooltip]="'meetings.minutes' | translate" (onClick)="openMinutes(m)" />
                  @if (m.status === 'scheduled') {
                    <p-button icon="pi pi-play" size="small" severity="info" [text]="true"
                      [pTooltip]="'meetings.start_meeting' | translate" (onClick)="start(m)" />
                    <p-button icon="pi pi-pencil" size="small" severity="secondary" [text]="true"
                      [pTooltip]="'common.edit' | translate" (onClick)="openForm(m)" />
                    <p-button icon="pi pi-times" size="small" severity="danger" [text]="true"
                      [pTooltip]="'common.cancel' | translate" (onClick)="cancel(m)" />
                  }
                  @if (m.status === 'in_progress') {
                    <p-button icon="pi pi-stop-circle" size="small" severity="success" [text]="true"
                      [pTooltip]="'meetings.end_meeting' | translate" (onClick)="close(m)" />
                  }
                </div>
              </div>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (store.pagination().total > store.pagination().limit) {
          <div class="flex justify-center gap-2 mt-2">
            <p-button icon="pi pi-chevron-left" severity="secondary" [text]="true"
              [disabled]="store.pagination().page <= 1" (onClick)="goToPage(store.pagination().page - 1)" />
            <span class="px-3 py-1 text-sm text-gray-600">
              Page {{ store.pagination().page }} / {{ totalPages() }}
            </span>
            <p-button icon="pi pi-chevron-right" severity="secondary" [text]="true"
              [disabled]="store.pagination().page >= totalPages()" (onClick)="goToPage(store.pagination().page + 1)" />
          </div>
        }
      }
    </div>

    <app-meeting-form-drawer #formDrawer (saved)="onSaved($event)" />
    <app-meeting-attendance-drawer #attendanceDrawer />
    <app-meeting-minutes-drawer #minutesDrawer />
  `,
})
export class MeetingListPage implements OnInit {
  protected readonly store = inject(MeetingsStore);
  private readonly api = inject(MeetingsApiService);
  private readonly translate = inject(TranslateService);

  private readonly formDrawer = viewChild.required<MeetingFormDrawerComponent>('formDrawer');
  private readonly attendanceDrawer = viewChild.required<MeetingAttendanceDrawerComponent>('attendanceDrawer');
  private readonly minutesDrawer = viewChild.required<MeetingMinutesDrawerComponent>('minutesDrawer');

  protected filterStatus: string | null = null;
  protected filterType: string | null = null;

  protected readonly statusOptions = [...MEETING_STATUSES].map(s => ({ label: this.statusLabelFn(s), value: s }));
  protected readonly typeOptions = [...MEETING_TYPES].map(t => ({ label: this.typeLabelFn(t), value: t }));

  ngOnInit(): void { this.store.loadMeetings(); this.store.loadStats(); }

  protected totalPages(): number {
    const p = this.store.pagination();
    return Math.ceil(p.total / p.limit);
  }

  protected statusSeverity(s: string): 'success' | 'warn' | 'danger' | 'secondary' | 'info' {
    return s === 'scheduled' ? 'info' : s === 'in_progress' ? 'warn' : s === 'completed' ? 'success' : 'danger';
  }

  protected statusLabel(s: string): string { return this.statusLabelFn(s); }
  protected typeLabel(t: string): string { return this.typeLabelFn(t); }

  private statusLabelFn(s: string): string {
    return this.translate.instant('meetings.status_' + s);
  }
  private typeLabelFn(t: string): string {
    const map: Record<string, string> = { general: 'meetings.type_general', board: 'meetings.type_board', extraordinary: 'meetings.type_extraordinary' };
    return this.translate.instant(map[t] ?? 'meetings.type_other');
  }

  protected openForm(m?: MeetingDto): void { this.formDrawer().open(m); }
  protected openAttendance(m: MeetingDto): void { this.attendanceDrawer().open(m); }

  protected openMinutes(m: MeetingDto): void {
    this.api.getById(m.id).subscribe({
      next: res => this.minutesDrawer().open(m, res.minute),
    });
  }

  protected applyFilters(): void {
    this.store.loadMeetings({ page: 1, status: this.filterStatus ?? undefined, type: this.filterType ?? undefined });
  }
  protected goToPage(page: number): void { this.store.loadMeetings({ page }); }
  protected onSaved(m: MeetingDto): void { this.store.upsertMeeting(m); this.store.loadStats(); }

  protected start(m: MeetingDto): void {
    this.api.start(m.id).subscribe({ next: updated => this.store.upsertMeeting(updated) });
  }
  protected close(m: MeetingDto): void {
    this.api.close(m.id).subscribe({ next: updated => this.store.upsertMeeting(updated) });
  }
  protected cancel(m: MeetingDto): void {
    this.api.cancel(m.id).subscribe({ next: updated => this.store.upsertMeeting(updated) });
  }
}
