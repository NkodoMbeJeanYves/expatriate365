import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { EventDto, EventRegistrationDto } from '@models/event.model';
import { EventsApiService } from '../../services/events-api.service';
import { EventFormDrawerComponent } from '../../components/event-form-drawer/event-form-drawer.component';
import { EventRegistrationsDrawerComponent } from '../../components/event-registrations-drawer/event-registrations-drawer.component';
import { AuthStore } from '@core/auth/auth.store';
import { STAFF_ROLES } from '@core/auth/models/role.model';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService, MessageService],
  imports: [
    CommonModule, DatePipe, RouterLink,
    ButtonModule, TagModule, ToastModule, ConfirmDialogModule,
    EventFormDrawerComponent, EventRegistrationsDrawerComponent,
    TranslatePipe,
  ],
  template: `
    <p-toast />
    <p-confirmdialog />

    <div class="p-4 md:p-8 max-w-4xl mx-auto flex flex-col gap-6">

      <!-- Back -->
      <a routerLink="/events/list" class="inline-flex items-center gap-2 text-sm text-gray-500 hover:text-gray-800 transition-colors w-fit">
        <i class="pi pi-arrow-left text-xs"></i> {{ 'events.title' | translate }}
      </a>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-gray-400"></i>
        </div>
      } @else if (event(); as ev) {

        <!-- Header -->
        <div class="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 flex flex-col gap-4">
          <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-2">
                <p-tag [value]="('events.type_' + ev.type) | translate" severity="info" />
                <p-tag [value]="('events.status_' + ev.status) | translate" [severity]="statusSeverity(ev.status)" />
              </div>
              <h1 class="text-2xl font-bold text-gray-900">{{ ev.title }}</h1>
            </div>

            @if (isStaff()) {
              <div class="flex gap-2 flex-wrap">
                @if (ev.status === 'draft') {
                  <p-button [label]="'events.publish' | translate" icon="pi pi-send" (onClick)="publish(ev)" [loading]="acting()" size="small" />
                }
                @if (ev.status === 'published') {
                  <p-button label="Complete" icon="pi pi-check" severity="success" (onClick)="complete(ev)" [loading]="acting()" size="small" />
                  <p-button [label]="'events.cancel_event' | translate" icon="pi pi-times" severity="danger" (onClick)="confirmCancel(ev)" [loading]="acting()" size="small" />
                }
                <p-button [label]="'events.edit_event' | translate" icon="pi pi-pencil" severity="secondary" (onClick)="openEdit(ev)" size="small" />
              </div>
            }
          </div>

          @if (ev.description) {
            <p class="text-gray-600 text-sm leading-relaxed">{{ ev.description }}</p>
          }

          <!-- Meta grid -->
          <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mt-2">
            <div class="flex flex-col gap-1">
              <span class="text-xs text-gray-400 uppercase tracking-wide">{{ 'events.start_date' | translate }}</span>
              <span class="text-sm font-medium text-gray-800">{{ ev.start_date | date:'dd MMM yyyy, HH:mm' }}</span>
            </div>
            <div class="flex flex-col gap-1">
              <span class="text-xs text-gray-400 uppercase tracking-wide">{{ 'events.end_date' | translate }}</span>
              <span class="text-sm font-medium text-gray-800">{{ ev.end_date | date:'dd MMM yyyy, HH:mm' }}</span>
            </div>
            @if (ev.location) {
              <div class="flex flex-col gap-1">
                <span class="text-xs text-gray-400 uppercase tracking-wide">{{ 'events.location' | translate }}</span>
                <span class="text-sm font-medium text-gray-800">{{ ev.location }}</span>
              </div>
            }
            @if (ev.max_capacity) {
              <div class="flex flex-col gap-1">
                <span class="text-xs text-gray-400 uppercase tracking-wide">{{ 'events.capacity' | translate }}</span>
                <span class="text-sm font-medium text-gray-800">{{ ev.registered_count }} / {{ ev.max_capacity }}</span>
              </div>
            }
          </div>
        </div>

        <!-- Stats -->
        <div class="grid grid-cols-3 gap-4">
          <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
            <div class="text-2xl font-bold text-blue-700">{{ ev.registered_count }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'events.registrations' | translate }}</div>
          </div>
          <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
            <div class="text-2xl font-bold text-green-700">{{ ev.attended_count }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'events.checkin' | translate }}</div>
          </div>
          <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 text-center">
            <div class="text-2xl font-bold text-gray-700">
              {{ ev.registered_count > 0 ? ((ev.attended_count / ev.registered_count) * 100 | number:'1.0-0') : 0 }}%
            </div>
            <div class="text-xs text-gray-500 mt-1">Attendance rate</div>
          </div>
        </div>

        <!-- Registrations table -->
        <div class="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
          <div class="flex items-center justify-between p-5 border-b border-gray-100">
            <h2 class="font-semibold text-gray-800">{{ 'events.registrations' | translate }}</h2>
            @if (isStaff() && ev.status === 'published') {
              <p-button label="Manage" icon="pi pi-users" severity="secondary" size="small" (onClick)="openRegistrations(ev)" />
            }
          </div>

          @if (registrations().length === 0) {
            <div class="text-center py-10 text-gray-400 text-sm">No registrations yet</div>
          } @else {
            <div class="overflow-x-auto">
              <table class="w-full text-sm">
                <thead class="bg-gray-50">
                  <tr>
                    <th class="text-left p-3 text-xs text-gray-500 font-medium">Member</th>
                    <th class="text-left p-3 text-xs text-gray-500 font-medium">Number</th>
                    <th class="text-left p-3 text-xs text-gray-500 font-medium">Status</th>
                    <th class="text-left p-3 text-xs text-gray-500 font-medium">Registered</th>
                  </tr>
                </thead>
                <tbody>
                  @for (reg of registrations(); track reg.id) {
                    <tr class="border-t border-gray-50 hover:bg-gray-50 transition-colors">
                      <td class="p-3 font-medium text-gray-800">{{ reg.member_name }}</td>
                      <td class="p-3 text-gray-500">{{ reg.membership_number }}</td>
                      <td class="p-3">
                        <p-tag [value]="reg.status" [severity]="regSeverity(reg.status)" />
                      </td>
                      <td class="p-3 text-gray-500">{{ reg.created_at | date:'dd MMM' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }
    </div>

    <!-- Drawers -->
    <app-event-form-drawer #formDrawer (saved)="reload()" />
    <app-event-registrations-drawer #regDrawer (changed)="reload()" />
  `,
})
export class EventDetailPage implements OnInit {
  private readonly api    = inject(EventsApiService);
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth   = inject(AuthStore);
  private readonly confirm = inject(ConfirmationService);
  private readonly toast   = inject(MessageService);

