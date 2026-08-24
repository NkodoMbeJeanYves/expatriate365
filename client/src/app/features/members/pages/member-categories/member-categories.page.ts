import {
  ChangeDetectionStrategy, Component, computed, inject, OnInit, signal,
} from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { MembersApiService } from '../../services/members-api.service';
import { MembershipCategory } from '@core/models/member.model';
import { ToastService } from '@core/services/toast.service';
import { AuthStore } from '@core/auth/auth.store';
import { PERMISSIONS } from '@core/auth/models/permission.model';

@Component({
  selector: 'app-member-categories',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe, FormsModule, ReactiveFormsModule,
    ButtonModule, InputTextModule, TextareaModule, CheckboxModule, InputNumberModule,
    TagModule, SkeletonModule, TooltipModule, DialogModule, TranslatePipe,
  ],
  template: `
    <div class="flex flex-col gap-4">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-xl font-medium text-gray-900 dark:text-white">{{ 'members.categories_title' | translate }}</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ 'members.categories_subtitle' | translate }}</p>
        </div>
        @if (isMemberAdmin()) {
          <p-button icon="pi pi-plus" [label]="'members.new_category' | translate" size="small" (click)="openDialog()" />
        }
      </div>

      <!-- Categories list -->
      <div class="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            <tr>
              <th class="text-left px-4 py-3">{{ 'common.name' | translate }}</th>
              <th class="text-left px-4 py-3 hidden md:table-cell">{{ 'members.contribution_rate' | translate }}</th>
              <th class="text-left px-4 py-3 hidden lg:table-cell">{{ 'members.voting_rights' | translate }}</th>
              <th class="text-left px-4 py-3 hidden lg:table-cell">{{ 'members.welfare_eligible' | translate }}</th>
              <th class="text-left px-4 py-3">{{ 'common.status' | translate }}</th>
              @if (isMemberAdmin()) {
                <th class="px-4 py-3" style="width:80px"></th>
              }
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-700 bg-white dark:bg-gray-900">
            @if (loading()) {
              @for (i of [1,2,3]; track i) {
                <tr>
                  <td colspan="6" class="px-4 py-3">
                    <div class="h-8 bg-gray-100 dark:bg-gray-700 rounded animate-pulse"></div>
                  </td>
                </tr>
              }
            } @else if (categories().length === 0) {
              <tr>
                <td colspan="6" class="text-center px-4 py-12 text-gray-400">
                  {{ 'members.no_categories' | translate }}
                </td>
              </tr>
            } @else {
              @for (cat of categories(); track cat.id) {
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                  <td class="px-4 py-3">
                    <div class="font-medium text-gray-900 dark:text-white">{{ cat.name }}</div>
                    @if (cat.description) {
                      <div class="text-xs text-gray-400 mt-0.5">{{ cat.description }}</div>
                    }
                  </td>
                  <td class="px-4 py-3 hidden md:table-cell text-gray-600 dark:text-gray-300">
                    {{ cat.contribution_rate | number:'1.0-0' }} XAF
                  </td>
                  <td class="px-4 py-3 hidden lg:table-cell">
                    <p-tag [value]="(cat.voting_rights ? 'common.yes' : 'common.no') | translate"
                      [severity]="cat.voting_rights ? 'success' : 'secondary'" />
                  </td>
                  <td class="px-4 py-3 hidden lg:table-cell">
                    <p-tag [value]="(cat.welfare_eligible ? 'common.yes' : 'common.no') | translate"
                      [severity]="cat.welfare_eligible ? 'success' : 'secondary'" />
                  </td>
                  <td class="px-4 py-3">
                    <p-tag [value]="(cat.is_active ? 'common.active' : 'common.inactive') | translate"
                      [severity]="cat.is_active ? 'success' : 'danger'" />
                  </td>
                  @if (isMemberAdmin()) {
                    <td class="px-4 py-3">
                      <div class="flex gap-1">
                        <p-button icon="pi pi-pencil" severity="secondary" [text]="true" size="small"
                          (click)="openDialog(cat)" [pTooltip]="'common.edit' | translate" />
                        <p-button icon="pi pi-trash" severity="danger" [text]="true" size="small"
                          (click)="deleteCategory(cat)" [pTooltip]="'common.delete' | translate" />
                      </div>
                    </td>
                  }
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create / Edit dialog -->
    <p-dialog
      [(visible)]="dialogVisible"
      [header]="(editingId() ? 'members.edit_category' : 'members.new_category') | translate"
      [modal]="true"
      [style]="{ width: '480px' }"
      [draggable]="false">
      <form [formGroup]="form" class="flex flex-col gap-4 py-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.name' | translate }} *</label>
          <input pInputText formControlName="name" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.description' | translate }}</label>
          <textarea pTextarea formControlName="description" rows="2" autoResize></textarea>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'members.contribution_rate' | translate }}</label>
          <p-inputnumber formControlName="contribution_rate" [min]="0" mode="decimal" />
        </div>
        <div class="flex gap-6">
          <div class="flex items-center gap-2">
            <p-checkbox formControlName="voting_rights" [binary]="true" inputId="voting" />
            <label for="voting" class="text-sm">{{ 'members.voting_rights' | translate }}</label>
          </div>
          <div class="flex items-center gap-2">
            <p-checkbox formControlName="welfare_eligible" [binary]="true" inputId="welfare" />
            <label for="welfare" class="text-sm">{{ 'members.welfare_eligible' | translate }}</label>
          </div>
        </div>
        <div class="flex justify-end gap-2 pt-2 border-t border-gray-100 dark:border-gray-700">
          <button type="button" (click)="dialogVisible = false"
            class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-xl text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">
            {{ 'common.cancel' | translate }}
          </button>
          <button type="button" (click)="save()" [disabled]="saving()"
            class="px-4 py-2 bg-emerald-600 text-white rounded-xl text-sm font-medium hover:bg-emerald-700 disabled:opacity-50">
            @if (saving()) { <i class="pi pi-spin pi-spinner mr-1"></i> }
            {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    </p-dialog>
  `,
})
export class MemberCategoriesPageComponent implements OnInit {
  private readonly api = inject(MembersApiService);
  private readonly toast = inject(ToastService);
  private readonly authStore = inject(AuthStore);
  private readonly fb = inject(FormBuilder);

