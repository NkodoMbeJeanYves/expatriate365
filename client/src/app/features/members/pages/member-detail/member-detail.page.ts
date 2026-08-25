import {
  ChangeDetectionStrategy, Component, computed, inject, OnInit, signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { SlicePipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { AvatarModule } from 'primeng/avatar';
import { SkeletonModule } from 'primeng/skeleton';
import { Member } from '@core/models/member.model';
import { AuthStore } from '@core/auth/auth.store';
import { MembersApiService } from '../../services/members-api.service';
import { MemberStatusBadgeComponent } from '../../components/member-status-badge/member-status-badge.component';
import { MemberFormDrawerComponent } from '../../components/member-form-drawer/member-form-drawer.component';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-member-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    SlicePipe, RouterLink, ButtonModule, CardModule, AvatarModule, SkeletonModule,
    MemberStatusBadgeComponent, MemberFormDrawerComponent, TranslatePipe,
  ],
  template: `
    <div class="flex flex-col gap-4 max-w-4xl mx-auto">

      <!-- Back -->
      <p-button
        icon="pi pi-arrow-left"
        [label]="'common.back' | translate"
        severity="secondary"
        [text]="true"
        routerLink="/members"
        styleClass="-ml-2" />

      @if (loading()) {
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <p-skeleton height="200px" />
          <p-skeleton height="200px" styleClass="md:col-span-2" />
        </div>
      } @else if (member()) {
        <!-- Header card -->
        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
          <div class="flex flex-col sm:flex-row items-start sm:items-center gap-4">
            <p-avatar
              [label]="initials()"
              shape="circle"
              size="xlarge"
              styleClass="text-xl min-w-16" />
            <div class="flex-1">
              <h1 class="text-2xl font-medium">{{ member()!.first_name }} {{ member()!.last_name }}</h1>
              <div class="text-gray-400 text-sm">{{ member()!.membership_number }}</div>
              <div class="flex flex-wrap items-center gap-2 mt-2">
                <app-member-status-badge [status]="member()!.status" />
                @if (member()!.category_name) {
                  <span class="text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded-full">
                    {{ member()!.category_name }}
                  </span>
                }
              </div>
            </div>
            @if (canEdit()) {
              <p-button
                icon="pi pi-pencil"
                [label]="'common.edit' | translate"
                (click)="drawerVisible = true"
                class="hidden sm:inline-flex" />
            }
          </div>
        </div>

        <!-- Info grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
            <h3 class="text-sm font-medium text-gray-500 uppercase tracking-wide mb-4">Informations personnelles</h3>
            <dl class="space-y-3">
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.email' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.email }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.phone' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.phone ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.date_of_birth' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.date_of_birth ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.gender' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.gender ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.profession' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.profession ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.address' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.address ?? '—' }}</dd>
              </div>
            </dl>
          </div>

          <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
            <h3 class="text-sm font-medium text-gray-500 uppercase tracking-wide mb-4">Adhésion</h3>
            <dl class="space-y-3">
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.joined_date' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.joined_date }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">{{ 'members.expiry_date' | translate }}</dt>
                <dd class="text-sm font-medium">{{ member()!.expiry_date ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">Membre depuis</dt>
                <dd class="text-sm font-medium">{{ member()!.created_at | slice:0:10 }}</dd>
              </div>
            </dl>

            <h3 class="text-sm font-medium text-gray-500 uppercase tracking-wide mb-4 mt-6">Contact d'urgence</h3>
            <dl class="space-y-3">
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">Nom</dt>
                <dd class="text-sm font-medium">{{ member()!.emergency_contact_name ?? '—' }}</dd>
              </div>
              <div class="flex flex-col sm:flex-row sm:justify-between gap-1">
                <dt class="text-sm text-gray-400">Téléphone</dt>
                <dd class="text-sm font-medium">{{ member()!.emergency_contact_phone ?? '—' }}</dd>
              </div>
            </dl>
          </div>

        </div>
      } @else {
        <div class="text-center py-12 text-gray-400">{{ 'common.no_data' | translate }}</div>
      }

      @if (canEdit()) {
        <!-- Mobile FAB edit -->
        @if (member()) {
          <div class="fixed bottom-6 right-6 sm:hidden">
            <p-button icon="pi pi-pencil" [rounded]="true" (click)="drawerVisible = true" />
          </div>
        }

        <app-member-form-drawer
          [(visible)]="drawerVisible"
          [memberId]="memberId"
          (saved)="reload()" />
      }
    </div>
  `,
})
export class MemberDetailPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api   = inject(MembersApiService);
  private readonly auth  = inject(AuthStore);

  readonly member  = signal<Member | null>(null);
  readonly loading = signal(true);
  readonly canEdit = computed(() => this.auth.hasPermission('members.update'));

  drawerVisible = false;
  memberId!: string;

  ngOnInit(): void {
    this.memberId = this.route.snapshot.paramMap.get('id')!;
    this.loadMember();
  }

  loadMember(): void {
    this.loading.set(true);
    this.api.getById(this.memberId).subscribe({
      next: (m) => { this.member.set(m); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  reload(): void {
    this.drawerVisible = false;
    this.loadMember();
  }

  initials(): string {
    const m = this.member();
    return m ? `${m.first_name[0] ?? ''}${m.last_name[0] ?? ''}`.toUpperCase() : '';
  }
}
