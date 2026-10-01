import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { PageHeaderComponent } from '@shared/components/page-header/page-header.component';
import { AuthStore } from '@core/auth/auth.store';
import { AuthService } from '@core/auth/auth.service';
import { ToastService } from '@service/toast.service';
import { HttpClient } from '@angular/common/http';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, PasswordModule, TranslatePipe, PageHeaderComponent],
  template: `
    <div class="p-6 max-w-2xl mx-auto">
      <app-page-header
        [title]="'profile.title' | translate"
        [subtitle]="'profile.subtitle' | translate" />

      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
        <form [formGroup]="form" (ngSubmit)="save()" class="flex flex-col gap-5">

          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ 'common.first_name' | translate }} *</label>
              <input pInputText formControlName="first_name" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-sm font-medium">{{ 'common.last_name' | translate }} *</label>
              <input pInputText formControlName="last_name" />
            </div>
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'common.email' | translate }}</label>
            <input pInputText [value]="user()?.email ?? ''" [disabled]="true"
              class="opacity-60 cursor-not-allowed" />
            <p class="text-xs text-gray-400">{{ 'profile.email_readonly' | translate }}</p>
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'profile.contact_email' | translate }}</label>
            <input pInputText formControlName="contact_email" type="email"
              [placeholder]="'profile.contact_email_ph' | translate" />
            <p class="text-xs text-gray-400">{{ 'profile.contact_email_hint' | translate }}</p>
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'common.phone' | translate }}</label>
            <input pInputText formControlName="phone" type="tel" />
          </div>

          @if (error()) {
            <p class="text-red-500 text-sm">{{ error() }}</p>
          }

          <div class="flex justify-end">
            <p-button type="submit" [label]="'common.save' | translate"
              [loading]="saving()" [disabled]="form.invalid || form.pristine" />
          </div>
        </form>
      </div>

      <!-- Security section -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6 mt-6">
        <h2 class="text-base font-semibold text-gray-900 dark:text-white mb-5">
          {{ 'profile.change_password' | translate }}
        </h2>

        <form [formGroup]="pwForm" (ngSubmit)="changePassword()" class="flex flex-col gap-4">

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'profile.current_password' | translate }} *</label>
            <p-password formControlName="current_password" [feedback]="false" [toggleMask]="true"
              styleClass="w-full" inputStyleClass="w-full" />
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'profile.new_password' | translate }} *</label>
            <p-password formControlName="new_password" [feedback]="true" [toggleMask]="true"
              styleClass="w-full" inputStyleClass="w-full" />
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'profile.confirm_password' | translate }} *</label>
            <p-password formControlName="confirm_password" [feedback]="false" [toggleMask]="true"
              styleClass="w-full" inputStyleClass="w-full" />
            @if (pwForm.errors?.['mismatch'] && pwForm.get('confirm_password')?.dirty) {
              <p class="text-red-500 text-xs">{{ 'profile.password_mismatch' | translate }}</p>
            }
          </div>

          @if (pwError()) {
            <p class="text-red-500 text-sm">{{ pwError() }}</p>
          }

          <div class="flex justify-end">
            <p-button type="submit" [label]="'profile.change_password' | translate"
              severity="secondary"
              [loading]="pwSaving()" [disabled]="pwForm.invalid" />
          </div>
        </form>
      </div>
    </div>
  `,
})
export class ProfilePage implements OnInit {
  private readonly store  = inject(AuthStore);
  private readonly auth   = inject(AuthService);
  private readonly toast  = inject(ToastService);
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly fb     = inject(FormBuilder);

  readonly user     = this.store.user;
  readonly saving   = signal(false);
  readonly error    = signal<string | null>(null);
  readonly pwSaving = signal(false);
  readonly pwError  = signal<string | null>(null);

  readonly form = this.fb.group({
    first_name:    ['', Validators.required],
    last_name:     ['', Validators.required],
    phone:         [''],
    contact_email: ['', Validators.email],
  });

  readonly pwForm = this.fb.group({
    current_password: ['', Validators.required],
    new_password:     ['', [Validators.required, Validators.minLength(8)]],
    confirm_password: ['', Validators.required],
  }, { validators: this.passwordsMatch });

  ngOnInit(): void {
    const u = this.user();
    if (u) {
      const [first, ...rest] = u.full_name.split(' ');
      this.form.patchValue({
        first_name:    first ?? '',
        last_name:     rest.join(' '),
        contact_email: u.contact_email ?? '',
      });
    }
  }

  private passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const nw = group.get('new_password')?.value;
    const confirm = group.get('confirm_password')?.value;
    return nw && confirm && nw !== confirm ? { mismatch: true } : null;
  }

  changePassword(): void {
    if (this.pwForm.invalid) return;
    this.pwSaving.set(true);
    this.pwError.set(null);
    const v = this.pwForm.value;

    this.auth.changePassword(v.current_password!, v.new_password!).subscribe({
      next: () => {
        this.pwSaving.set(false);
        this.pwForm.reset();
        this.toast.success('Mot de passe mis à jour. Reconnectez-vous.');
        this.auth.logout();
      },
      error: (err: any) => {
        this.pwError.set(err?.error?.error ?? 'Erreur lors du changement de mot de passe.');
        this.pwSaving.set(false);
      },
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    const v = this.form.value;

    this.http.put(`${this.config.apiUrl}/api/v1/auth/profile`, {
      first_name:    v.first_name,
      last_name:     v.last_name,
      phone:         v.phone || null,
      contact_email: v.contact_email || null,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.form.markAsPristine();
        this.toast.success('Profil mis à jour.');
        this.auth.me().subscribe();
      },
      error: (err: any) => {
        this.error.set(err?.error?.error ?? 'Erreur lors de la mise à jour.');
        this.saving.set(false);
      },
    });
  }
}
