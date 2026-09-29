import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
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
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, TranslatePipe, PageHeaderComponent],
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

  readonly user   = this.store.user;
  readonly saving = signal(false);
  readonly error  = signal<string | null>(null);

  readonly form = this.fb.group({
    first_name:    ['', Validators.required],
    last_name:     ['', Validators.required],
    phone:         [''],
    contact_email: ['', Validators.email],
  });

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
