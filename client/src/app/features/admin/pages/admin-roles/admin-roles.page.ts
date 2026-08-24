import {
  ChangeDetectionStrategy, Component, computed,
  inject, OnInit, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { CheckboxModule } from 'primeng/checkbox';
import { ProgressSpinner } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { PermissionDomain, RoleDto } from '@models/admin.model';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { RolesApiService } from '@service/roles-api.service';
import { ToastService } from '@service/toast.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-admin-roles',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, ButtonModule, CardModule,
    CheckboxModule, ProgressSpinner, TooltipModule, TranslatePipe,
    PageHeaderComponent,
  ],
  template: `
    <div class="p-6 max-w-7xl mx-auto">

      <app-page-header
        [title]="'roles.title' | translate"
        [subtitle]="'roles.subtitle' | translate" />

      @if (loading()) {
        <div class="flex justify-center py-20">
          <p-progressspinner strokeWidth="4" styleClass="w-12 h-12" />
        </div>
      } @else {
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-6">

          <!-- Role list (left panel) -->
          <div class="lg:col-span-1">
            <p-card styleClass="h-full">
              <div class="flex flex-col gap-1">
                @for (role of roles(); track role.id) {
                  <button
                    type="button"
                    (click)="selectRole(role)"
                    [class]="selectedRole()?.id === role.id
                      ? 'text-left px-3 py-2 rounded-lg bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300 font-medium text-sm'
                      : 'text-left px-3 py-2 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 text-sm transition-colors'">
                    <div class="font-medium">{{ role.label }}</div>
                    <div class="text-xs text-gray-400 mt-0.5">{{ permCount(role) }} permissions</div>
                  </button>
                }
              </div>
            </p-card>
          </div>

          <!-- Permission editor (right panel) -->
          <div class="lg:col-span-3">
            @if (selectedRole(); as role) {
              <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">

                <!-- Role header -->
                <div class="flex items-center justify-between px-5 py-4 border-b border-gray-100 dark:border-gray-700">
                  <div>
                    <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ role.label }}</h2>
                    @if (role.description) {
                      <p class="text-sm text-gray-500">{{ role.description }}</p>
                    }
                  </div>
                  <span class="text-xs text-gray-400">{{ selectedCount() }} / {{ totalCount() }}</span>
                </div>

                <!-- Permissions body -->
                <div class="flex flex-col gap-6 p-5">
                  @for (domain of domains(); track domain.domain) {
                    <div>
                      <div class="flex items-center justify-between mb-2">
                        <h3 class="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                          {{ ('roles.domain_' + domain.domain) | translate }}
                        </h3>
                        <button
                          type="button"
                          class="text-xs text-emerald-600 hover:text-emerald-800 transition-colors"
                          (click)="toggleDomain(domain)">
                          {{ domainAllSelected(domain) ? ('roles.deselect_all' | translate) : ('roles.select_all' | translate) }}
                        </button>
                      </div>

                      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                        @for (perm of domain.permissions; track perm) {
                          <label class="flex items-center gap-2 cursor-pointer group">
                            <p-checkbox
                              [binary]="true"
                              [ngModel]="isChecked(perm)"
                              (ngModelChange)="toggle(perm, $event)" />
                            <span class="text-sm text-gray-700 dark:text-gray-300 group-hover:text-gray-900 dark:group-hover:text-white">
                              {{ perm.split('.').slice(1).join('.') }}
                            </span>
                          </label>
                        }
                      </div>
                    </div>
                  }
                </div>

                <!-- Save bar -->
                <div class="flex justify-end px-5 py-4 border-t border-gray-100 dark:border-gray-700">
                  <p-button
                    label="{{ 'common.save' | translate }}"
                    icon="pi pi-check"
                    [loading]="saving()"
                    (onClick)="save()" />
                </div>

              </div>
            } @else {
              <div class="flex items-center justify-center h-64 text-gray-400">
                <div class="text-center">
                  <i class="pi pi-shield text-4xl mb-3"></i>
                  <p class="text-sm">{{ 'roles.select_role_hint' | translate }}</p>
                </div>
              </div>
            }
          </div>

        </div>
      }

    </div>
  `,
})
export class AdminRolesPage implements OnInit {
  private readonly api   = inject(RolesApiService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly saving  = signal(false);
  readonly roles   = signal<RoleDto[]>([]);
  readonly domains = signal<PermissionDomain[]>([]);

  readonly selectedRole = signal<RoleDto | null>(null);

  /** Working copy of checked permissions for the selected role */
  private readonly _checked = signal<Set<string>>(new Set());

  readonly selectedCount = computed(() => this._checked().size);
  readonly totalCount    = computed(() =>
    this.domains().reduce((n, d) => n + d.permissions.length, 0)
  );

  ngOnInit(): void {
    forkJoin({
      roles:   this.api.list(),
      domains: this.api.listPermissions(),
    }).subscribe({
      next: ({ roles, domains }) => {
        this.roles.set(roles);
        this.domains.set(domains);
        if (roles.length) this.selectRole(roles[0]);
        this.loading.set(false);
      },
      error: () => {
        this.toast.error('Erreur lors du chargement des rôles.');
        this.loading.set(false);
      },
    });
  }

  selectRole(role: RoleDto): void {
    this.selectedRole.set(role);
    this._checked.set(new Set(
      Array.isArray(role.permissions)
        ? role.permissions
        : this.parsePermissions(role.permissions as unknown as string)
    ));
  }

  private parsePermissions(raw: string): string[] {
    try { return JSON.parse(raw) as string[]; } catch { return []; }
  }

  permCount(role: RoleDto): number {
    if (Array.isArray(role.permissions)) return role.permissions.length;
    return this.parsePermissions(role.permissions as unknown as string).length;
  }

  isChecked(perm: string): boolean {
    return this._checked().has(perm);
  }

  toggle(perm: string, checked: boolean): void {
    this._checked.update(set => {
      const next = new Set(set);
      checked ? next.add(perm) : next.delete(perm);
      return next;
    });
  }

  domainAllSelected(domain: PermissionDomain): boolean {
    return domain.permissions.every(p => this._checked().has(p));
  }

  toggleDomain(domain: PermissionDomain): void {
    const allSelected = this.domainAllSelected(domain);
    this._checked.update(set => {
      const next = new Set(set);
      domain.permissions.forEach(p => allSelected ? next.delete(p) : next.add(p));
      return next;
    });
  }

  save(): void {
    const role = this.selectedRole();
    if (!role) return;

    this.saving.set(true);
    const permissions = [...this._checked()];

    this.api.updatePermissions(role.id, { permissions }).subscribe({
      next: () => {
        // Update local role object so the counter refreshes
        this.roles.update(list =>
          list.map(r => r.id === role.id ? { ...r, permissions } : r)
        );
        this.selectedRole.set({ ...role, permissions });
        this.saving.set(false);
        this.toast.success('Permissions enregistrées.');
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Erreur lors de la sauvegarde.');
      },
    });
  }
}
