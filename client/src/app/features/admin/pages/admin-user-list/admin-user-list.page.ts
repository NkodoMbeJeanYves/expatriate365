import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { ROLES } from '@core/auth/models/role.model';
import { AdminUserDto } from '@models/admin.model';
import { AdminStore } from '../../store/admin.store';
import { AdminApiService } from '../../services/admin-api.service';

@Component({
  selector: 'app-admin-user-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    DatePipe, FormsModule, ReactiveFormsModule, ButtonModule, TagModule, SelectModule,
    DrawerModule, InputTextModule, ProgressSpinnerModule, TooltipModule, ConfirmDialog, TranslatePipe,
  ],
  template: `
    <p-confirmdialog />

    <div class="p-6 flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'admin.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'admin.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'admin.invite_user' | translate" icon="pi pi-user-plus" (onClick)="showInvite.set(true)" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-3 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total_users }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'admin.users' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ s.active_users }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'admin.status_active' | translate }}</div>
          </div>
          <div class="bg-red-50 rounded-xl p-4 border border-red-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-red-600">{{ s.inactive_users }}</div>
            <div class="text-xs text-red-500 mt-1">{{ 'common.inactive_label' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="roleOptions" [(ngModel)]="filterRole" optionLabel="label" optionValue="value"
          placeholder="Tous les rôles" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          placeholder="Tous les statuts" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Liste -->
      @if (store.loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.users().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-users text-4xl mb-3 block"></i>
          <p>{{ 'admin.no_users' | translate }}</p>
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-gray-100 shadow-sm overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 text-gray-500 text-xs uppercase">
              <tr>
                <th class="px-4 py-3 text-left">{{ 'admin.users' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'admin.user_role' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'common.status' | translate }}</th>
                <th class="px-4 py-3 text-left">{{ 'admin.last_login' | translate }}</th>
                <th class="px-4 py-3 text-right">{{ 'common.actions' | translate }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-50">
              @for (user of store.users(); track user.id) {
                <tr class="hover:bg-gray-50 transition-colors">
                  <td class="px-4 py-3">
                    <div class="font-medium text-gray-800">{{ user.full_name }}</div>
                    <div class="text-xs text-gray-400">{{ user.email }}</div>
                  </td>
                  <td class="px-4 py-3">
                    <p-tag [value]="user.role" severity="info" />
                  </td>
                  <td class="px-4 py-3">
                    <p-tag [value]="user.status"
                      [severity]="user.status === 'active' ? 'success' : user.status === 'pending' ? 'warn' : 'danger'" />
                  </td>
                  <td class="px-4 py-3 text-gray-400 text-xs">
                    {{ user.last_login_at ? (user.last_login_at | date:'dd/MM/yyyy HH:mm') : '–' }}
                  </td>
                  <td class="px-4 py-3">
                    <div class="flex items-center justify-end gap-1">
                      <p-button size="small" icon="pi pi-shield" [pTooltip]="'admin.change_role' | translate"
                        severity="secondary" (onClick)="openRoleEditor(user)" />
                      @if (user.is_active) {
                        <p-button size="small" icon="pi pi-ban" [pTooltip]="'admin.suspend_user' | translate"
                          severity="warn" (onClick)="confirmToggle(user, false)" />
                      } @else {
                        <p-button size="small" icon="pi pi-check-circle" [pTooltip]="'admin.activate_user' | translate"
                          severity="success" (onClick)="confirmToggle(user, true)" />
                      }
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Drawer: inviter -->
    <p-drawer #inviteDrawer [visible]="showInvite()" [header]="'admin.invite_user' | translate"
      position="right" styleClass="w-full sm:w-[480px]"
      (visibleChange)="showInvite.set($event)">
      <div class="flex flex-col h-full">
        <div class="flex-1 overflow-y-auto">
          <form [formGroup]="inviteForm" (ngSubmit)="submitInvite()" class="flex flex-col gap-4 p-2">
            <div class="grid grid-cols-2 gap-4">
              <div class="flex flex-col gap-1">
                <label class="text-sm font-medium">{{ 'members.first_name' | translate }}</label>
                <input pInputText formControlName="first_name" />
              </div>
              <div class="flex flex-col gap-1">
                <label class="text-sm font-medium">{{ 'members.last_name' | translate }}</label>
                <input pInputText formControlName="last_name" />
              </div>
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ 'admin.user_email' | translate }}</label>
              <input pInputText type="email" formControlName="email" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ 'members.phone' | translate }}</label>
              <input pInputText formControlName="phone" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ 'admin.user_role' | translate }}</label>
              <p-select formControlName="role" [options]="roleOptions" optionLabel="label" optionValue="value" appendTo="body" />
            </div>
            <p class="text-xs text-gray-400">
              L'utilisateur recevra ses identifiants pour se connecter.
            </p>
            @if (inviteError()) { <p class="text-red-500 text-sm">{{ inviteError() }}</p> }
          </form>
        </div>
        <div class="flex gap-3 p-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <button type="button" (click)="inviteDrawerRef()?.close($event)"
            class="flex-1 px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl text-sm font-medium
                   text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">
            {{ 'common.cancel' | translate }}
          </button>
          <button type="button" (click)="submitInvite()" [disabled]="inviteForm.invalid || inviteSaving()"
            class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
            {{ inviteSaving() ? ('common.loading' | translate) : ('admin.invite_user' | translate) }}
          </button>
        </div>
      </div>
    </p-drawer>

    <!-- Drawer: changer rôle -->
    <p-drawer #roleDrawer [visible]="showRoleEditor()" [header]="'Rôle — ' + (editingUser()?.full_name ?? '')"
      position="right" styleClass="w-full sm:w-[360px]"
      (visibleChange)="showRoleEditor.set($event)">
      <div class="flex flex-col h-full">
        <div class="flex-1 overflow-y-auto">
          <div class="flex flex-col gap-4 p-2">
            <p-select [options]="roleOptions" [(ngModel)]="selectedRole"
              optionLabel="label" optionValue="value" placeholder="Choisir un rôle" appendTo="body" />
          </div>
        </div>
        <div class="flex gap-3 p-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <button type="button" (click)="roleDrawerRef()?.close($event)"
            class="flex-1 px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl text-sm font-medium
                   text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700">
            {{ 'common.cancel' | translate }}
          </button>
          <button type="button" (click)="submitRole()" [disabled]="roleSaving()"
            class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
            {{ roleSaving() ? ('common.loading' | translate) : ('common.save' | translate) }}
          </button>
        </div>
      </div>
    </p-drawer>
  `,
})
export class AdminUserListPage implements OnInit {
  protected readonly store = inject(AdminStore);
  private readonly api = inject(AdminApiService);
  private readonly confirm = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);

  readonly inviteDrawerRef = viewChild<Drawer>('inviteDrawer');
  readonly roleDrawerRef   = viewChild<Drawer>('roleDrawer');

  readonly showInvite = signal(false);
  readonly inviteSaving = signal(false);
  readonly inviteError = signal<string | null>(null);

  readonly showRoleEditor = signal(false);
  readonly editingUser = signal<AdminUserDto | null>(null);
  readonly roleSaving = signal(false);
  selectedRole = '';

  filterRole: string | null = null;
  filterStatus: string | null = null;

  readonly roleOptions = Object.values(ROLES).map(r => ({ label: r, value: r }));
  get statusOptions() {
    return [
      { label: this.translate.instant('admin.status_active'),    value: 'active'    },
      { label: this.translate.instant('admin.status_pending'),   value: 'pending'   },
      { label: this.translate.instant('admin.status_suspended'), value: 'suspended' },
    ];
  }

  readonly inviteForm = this.fb.group({
    first_name: ['', Validators.required],
    last_name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    role: ['member', Validators.required],
  });

  ngOnInit(): void {
    this.store.load();
    this.store.loadStats();
  }

  applyFilters(): void {
    this.store.load({
      page: 1,
      role: this.filterRole ?? undefined,
      status: this.filterStatus ?? undefined,
    });
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) return;
    this.inviteSaving.set(true);
    this.inviteError.set(null);
    const v = this.inviteForm.value;
    this.api.invite({
      first_name: v.first_name!,
      last_name: v.last_name!,
      email: v.email!,
      phone: v.phone || undefined,
      role: v.role!,
    }).subscribe({
      next: u => {
        this.store.upsert(u);
        this.store.loadStats();
        this.inviteSaving.set(false);
        this.showInvite.set(false);
        this.inviteForm.reset({ role: 'member' });
      },
      error: () => { this.inviteError.set('Cet email est déjà utilisé.'); this.inviteSaving.set(false); },
    });
  }

  openRoleEditor(user: AdminUserDto): void {
    this.editingUser.set(user);
    this.selectedRole = user.role;
    this.showRoleEditor.set(true);
  }

  submitRole(): void {
    const user = this.editingUser();
    if (!user || !this.selectedRole) return;
    this.roleSaving.set(true);
    this.api.changeRole(user.id, { role: this.selectedRole }).subscribe({
      next: u => {
        this.store.upsert(u);
        this.roleSaving.set(false);
        this.showRoleEditor.set(false);
      },
      error: () => this.roleSaving.set(false),
    });
  }

  confirmToggle(user: AdminUserDto, activate: boolean): void {
    this.confirm.confirm({
      message: activate
        ? `Activer le compte de ${user.full_name} ?`
        : `Désactiver le compte de ${user.full_name} ?`,
      header: 'Confirmation',
      icon: activate ? 'pi pi-check-circle' : 'pi pi-ban',
      accept: () => {
        const req$ = activate ? this.api.activate(user.id) : this.api.deactivate(user.id);
        req$.subscribe(u => {
          this.store.upsert(u);
          this.store.loadStats();
        });
      },
    });
  }
}
