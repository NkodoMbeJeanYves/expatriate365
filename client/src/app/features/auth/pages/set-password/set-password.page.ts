import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { TranslatePipe } from '@ngx-translate/core';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-set-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, PasswordModule, ButtonModule, TranslatePipe, RouterLink],
  template: `
    <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-xl border border-gray-100 dark:border-gray-800 p-8">

      <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-1">
        {{ 'auth.set_password_title' | translate }}
      </h2>
      <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">
        {{ 'auth.set_password_hint' | translate }}
      </p>

      @if (invalidLink()) {
        <div class="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4 text-sm text-red-700 dark:text-red-400 mb-4">
          <i class="pi pi-times-circle mr-2"></i>{{ 'auth.set_password_invalid' | translate }}
        </div>
        <div class="text-center mt-4">
          <a routerLink="/auth/login" class="text-emerald-600 hover:underline text-sm">
            {{ 'auth.back_to_login' | translate }}
          </a>
        </div>
      }

      @if (done()) {
        <div class="rounded-lg bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 p-4 text-sm text-emerald-700 dark:text-emerald-400 mb-4">
          <i class="pi pi-check-circle mr-2"></i>{{ 'auth.set_password_done' | translate }}
        </div>
        <div class="text-center mt-4">
          <a routerLink="/auth/login" class="text-emerald-600 hover:underline text-sm">
            {{ 'auth.back_to_login' | translate }}
          </a>
        </div>
      }

      @if (!done() && !invalidLink()) {
        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-5">

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-gray-700 dark:text-gray-300">
              {{ 'profile.new_password' | translate }} *
            </label>
            <p-password formControlName="new_password" [feedback]="true" [toggleMask]="true"
              styleClass="w-full" inputStyleClass="w-full" />
          </div>

          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-gray-700 dark:text-gray-300">
              {{ 'profile.confirm_password' | translate }} *
            </label>
            <p-password formControlName="confirm_password" [feedback]="false" [toggleMask]="true"
              styleClass="w-full" inputStyleClass="w-full" />
            @if (form.errors?.['mismatch'] && form.get('confirm_password')?.dirty) {
              <p class="text-red-500 text-xs">{{ 'profile.password_mismatch' | translate }}</p>
            }
          </div>

          @if (error()) {
            <p class="text-red-500 text-sm">{{ error() }}</p>
          }

          <p-button type="submit" styleClass="w-full"
            [label]="'auth.set_password_submit' | translate"
            [loading]="loading()" [disabled]="form.invalid" />
        </form>
      }
    </div>
  `,
})
export class SetPasswordPage implements OnInit {
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);
  private readonly route  = inject(ActivatedRoute);
  private readonly fb     = inject(FormBuilder);

  readonly loading     = signal(false);
  readonly done        = signal(false);
  readonly invalidLink = signal(false);
  readonly error       = signal<string | null>(null);

  private token: string | null = null;

  readonly form = this.fb.group({
    new_password:     ['', [Validators.required, Validators.minLength(8)]],
    confirm_password: ['', Validators.required],
  }, { validators: this.passwordsMatch });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');
    if (!this.token) this.invalidLink.set(true);
  }

  private passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const nw = group.get('new_password')?.value;
    const confirm = group.get('confirm_password')?.value;
    return nw && confirm && nw !== confirm ? { mismatch: true } : null;
  }

  submit(): void {
    if (this.form.invalid || !this.token) return;
    this.loading.set(true);
    this.error.set(null);

    this.http.post(`${this.config.apiUrl}/api/v1/auth/set-password`, {
      token: this.token,
      new_password: this.form.value.new_password,
    }).subscribe({
      next: () => { this.loading.set(false); this.done.set(true); },
      error: (err: any) => {
        const msg = err?.error?.error ?? '';
        if (msg.includes('expiré') || msg.includes('invalide')) this.invalidLink.set(true);
        else this.error.set(msg || 'Erreur lors de l\'activation.');
        this.loading.set(false);
      },
    });
  }
}