  event         = signal<EventDto | null>(null);
  registrations = signal<EventRegistrationDto[]>([]);
  loading       = signal(true);
  acting        = signal(false);

  isStaff = computed(() => this.auth.hasAnyRole(STAFF_ROLES));

  formDrawer = viewChild<EventFormDrawerComponent>('formDrawer');
  regDrawer  = viewChild<EventRegistrationsDrawerComponent>('regDrawer');

  private eventId!: string;

  ngOnInit() {
    this.eventId = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load() {
    this.loading.set(true);
    this.api.getById(this.eventId).subscribe({
      next: ({ event, registrations }) => {
        this.event.set(event);
        this.registrations.set(registrations);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.router.navigate(['/events/list']);
      },
    });
  }

  reload() { this.load(); }

  openEdit(ev: EventDto) { this.formDrawer()?.open(ev); }
  openRegistrations(ev: EventDto) { this.regDrawer()?.open(ev); }

  publish(ev: EventDto) {
    this.acting.set(true);
    this.api.publish(ev.id).subscribe({
      next: (updated) => { this.event.set(updated); this.acting.set(false); },
      error: () => this.acting.set(false),
    });
  }

  complete(ev: EventDto) {
    this.acting.set(true);
    this.api.complete(ev.id).subscribe({
      next: (updated) => { this.event.set(updated); this.acting.set(false); },
      error: () => this.acting.set(false),
    });
  }

  confirmCancel(ev: EventDto) {
    this.confirm.confirm({
      message: `Cancel "${ev.title}"?`,
      header: 'Confirm cancellation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.acting.set(true);
        this.api.cancelEvent(ev.id).subscribe({
          next: (updated) => { this.event.set(updated); this.acting.set(false); },
          error: () => this.acting.set(false),
        });
      },
    });
  }

  statusSeverity(status: string): 'info' | 'success' | 'warn' | 'danger' | 'secondary' {
    const map: Record<string, 'info' | 'success' | 'warn' | 'danger' | 'secondary'> = {
      draft: 'secondary', published: 'info', completed: 'success', cancelled: 'danger',
    };
    return map[status] ?? 'secondary';
  }

  regSeverity(status: string): 'info' | 'success' | 'warn' | 'danger' | 'secondary' {
    const map: Record<string, 'info' | 'success' | 'warn' | 'danger' | 'secondary'> = {
      registered: 'info', attended: 'success', no_show: 'warn', cancelled: 'danger',
    };
    return map[status] ?? 'secondary';
  }
}

