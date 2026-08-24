import { ChangeDetectionStrategy, Component, OnInit, inject, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { EventDto, EVENT_STATUSES, EVENT_TYPES } from '@models/event.model';
import { EventsStore } from '../../store/events.store';
import { EventFormDrawerComponent } from '../../components/event-form-drawer/event-form-drawer.component';
import { EventRegistrationsDrawerComponent } from '../../components/event-registrations-drawer/event-registrations-drawer.component';
import { EventsApiService } from '../../services/events-api.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-event-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, ButtonModule, CardModule,
    TagModule, SelectModule, ProgressSpinnerModule, TooltipModule,
    EventFormDrawerComponent, EventRegistrationsDrawerComponent,
    TranslatePipe,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'events.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'events.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'events.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total_count }}</div>
            <div class="text-xs text-gray-500 mt-1">Total</div>
          </div>
          <div class="bg-yellow-50 rounded-xl p-4 border border-yellow-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-yellow-700">{{ s.draft_count }}</div>
            <div class="text-xs text-yellow-600 mt-1">{{ 'events.status_draft' | translate }}</div>
          </div>
          <div class="bg-blue-50 rounded-xl p-4 border border-blue-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-blue-700">{{ s.published_count }}</div>
            <div class="text-xs text-blue-600 mt-1">{{ 'events.status_published' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ s.completed_count }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'events.status_completed' | translate }}</div>
          </div>
          <div class="bg-purple-50 rounded-xl p-4 border border-purple-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-purple-700">{{ s.total_registrations }}</div>
            <div class="text-xs text-purple-600 mt-1">{{ 'events.registrations' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filters -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          placeholder="Tous les statuts" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          placeholder="Tous les types" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Grid -->
      @if (store.loading()) {
        <div class="flex justify-center py-12">
          <p-progressspinner strokeWidth="4" />
        </div>
      } @else if (store.events().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-calendar text-4xl mb-3 block"></i>
          <p>{{ 'events.no_events' | translate }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (ev of store.events(); track ev.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow overflow-hidden">
              <div class="p-4 flex flex-col gap-3">
                <div class="flex items-start justify-between gap-2">
                  <h3 class="font-semibold text-gray-800 leading-tight">{{ ev.title }}</h3>
                  <p-tag [value]="'events.status_' + ev.status | translate" [severity]="statusSeverity(ev.status)" class="shrink-0" />
                </div>
                <div class="flex items-center gap-2 text-xs text-gray-500">
                  <i class="pi pi-tag"></i>
                  <span>{{ ev.type }}</span>
                  @if (ev.location) { <span>·</span><i class="pi pi-map-marker"></i><span>{{ ev.location }}</span> }
                </div>
                <div class="flex items-center gap-2 text-xs text-gray-500">
                  <i class="pi pi-calendar"></i>
                  <span>{{ ev.start_date | date:'d MMM y, HH:mm' }}</span>
                </div>
                <div class="flex items-center justify-between text-xs text-gray-500">
                  <span><i class="pi pi-users"></i> {{ ev.registered_count }} inscrits
                    @if (ev.max_capacity) { / {{ ev.max_capacity }} }</span>
                  @if (ev.attended_count > 0) { <span>{{ ev.attended_count }} présents</span> }
                </div>
              </div>
              <div class="px-4 pb-3 flex gap-2 border-t border-gray-50 pt-3">
                <p-button icon="pi pi-users" size="small" severity="secondary" [text]="true"
                  [pTooltip]="'events.registrations' | translate" (onClick)="openRegistrations(ev)" />
                @if (ev.status === 'draft') {
                  <p-button icon="pi pi-send" size="small" severity="info" [text]="true"
                    [pTooltip]="'events.publish' | translate" (onClick)="publish(ev)" />
                  <p-button icon="pi pi-pencil" size="small" severity="secondary" [text]="true"
                    [pTooltip]="'common.edit' | translate" (onClick)="openForm(ev)" />
                }
                @if (ev.status === 'published') {
                  <p-button icon="pi pi-check-circle" size="small" severity="success" [text]="true"
                    pTooltip="Terminer" (onClick)="complete(ev)" />
                  <p-button icon="pi pi-pencil" size="small" severity="secondary" [text]="true"
                    [pTooltip]="'common.edit' | translate" (onClick)="openForm(ev)" />
                  <p-button icon="pi pi-times-circle" size="small" severity="danger" [text]="true"
                    pTooltip="Annuler" (onClick)="cancelEvent(ev)" />
                }
              </div>
            </div>
          }
        </div>

        <!-- Pagination -->
        @if (store.pagination().total > store.pagination().limit) {
          <div class="flex justify-center gap-2 mt-4">
            <p-button icon="pi pi-chevron-left" severity="secondary" [text]="true"
              [disabled]="store.pagination().page <= 1"
              (onClick)="goToPage(store.pagination().page - 1)" />
            <span class="px-3 py-1 text-sm text-gray-600">
              Page {{ store.pagination().page }} / {{ totalPages() }}
            </span>
            <p-button icon="pi pi-chevron-right" severity="secondary" [text]="true"
              [disabled]="store.pagination().page >= totalPages()"
              (onClick)="goToPage(store.pagination().page + 1)" />
          </div>
        }
      }
    </div>

    <app-event-form-drawer #formDrawer (saved)="onSaved($event)" />
    <app-event-registrations-drawer #regsDrawer />
  `,
})
export class EventListPage implements OnInit {
  protected readonly store = inject(EventsStore);
  private readonly api = inject(EventsApiService);

  private readonly formDrawer = viewChild.required<EventFormDrawerComponent>('formDrawer');
  private readonly regsDrawer = viewChild.required<EventRegistrationsDrawerComponent>('regsDrawer');

  protected filterStatus: string | null = null;
  protected filterType: string | null = null;

  protected readonly statusOptions = [...EVENT_STATUSES].map(s => ({ label: s.charAt(0).toUpperCase() + s.slice(1), value: s }));
  protected readonly typeOptions = [...EVENT_TYPES].map(t => ({ label: t.charAt(0).toUpperCase() + t.slice(1), value: t }));

  ngOnInit(): void {
    this.store.loadEvents();
    this.store.loadStats();
  }

  protected totalPages(): number {
    const p = this.store.pagination();
    return Math.ceil(p.total / p.limit);
  }

  protected statusSeverity(status: string): 'success' | 'warn' | 'danger' | 'secondary' | 'info' {
    return status === 'published' ? 'info' : status === 'completed' ? 'success' : status === 'cancelled' ? 'danger' : 'secondary';
  }

  protected openForm(ev?: EventDto): void { this.formDrawer().open(ev); }
  protected openRegistrations(ev: EventDto): void { this.regsDrawer().open(ev); }

  protected applyFilters(): void {
    this.store.loadEvents({ page: 1, status: this.filterStatus ?? undefined, type: this.filterType ?? undefined });
  }

  protected goToPage(page: number): void { this.store.loadEvents({ page }); }

  protected onSaved(ev: EventDto): void { this.store.upsertEvent(ev); this.store.loadStats(); }

  protected publish(ev: EventDto): void {
    this.api.publish(ev.id).subscribe({ next: updated => this.store.upsertEvent(updated) });
  }

  protected complete(ev: EventDto): void {
    this.api.complete(ev.id).subscribe({ next: updated => this.store.upsertEvent(updated) });
  }

  protected cancelEvent(ev: EventDto): void {
    this.api.cancelEvent(ev.id).subscribe({ next: updated => this.store.upsertEvent(updated) });
  }
}
