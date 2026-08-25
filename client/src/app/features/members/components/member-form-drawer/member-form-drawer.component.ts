import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy, Component,
  computed, effect,
  inject, input, model, OnInit, output, signal,
  viewChild,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TenantStore } from '@core/tenant/tenant.store';
import { AuthStore } from '@core/auth/auth.store';
import { PERMISSIONS } from '@core/auth/models/permission.model';
import { APP_CONFIG } from '@core/config/app-config.token';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ToastService } from '@service/toast.service';
import { DatePickerModule } from 'primeng/datepicker';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { MembersApiService } from '../../services/members-api.service';
import { MembersStore } from '../../store/members.store';
import { AdminApiService } from '@admin/services/admin-api.service';
import { ROLES } from '@core/auth/models/role.model';
import { RolesApiService } from '@service/roles-api.service';

@Component({
  selector: 'app-member-form-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, InputTextModule, SelectModule,
    DrawerModule, DatePickerModule, MessageModule, TranslatePipe,
  ],
  template: `
    <p-drawer
      #drawerEl
      [(visible)]="visibleBinding"
      [position]="'right'"
      styleClass="w-full sm:w-[480px]"
      [header]="memberId() ? ('members.edit_member' | translate) : ('members.add_member' | translate)">

      @if (error()) {
        <p-message severity="error" styleClass="mb-4 w-full">{{ error() }}</p-message>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4 pb-24">

        <p class="text-sm font-medium text-gray-500 uppercase tracking-wide">Identité</p>

        <!-- Photo -->
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">Photo <span class="text-gray-400 text-xs">(optionnel)</span></label>
          <div class="flex items-center gap-4">
            <div class="w-16 h-16 rounded-full bg-gray-100 border border-gray-200 overflow-hidden flex items-center justify-center flex-shrink-0">
              @if (photoPreview()) {
                <img [src]="photoPreview()" alt="Photo" class="w-full h-full object-cover" />
              } @else {
                <i class="pi pi-user text-2xl text-gray-400"></i>
              }
            </div>
            <label class="cursor-pointer inline-flex items-center gap-2 px-3 py-2 rounded-lg border border-gray-300 text-sm text-gray-600 hover:bg-gray-50 transition-colors">
              @if (photoUploading()) {
                <i class="pi pi-spin pi-spinner text-sm"></i> Envoi en cours…
              } @else {
                <i class="pi pi-upload text-sm"></i> Choisir une photo
              }
              <input type="file" class="hidden" accept="image/jpeg,image/png,image/gif,image/webp"
                (change)="onPhotoSelected($event)" [disabled]="photoUploading()" />
            </label>
            @if (photoPreview()) {
              <button type="button" (click)="removePhoto()" class="text-xs text-red-500 hover:text-red-700">
                <i class="pi pi-times"></i>
              </button>
            }
          </div>
          @if (photoError()) {
            <p class="text-red-500 text-xs">{{ photoError() }}</p>
          }
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.first_name' | translate }} *</label>
            <input pInputText formControlName="first_name" class="w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.last_name' | translate }} *</label>
            <input pInputText formControlName="last_name" class="w-full" />
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'members.email' | translate }}</label>
          <input pInputText formControlName="email" type="email" readonly
            class="w-full bg-gray-50 dark:bg-gray-800 text-gray-500 dark:text-gray-400 cursor-not-allowed"
            [placeholder]="emailPreview()" />
          @if (!memberId()) {
            <p class="text-xs text-gray-400">{{ 'members.email_auto_hint' | translate }}</p>
          }
        </div>

        @if (isMemberAdmin()) {
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.role' | translate }}</label>
            <p-select
              formControlName="role"
              [options]="roleOptions()"
              optionLabel="label"
              optionValue="value"
              appendTo="body"
              styleClass="w-full" />
          </div>
        }

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.phone' | translate }}</label>
            <input pInputText formControlName="phone" class="w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.gender' | translate }}</label>
            <p-select
              formControlName="gender"
              [options]="genderOptions"
              optionLabel="label"
              optionValue="value"
              [placeholder]="'common.none' | translate"
              appendTo="body"
              styleClass="w-full" />
          </div>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.date_of_birth' | translate }}</label>
            <p-datepicker formControlName="date_of_birth" [showIcon]="true" dateFormat="yy-mm-dd" styleClass="w-full" inputStyleClass="w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.profession' | translate }}</label>
            <input pInputText formControlName="profession" class="w-full" />
          </div>
        </div>

        <p class="text-sm font-medium text-gray-500 uppercase tracking-wide mt-2">Adhésion</p>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.joined_date' | translate }} *</label>
            <p-datepicker formControlName="joined_date" [showIcon]="true" dateFormat="yy-mm-dd" styleClass="w-full" inputStyleClass="w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.expiry_date' | translate }}</label>
            <p-datepicker formControlName="expiry_date" [showIcon]="true" dateFormat="yy-mm-dd" styleClass="w-full" inputStyleClass="w-full" />
          </div>
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'members.category' | translate }}</label>
          <p-select
            formControlName="category_id"
            [options]="store.categories()"
            optionLabel="name"
            optionValue="id"
            [placeholder]="'common.none' | translate"
            appendTo="body"
            styleClass="w-full" />
        </div>

        <p class="text-sm font-medium text-gray-500 uppercase tracking-wide mt-2">Contact</p>

        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'members.address' | translate }}</label>
          <input pInputText formControlName="address" class="w-full" />
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.emergency_contact' | translate }}</label>
            <input pInputText formControlName="emergency_contact_name" class="w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'members.emergency_phone' | translate }}</label>
            <input pInputText formControlName="emergency_contact_phone" class="w-full" />
          </div>
        </div>
      </form>

      <div class="absolute bottom-0 left-0 right-0 p-4 bg-white border-t border-gray-200 flex gap-3">
        <button type="button" (click)="drawerRef()?.close($event)"
          class="flex-1 px-4 py-2.5 border border-gray-300 rounded-xl text-sm font-medium text-gray-700 hover:bg-gray-50">
          {{ 'common.cancel' | translate }}
        </button>
        <button type="button" (click)="submit()" [disabled]="loading()"
          class="flex-1 px-4 py-2.5 rounded-xl text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50">
          {{ loading() ? ('common.loading' | translate) : ('common.save' | translate) }}
        </button>
      </div>
    </p-drawer>
  `,
})
export class MemberFormDrawerComponent implements OnInit {
  readonly visible = model<boolean>(false);
  get visibleBinding() { return this.visible(); }
  set visibleBinding(v: boolean) { this.visible.set(v); }
  readonly drawerRef = viewChild<Drawer>('drawerEl');

