import {
  ChangeDetectionStrategy, Component, computed, inject, OnInit, signal,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { AppPaginatorComponent, PageChangeEvent } from '@shared/components/paginator/app-paginator.component';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { MembersStore } from '../../store/members.store';
import { MembersApiService } from '../../services/members-api.service';
import { MemberFormDrawerComponent } from '../../components/member-form-drawer/member-form-drawer.component';
import { MemberStatus } from '@core/models/member.model';
import { MemberStatusBadgeComponent } from '../../components/member-status-badge/member-status-badge.component';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthStore } from '@core/auth/auth.store';
import { PERMISSIONS } from '@core/auth/models/permission.model';
import { ToastService } from '@service/toast.service';

@Component({
  selector: 'app-member-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, ReactiveFormsModule, RouterLink,
    ButtonModule, InputTextModule, SelectModule, AppPaginatorComponent, TagModule,
    SkeletonModule, TooltipModule, AvatarModule,
    MemberFormDrawerComponent, MemberStatusBadgeComponent, TranslatePipe,
  ],
  template: `
    <div class="flex flex-col gap-4">

      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 class="text-xl font-medium text-gray-900 dark:text-white">{{ 'members.title' | translate }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ store.pagination().total }} membre{{ store.pagination().total > 1 ? 's' : '' }}
          </p>
        </div>
        <div class="flex gap-2">
          <p-button
            icon="pi pi-download"
            [label]="'common.export' | translate"
            severity="secondary"
            [outlined]="true"
            size="small"
            (click)="exportCsv()"
            class="hidden sm:inline-flex" />
          @if (isMemberAdmin()) {
            <p-button
              icon="pi pi-plus"
              [label]="'members.new' | translate"
              size="small"
              (click)="openDrawer()" />
          }
        </div>
      </div>

      <!-- Filtres -->
      <div class="flex flex-col sm:flex-row gap-3">
        <input
          pInputText
          [formControl]="searchCtrl"
          [placeholder]="'common.search' | translate"
          class="flex-1 w-full" />
        <p-select
          [options]="statusOptions"
          optionLabel="label"
          optionValue="value"
          [placeholder]="'common.all' | translate"
          [ngModel]="selectedStatus()"
          (ngModelChange)="onStatusChange($event)"
          styleClass="w-full sm:w-44" />
        <p-select
          [options]="store.categories()"
          optionLabel="name"
          optionValue="id"
          [placeholder]="'common.all' | translate"
          [ngModel]="selectedCategory()"
          (ngModelChange)="onCategoryChange($event)"
          styleClass="w-full sm:w-48" />
      </div>

      <!-- Table desktop / cards mobile -->
      <!-- Desktop table -->
      <div class="hidden sm:block overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            <tr>
              <th class="text-left px-4 py-3">{{ 'common.member' | translate }}</th>
              <th class="text-left px-4 py-3 hidden lg:table-cell">{{ 'members.email' | translate }}</th>
              <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
              <th class="text-left px-4 py-3 hidden md:table-cell">{{ 'members.category' | translate }}</th>
              <th class="text-left px-4 py-3 hidden lg:table-cell">{{ 'members.joined_date' | translate }}</th>
              <th class="px-4 py-3" style="width:100px"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-700 bg-white dark:bg-gray-900">
            @if (store.loading()) {
              @for (i of [1,2,3,4,5]; track i) {
                <tr>
                  <td colspan="6" class="px-4 py-3">
                    <div class="h-8 bg-gray-100 dark:bg-gray-700 rounded animate-pulse"></div>
                  </td>
                </tr>
              }
            } @else if (!store.hasMembers()) {
              <tr>
                <td colspan="6" class="text-center px-4 py-12 text-gray-400">
                  {{ 'members.no_members' | translate }}
                </td>
              </tr>
            } @else {
              @for (m of store.members(); track m.id) {
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                  <td class="px-4 py-3">
                    <div class="flex items-center gap-2">
                      <p-avatar [label]="initials(m)" shape="circle" size="normal" styleClass="text-xs" />
                      <div>
                        <div class="font-medium text-gray-900 dark:text-white">{{ m.first_name }} {{ m.last_name }}</div>
                        <div class="text-xs text-gray-400">{{ m.membership_number }}</div>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 hidden lg:table-cell text-gray-600 dark:text-gray-300">{{ m.email }}</td>
                  <td class="px-4 py-3"><app-member-status-badge [status]="m.status" /></td>
                  <td class="px-4 py-3 hidden md:table-cell text-gray-600 dark:text-gray-300">{{ m.category_name ?? '—' }}</td>
                  <td class="px-4 py-3 hidden lg:table-cell text-gray-600 dark:text-gray-300">{{ m.joined_date }}</td>
                  <td class="px-4 py-3">
                    <div class="flex gap-1">
                      <p-button icon="pi pi-eye" severity="secondary" [text]="true" size="small"
                        [routerLink]="['/members', m.id]" [pTooltip]="'common.view' | translate" />
                      @if (isSuperAdmin() && m.status === 'pending') {
                        <p-button icon="pi pi-send" severity="info" [text]="true" size="small"
                          [loading]="activating() === m.id"
                          (click)="sendActivation(m.id)" [pTooltip]="'members.send_activation' | translate" />
                      }
                      @if (isMemberAdmin()) {
                        <p-button icon="pi pi-pencil" severity="secondary" [text]="true" size="small"
                          (click)="openDrawer(m.id)" [pTooltip]="'common.edit' | translate" />
                      }
                    </div>
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
        <app-paginator [page]="store.pagination().page" [limit]="store.pagination().limit" [total]="store.pagination().total" (pageChange)="onPageChange($event)" />
      </div>

        <!-- Mobile cards -->
        <div class="flex flex-col gap-3 sm:hidden">
          @for (m of store.members(); track m.id) {
            <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
              <div class="flex items-start justify-between gap-2">
                <div class="flex items-center gap-3">
                  <p-avatar [label]="initials(m)" shape="circle" size="large" />
                  <div>
                    <div class="font-medium">{{ m.first_name }} {{ m.last_name }}</div>
                    <div class="text-xs text-gray-400">{{ m.membership_number }}</div>
                    <div class="text-xs text-gray-500 mt-0.5">{{ m.email }}</div>
                  </div>
                </div>
                <app-member-status-badge [status]="m.status" />
              </div>
              <div class="flex items-center justify-between mt-3 pt-3 border-t border-gray-100 dark:border-gray-700">
                <span class="text-xs text-gray-400">{{ m.category_name ?? ('common.none' | translate) }}</span>
                <div class="flex gap-1">
                  <p-button icon="pi pi-eye" severity="secondary" [text]="true" size="small" [routerLink]="['/members', m.id]" />
                  @if (isMemberAdmin()) {
                    <p-button icon="pi pi-pencil" severity="secondary" [text]="true" size="small" (click)="openDrawer(m.id)" />
                  }
                </div>
              </div>
            </div>
          }
          @if (!store.hasMembers()) {
            <div class="text-center py-12 text-gray-400">{{ 'members.no_members' | translate }}</div>
          }

          <!-- Mobile pagination -->
          @if (store.pagination().total > store.pagination().limit) {
            <app-paginator [page]="store.pagination().page" [limit]="store.pagination().limit" [total]="store.pagination().total" (pageChange)="onPageChange($event)" />
          }
        </div>

      <!-- Mobile FAB -->
      <div class="fixed bottom-6 right-6 sm:hidden flex flex-col gap-2">
        <p-button icon="pi pi-download" severity="secondary" [rounded]="true" (click)="exportCsv()" [pTooltip]="'common.export' | translate" />
        @if (isMemberAdmin()) {
          <p-button icon="pi pi-plus" [rounded]="true" (click)="openDrawer()" />
        }
      </div>

    </div>

    <app-member-form-drawer
      [(visible)]="drawerVisible"
      [memberId]="editingMemberId()"
      (saved)="onSaved()" />
  `,
})
export class MemberListPageComponent implements OnInit {
  readonly store = inject(MembersStore);
  private readonly api = inject(MembersApiService);
  private readonly translate = inject(TranslateService);
  private readonly authStore = inject(AuthStore);
  private readonly toast = inject(ToastService);