  readonly isMemberAdmin = computed(() => this.authStore.hasPermission(PERMISSIONS.CATEGORIES_CREATE));

  readonly categories = signal<MembershipCategory[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);

  dialogVisible = false;

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: [''],
    contribution_rate: [0, [Validators.min(0)]],
    voting_rights: [false],
    welfare_eligible: [false],
  });

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.api.categories().subscribe({
      next: (cats) => { this.categories.set(cats); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }

  openDialog(cat?: MembershipCategory): void {
    this.editingId.set(cat?.id ?? null);
    this.form.reset({
      name: cat?.name ?? '',
      description: cat?.description ?? '',
      contribution_rate: cat?.contribution_rate ?? 0,
      voting_rights: cat?.voting_rights ?? false,
      welfare_eligible: cat?.welfare_eligible ?? false,
    });
    this.dialogVisible = true;
  }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const body = this.form.getRawValue() as {
      name: string; description: string; contribution_rate: number;
      voting_rights: boolean; welfare_eligible: boolean;
    };
    this.saving.set(true);
    const id = this.editingId();
    const req = id ? this.api.updateCategory(id, body) : this.api.createCategory(body);
    req.subscribe({
      next: (saved) => {
        if (id) {
          this.categories.update(list => list.map(c => c.id === id ? { ...c, ...saved } : c));
        } else {
          this.categories.update(list => [...list, saved]);
        }
        this.saving.set(false);
        this.dialogVisible = false;
        this.toast.success(id ? 'Catégorie mise à jour.' : 'Catégorie créée.');
      },
      error: () => { this.saving.set(false); this.toast.error('Une erreur est survenue.'); },
    });
  }

  deleteCategory(cat: MembershipCategory): void {
    if (!confirm(`Supprimer "${cat.name}" ?`)) return;
    this.api.deleteCategory(cat.id).subscribe({
      next: () => {
        this.categories.update(list => list.filter(c => c.id !== cat.id));
        this.toast.success('Catégorie supprimée.');
      },
      error: (err) => {
        const msg = err?.error?.error ?? 'Une erreur est survenue.';
        this.toast.error(msg);
      },
    });
  }
}
