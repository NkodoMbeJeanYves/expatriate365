import {
  ChangeDetectionStrategy, Component, computed,
  inject, OnInit, signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
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
    ProgressSpinner, TooltipModule, TranslatePipe,
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
                    <div class="font-medium">{{ ('roles.label_' + role.name) | translate }}</div>
                    <div class="text-xs text-gray-400 mt-0.5">{{ permCount(role) }} permissions</div>
                  </button>
                }
              </div>
            </p-card>
          </div>

          <!-- Permission editor (right panel) -->
          <div class="lg:col-span-3">
            @if (selectedRole(); as role) {
              <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 flex flex-col">

                <!-- Role header -->
                <div class="flex items-center justify-between px-5 py-4 border-b border-gray-100 dark:border-gray-700 flex-shrink-0">
                  <div>
                    <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ ('roles.label_' + role.name) | translate }}</h2>
                    @if (role.description) {
                      <p class="text-sm text-gray-500">{{ role.description }}</p>
                    }
                  </div>
                  <span class="text-xs text-gray-400">{{ selectedCount() }} / {{ totalCount() }}</span>
                </div>

                <!-- Body: domain nav + permissions -->
                <div class="flex min-h-0 flex-1" style="height: 420px;">

                  <!-- Domain sidebar -->
                  <nav class="w-40 flex-shrink-0 border-r border-gray-100 dark:border-gray-700 overflow-y-auto py-2">
                    @for (domain of domains(); track domain.domain) {
                      <button
                        type="button"
                        (click)="selectDomain(domain)"
                        [class]="selectedDomain()?.domain === domain.domain
                          ? 'w-full text-left flex items-center justify-between px-3 py-2 mx-1 rounded-lg text-xs font-medium bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300'
                          : 'w-full text-left flex items-center justify-between px-3 py-2 mx-1 rounded-lg text-xs text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors'">
                        <span>{{ ('roles.domain_' + domain.domain) | translate }}</span>
                        <span [class]="selectedDomain()?.domain === domain.domain
                          ? 'text-[10px] bg-emerald-100 dark:bg-emerald-800 text-emerald-700 dark:text-emerald-300 rounded-full px-1.5 py-0.5 font-medium'
                          : 'text-[10px] bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 rounded-full px-1.5 py-0.5'">
                          {{ domainCheckedCount(domain) }}/{{ domain.permissions.length }}
                        </span>
                      </button>
                    }
                  </nav>

                  <!-- Permissions grid for selected domain -->
                  <div class="flex-1 overflow-y-auto p-5">
                    @if (selectedDomain(); as domain) {
                      <div class="flex items-center justify-between mb-4">
                        <h3 class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                          {{ ('roles.domain_' + domain.domain) | translate }}
                        </h3>
                        <button
                          type="button"
                          class="text-xs text-emerald-600 dark:text-emerald-400 hover:text-emerald-800 transition-colors"
                          (click)="toggleDomain(domain)">
                          {{ domainAllSelected(domain) ? ('roles.deselect_all' | translate) : ('roles.select_all' | translate) }}
                        </button>
                      </div>

                      <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                        @for (perm of domain.permissions; track perm) {
                          <button
                            type="button"
                            (click)="toggle(perm)"
                            [class]="isChecked(perm)
                              ? 'flex items-center gap-3 px-3 py-2.5 rounded-lg border text-left text-sm font-medium transition-all border-emerald-300 dark:border-emerald-700 bg-emerald-50 dark:bg-emerald-900/20 text-emerald-800 dark:text-emerald-300'
                              : 'flex items-center gap-3 px-3 py-2.5 rounded-lg border text-left text-sm transition-all border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:border-gray-300 dark:hover:border-gray-500 hover:bg-gray-50 dark:hover:bg-gray-700'">
                            <span [class]="isChecked(perm)
                              ? 'flex-shrink-0 w-4 h-4 rounded-full bg-emerald-500 flex items-center justify-center'
                              : 'flex-shrink-0 w-4 h-4 rounded-full border-2 border-gray-300 dark:border-gray-600'">
                              @if (isChecked(perm)) {
                                <i class="pi pi-check text-white" style="font-size: 8px;"></i>
                              }
                            </span>
                            {{ ('roles.perm_' + perm.split('.').slice(1).join('_')) | translate }}
                          </button>
                        }
                      </div>
                    }
                  </div>

                </div>

                <!-- Save bar -->
                <div class="flex items-center justify-between px-5 py-4 border-t border-gray-100 dark:border-gray-700 flex-shrink-0">
                  <div class="flex items-center gap-2">
                    @if (role.is_customized) {
                      <span class="inline-flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 px-2 py-1 rounded-full">
                        <i class="pi pi-pencil text-xs"></i> {{ 'roles.customized' | translate }}
                      </span>
                      <p-button
                        [label]="'roles.reset_defaults' | translate"
                        icon="pi pi-refresh"
                        severity="secondary"
                        [text]="true"
                        size="small"
                        [loading]="resetting()"
                        (onClick)="resetDefaults()" />
                    }
                  </div>
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

  readonly loading   = signal(true);
  readonly saving    = signal(false);
  readonly resetting = signal(false);
  readonly roles   = signal<RoleDto[]>([]);
  readonly domains = signal<PermissionDomain[]>([]);

  readonly selectedRole   = signal<RoleDto | null>(null);
  readonly selectedDomain = signal<PermissionDomain | null>(null);

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
    const domains = this.domains();
    if (domains.length) this.selectedDomain.set(domains[0]);
  }

  selectDomain(domain: PermissionDomain): void {
    this.selectedDomain.set(domain);
  }

  private parsePermissions(raw: string): string[] {
    try { return JSON.parse(raw) as string[]; } catch { return []; }
  }

  permCount(role: RoleDto): number {
    if (Array.isArray(role.permissions)) return role.permissions.length;
    return this.parsePermissions(role.permissions as unknown as string).length;
  }

  domainCheckedCount(domain: PermissionDomain): number {
    const checked = this._checked();
    return domain.permissions.filter(p => checked.has(p)).length;
  }

  isChecked(perm: string): boolean {
    return this._checked().has(perm);
  }

  toggle(perm: string): void {
    this._checked.update(set => {
      const next = new Set(set);
      next.has(perm) ? next.delete(perm) : next.add(perm);
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

  resetDefaults(): void {
    const role = this.selectedRole();
    if (!role) return;
    this.resetting.set(true);
    this.api.resetPermissions(role.id).subscribe({
      next: () => {
        this.api.list().subscribe(roles => {
          this.roles.set(roles);
          const refreshed = roles.find(r => r.id === role.id);
          if (refreshed) this.selectRole(refreshed);
        });
        this.resetting.set(false);
        this.toast.success('Permissions réinitialisées aux valeurs par défaut.');
      },
      error: () => {
        this.resetting.set(false);
        this.toast.error('Erreur lors de la réinitialisation.');
      },
    });
  }

  save(): void {
    const role = this.selectedRole();
    if (!role) return;

    this.saving.set(true);
    const permissions = [...this._checked()];

    this.api.updatePermissions(role.id, { permissions }).subscribe({
      next: () => {
        this.roles.update(list =>
          list.map(r => r.id === role.id ? { ...r, permissions, is_customized: true } : r)
        );
        this.selectedRole.set({ ...role, permissions, is_customized: true });
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