  readonly isMemberAdmin = computed(() => this.authStore.hasPermission(PERMISSIONS.MEMBERS_CREATE));
  readonly isSuperAdmin  = computed(() => this.authStore.hasPermission(PERMISSIONS.MEMBERS_SEND_ACTIVATION));

  readonly activating = signal<string | null>(null);

  drawerVisible = false;
  readonly editingMemberId = signal<string | null>(null);
  readonly selectedStatus = signal<string>('');
  readonly selectedCategory = signal<string>('');

  readonly searchCtrl = new FormControl('');

  get statusOptions() {
    return [
      { label: this.translate.instant('common.all'), value: '' },
      { label: this.translate.instant('members.status_active'), value: 'active' },
      { label: this.translate.instant('members.status_suspended'), value: 'suspended' },
      { label: this.translate.instant('members.status_inactive'), value: 'inactive' },
      { label: this.translate.instant('members.status_pending'), value: 'pending' },
    ];
  }

  ngOnInit(): void {
    this.store.loadCategories();
    this.store.loadMembers({ page: 1, limit: 20 });

    this.searchCtrl.valueChanges.pipe(
      debounceTime(350),
      distinctUntilChanged(),
    ).subscribe((search) => {
      this.store.setFilters({ search: search ?? undefined });
      this.store.loadMembers(this.store.filters());
    });
  }

  onPageChange(event: PageChangeEvent): void {
    this.store.setFilters({ page: event.page, limit: event.limit });
    this.store.loadMembers(this.store.filters());
  }

  onStatusChange(status: string): void {
    this.selectedStatus.set(status);
    this.store.setFilters({ status: status as MemberStatus | '' });
    this.store.loadMembers(this.store.filters());
  }

  onCategoryChange(id: string): void {
    this.selectedCategory.set(id);
    this.store.setFilters({ category_id: id || undefined });
    this.store.loadMembers(this.store.filters());
  }

  openDrawer(id?: string): void {
    this.editingMemberId.set(id ?? null);
    this.drawerVisible = true;
  }

  onSaved(): void {
    this.drawerVisible = false;
    this.store.loadMembers(this.store.filters());
  }

  exportCsv(): void {
    this.api.exportCsv(this.selectedStatus() || undefined);
  }

  sendActivation(id: string): void {
    this.activating.set(id);
    this.api.sendActivation(id).subscribe({
      next: () => {
        this.activating.set(null);
        this.toast.success('Email d\'activation envoyé.');
        this.store.loadMembers(this.store.filters());
      },
      error: () => this.activating.set(null),
    });
  }

  initials(m: { first_name: string; last_name: string }): string {
    return `${m.first_name?.[0] ?? ''}${m.last_name?.[0] ?? ''}`.toUpperCase();
  }
}