  readonly memberId = input<string | null>(null);
  readonly saved = output<void>();

  readonly store = inject(MembersStore);
  private readonly api = inject(MembersApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly rolesApi = inject(RolesApiService);
  private readonly translate = inject(TranslateService);
  private readonly http = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly toast = inject(ToastService);
  private readonly tenantStore = inject(TenantStore);
  private readonly authStore = inject(AuthStore);

  readonly isMemberAdmin = computed(() => this.authStore.hasPermission(PERMISSIONS.MEMBERS_CREATE));

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private readonly _firstName = signal('');
  private readonly _lastName  = signal('');

  readonly emailPreview = computed(() => {
    const first = this.normalizeNamePart(this._firstName());
    const last  = this.normalizeNamePart(this._lastName());
    if (!first && !last) return 'prenom.nom@association.pays';
    const slug    = this.tenantStore.settings().slug || 'org';
    const country = (this.tenantStore.settings().country_code || 'MU').toLowerCase();
    return `${first || 'prenom'}.${last || 'nom'}@${slug}.${country}`;
  });
  readonly photoUrl = signal<string | null>(null);
  readonly photoPreview = signal<string | null>(null);
  readonly photoUploading = signal(false);
  readonly photoError = signal<string | null>(null);

  get genderOptions() {
    return [
      { label: this.translate.instant('members.gender_male'), value: 'M' },
      { label: this.translate.instant('members.gender_female'), value: 'F' },
    ];
  }

  readonly roleOptions = signal<{ label: string; value: string }[]>([
    { label: 'Membre', value: 'member' }, // fallback
  ]);

  readonly form = new FormGroup({
    first_name: new FormControl('', [Validators.required]),
    last_name: new FormControl('', [Validators.required]),
    email: new FormControl({ value: '', disabled: true }),
    role: new FormControl('member'),
    phone: new FormControl(''),
    gender: new FormControl(''),
    date_of_birth: new FormControl<Date | null>(null),
    profession: new FormControl(''),
    joined_date: new FormControl<Date | null>(null, [Validators.required]),
    expiry_date: new FormControl<Date | null>(null),
    category_id: new FormControl(''),
    address: new FormControl(''),
    emergency_contact_name: new FormControl(''),
    emergency_contact_phone: new FormControl(''),
  });

  private _referenceLoaded = false;

  constructor() {
    effect(() => {
      const id = this.memberId();
      if (this.visible()) {
        // Load reference data once on first open
        if (!this._referenceLoaded) {
          this._referenceLoaded = true;
          this.store.loadCategories();
          this.rolesApi.list().subscribe({
            next: (roles) => this.roleOptions.set(roles.map(r => ({ label: r.label, value: r.name }))),
            error: () => {},
          });
        }
        if (id) {
          this.loadMember(id);
        } else {
          this.form.reset({ joined_date: new Date() });
          this.error.set(null);
          this.photoUrl.set(null);
          this.photoPreview.set(null);
          this.photoError.set(null);
        }
      }
    });
  }

  ngOnInit(): void {
    this.form.get('first_name')!.valueChanges.subscribe(v => this._firstName.set(v ?? ''));
    this.form.get('last_name')!.valueChanges.subscribe(v => this._lastName.set(v ?? ''));
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.photoError.set(null);
    this.photoUploading.set(true);
    const preview = URL.createObjectURL(file);
    this.photoPreview.set(preview);
    const fd = new FormData();
    fd.append('file', file);
    this.http.post<{ file_url: string }>(`${this.config.apiUrl}/api/v1/upload`, fd).subscribe({
      next: r => { this.photoUrl.set(r.file_url); this.photoUploading.set(false); },
      error: () => {
        this.photoError.set('Échec de l\'envoi de la photo.');
        this.photoPreview.set(null);
        this.photoUploading.set(false);
      },
    });
  }

  removePhoto(): void {
    this.photoUrl.set(null);
    this.photoPreview.set(null);
  }

  normalizeNamePart(s: string): string {
    return s.toLowerCase()
      .normalize('NFD').replace(/\p{Mn}/gu, '')
      .replace(/[\s\-_]+/g, '.')
      .replace(/[^a-z0-9.]/g, '')
      .replace(/\.{2,}/g, '.')
      .replace(/^\.+|\.+$/g, '');
  }

  private loadMember(id: string): void {
    this.api.getById(id).subscribe({
      next: (m) => {
        if (m.photo_url) { this.photoUrl.set(m.photo_url); this.photoPreview.set(m.photo_url); }
        this.form.patchValue({
          first_name: m.first_name,
          last_name: m.last_name,
          role: m.role ?? 'member',
          phone: m.phone ?? '',
          gender: m.gender ?? '',
          date_of_birth: m.date_of_birth ? new Date(m.date_of_birth) : null,
          profession: m.profession ?? '',
          joined_date: new Date(m.joined_date),
          expiry_date: m.expiry_date ? new Date(m.expiry_date) : null,
          category_id: m.category_id ?? '',
          address: m.address ?? '',
          emergency_contact_name: m.emergency_contact_name ?? '',
          emergency_contact_phone: m.emergency_contact_phone ?? '',
        });
      },
      error: () => this.error.set('Impossible de charger le membre.'),
    });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set(null);

    const v = this.form.getRawValue();
    const toDateStr = (d: Date | null) => d ? d.toISOString().slice(0, 10) : undefined;

    const photo = this.photoUrl() ?? undefined;
    const id = this.memberId();
    const obs = id
      ? this.api.update(id, {
          first_name: v.first_name!, last_name: v.last_name!, email: v.email!,
          phone: v.phone || undefined, gender: v.gender || undefined,
          date_of_birth: toDateStr(v.date_of_birth),
          profession: v.profession || undefined,
          joined_date: toDateStr(v.joined_date)!,
          expiry_date: toDateStr(v.expiry_date),
          photo_url: photo,
          category_id: v.category_id || undefined,
          address: v.address || undefined,
          emergency_contact_name: v.emergency_contact_name || undefined,
          emergency_contact_phone: v.emergency_contact_phone || undefined,
          is_active: true,
        })
      : this.api.create({
          first_name: v.first_name!, last_name: v.last_name!,
          phone: v.phone || undefined, gender: v.gender || undefined,
          date_of_birth: toDateStr(v.date_of_birth),
          profession: v.profession || undefined,
          joined_date: toDateStr(v.joined_date)!,
          expiry_date: toDateStr(v.expiry_date),
          photo_url: photo,
          category_id: v.category_id || undefined,
          address: v.address || undefined,
          emergency_contact_name: v.emergency_contact_name || undefined,
          emergency_contact_phone: v.emergency_contact_phone || undefined,
        });

    obs.subscribe({
      next: (member) => {
        const selectedRole = v.role || 'member';
        const applyRole$ = selectedRole !== 'member'
          ? this.adminApi.changeRole(member.user_id, { role: selectedRole })
          : null;

        const finish = () => {
          this.loading.set(false);
          this.toast.success(id ? 'Membre mis à jour.' : 'Membre créé avec succès.');
          this.saved.emit();
        };

        if (applyRole$) {
          applyRole$.subscribe({ next: finish, error: finish });
        } else {
          finish();
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error('Erreur lors de l\'enregistrement.');
        this.error.set(err?.error?.error ?? 'Une erreur est survenue.');
      },
    });
  }
}
