import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, signal, viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { SuperAdminApiService } from '../../services/super-admin-api.service';
import { ToastService } from '@service/toast.service';
import { AuthService } from '@core/auth/auth.service';
import { TenantSummaryDto } from '@models/admin.model';

@Component({
  selector: 'app-admin-tenants',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe, ReactiveFormsModule, ButtonModule, InputTextModule, PasswordModule,
    DrawerModule, TagModule, ProgressSpinnerModule, TooltipModule,
    TranslatePipe,
  ],
  template: `
    <div class="p-6 max-w-7xl mx-auto">

      <!-- Header -->
      <div class="flex items-start justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
            {{ 'tenants.title' | translate }}
          </h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ 'tenants.subtitle' | translate }}</p>
        </div>
        <p-button
          [label]="'tenants.new' | translate"
          icon="pi pi-plus"
          (onClick)="openForm()" />
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <p-progressspinner strokeWidth="4" styleClass="w-12 h-12" />
        </div>
      } @else {
        <div class="flex items-center gap-4 mb-4">
          <span class="text-sm text-gray-500">
            {{ tenants().length }} {{ 'tenants.count' | translate }}
          </span>
        </div>

        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
              <tr>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'tenants.col_name' | translate }}</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'tenants.col_slug' | translate }}</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'tenants.col_admin' | translate }}</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'tenants.col_users' | translate }}</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'tenants.col_status' | translate }}</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">{{ 'common.created_at' | translate }}</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
              @for (t of tenants(); track t.id) {
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                  <td class="px-4 py-3">
                    <div class="font-medium text-gray-900 dark:text-white">{{ t.name }}</div>
                    <div class="text-xs text-gray-400">{{ t.country_code }} · {{ t.base_currency }}</div>
                  </td>
                  <td class="px-4 py-3">
                    <code class="text-xs bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded text-gray-600 dark:text-gray-300">{{ t.slug }}</code>
                  </td>
                  <td class="px-4 py-3">
                    <div class="text-gray-700 dark:text-gray-300">{{ t.admin_full_name }}</div>
                    <div class="text-xs text-gray-400">{{ t.admin_email }}</div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ t.user_count }}</td>
                  <td class="px-4 py-3">
                    <p-tag
                      [value]="t.is_active ? ('common.active' | translate) : ('common.inactive' | translate)"
                      [severity]="t.is_active ? 'success' : 'danger'" />
                  </td>
                  <td class="px-4 py-3 text-xs text-gray-400">{{ t.created_at | date:'dd/MM/yyyy' }}</td>
                  <td class="px-4 py-3">
                    <p-button
                      [label]="'tenants.manage' | translate"
                      icon="pi pi-arrow-right"
                      iconPos="right"
                      severity="secondary"
                      size="small"
                      [loading]="enteringId() === t.id"
                      [disabled]="!!enteringId()"
                      (onClick)="enterTenant(t)" />
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7" class="text-center py-16 text-gray-400">
                    <i class="pi pi-building text-4xl mb-3 block"></i>
                    <p class="mb-4">{{ 'tenants.empty' | translate }}</p>
                    <p-button [label]="'tenants.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Drawer: create association -->
    <p-drawer #drawerEl [visible]="showForm()" [header]="'tenants.new' | translate"
      position="right" styleClass="!w-full md:!w-[520px]"
      (visibleChange)="showForm.set($event)">
      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 p-2">

        <div class="text-xs font-semibold text-gray-400 uppercase tracking-wider mt-2">
          {{ 'tenants.section_association' | translate }}
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'tenants.association_name' | translate }} *</label>
          <input pInputText formControlName="association_name"
            [placeholder]="'tenants.association_name_ph' | translate" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'tenants.slug' | translate }} *</label>
          <input pInputText formControlName="slug" [placeholder]="'tenants.slug_ph' | translate" />
          <p class="text-xs text-gray-400">{{ 'tenants.slug_hint' | translate }}</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'tenants.country_code' | translate }}</label>
            <input pInputText formControlName="country_code" placeholder="MU" maxlength="2" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'tenants.currency' | translate }}</label>
            <input pInputText formControlName="base_currency" placeholder="MUR" maxlength="3" />
          </div>
        </div>

        <div class="text-xs font-semibold text-gray-400 uppercase tracking-wider mt-2">
          {{ 'tenants.section_admin' | translate }}
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'common.first_name' | translate }} *</label>
            <input pInputText formControlName="admin_first_name" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'common.last_name' | translate }} *</label>
            <input pInputText formControlName="admin_last_name" />
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.email' | translate }} *</label>
          <input pInputText formControlName="admin_email" type="email" />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.password' | translate }} *</label>
          <p-password formControlName="admin_password" [feedback]="false" [toggleMask]="true"
            styleClass="w-full" inputStyleClass="w-full" />
        </div>

        @if (error()) {
          <p class="text-red-500 text-sm">{{ error() }}</p>
        }

        <div class="flex justify-end gap-2 pt-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate"
            (onClick)="drawerRef()?.close($event)" />
          <p-button type="submit" [label]="'tenants.create' | translate"
            [loading]="saving()" [disabled]="form.invalid" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class AdminTenantsPage implements OnInit {
  private readonly api    = inject(SuperAdminApiService);
  private readonly auth   = inject(AuthService);
  private readonly toast  = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  protected readonly drawerRef = viewChild<Drawer>('drawerEl');

  readonly loading   = signal(true);
  readonly saving    = signal(false);
  readonly showForm  = signal(false);
  readonly tenants   = signal<TenantSummaryDto[]>([]);
  readonly error     = signal<string | null>(null);
  readonly enteringId = signal<string | null>(null);

  readonly form = this.fb.group({
    association_name: ['', Validators.required],
    slug:             ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    country_code:     ['MU'],
    base_currency:    ['MUR'],
    admin_first_name: ['', Validators.required],
    admin_last_name:  ['', Validators.required],
    admin_email:      ['', [Validators.required, Validators.email]],
    admin_password:   ['', [Validators.required, Validators.minLength(8)]],
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.listTenants().subscribe({
      next: t  => { this.tenants.set(t); this.loading.set(false); },
      error: () => { this.toast.error('Erreur de chargement.'); this.loading.set(false); },
    });
  }

  openForm(): void {
    this.form.reset({ country_code: 'MU', base_currency: 'MUR' });
    this.error.set(null);
    this.showForm.set(true);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.value;

    this.api.createTenant({
      association_name: v.association_name!,
      slug:             v.slug!,
      country_code:     v.country_code || 'MU',
      base_currency:    v.base_currency || 'MUR',
      admin_first_name: v.admin_first_name!,
      admin_last_name:  v.admin_last_name!,
      admin_email:      v.admin_email!,
      admin_password:   v.admin_password!,
    }).subscribe({
      next: tenant => {
        this.tenants.update(list => [tenant, ...list]);
        this.saving.set(false);
        this.drawerRef()?.close(new MouseEvent('click'));
        this.toast.success(`Association "${tenant.name}" créée.`);
      },
      error: (err: any) => {
        this.error.set(err?.error?.error ?? 'Erreur lors de la création.');
        this.saving.set(false);
      },
    });
  }

  enterTenant(t: TenantSummaryDto): void {
    this.enteringId.set(t.id);
    this.auth.selectTenant(t.id).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: (err: any) => {
        this.toast.error(err?.error?.error ?? 'Impossible d\'entrer dans cette association.');
        this.enteringId.set(null);
      },
    });
  }
}
