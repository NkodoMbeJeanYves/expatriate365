import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TranslatePipe } from '@ngx-translate/core';
import { APP_CONFIG } from '@core/config/app-config.token';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, InputTextModule, ButtonModule, TranslatePipe, RouterLink],
  template: `
    <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-xl border border-gray-100 dark:border-gray-800 p-8">

      <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-1">
        {{ 'auth.forgot_password' | translate }}
      </h2>
      <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">
        {{ 'auth.forgot_password_hint' | translate }}
      </p>

      @if (sent()) {
        <div class="rounded-lg bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 p-4 text-sm text-emerald-700 dark:text-emerald-400 mb-4">
          <i class="pi pi-check-circle mr-2"></i>{{ 'auth.forgot_password_sent' | translate }}
        </div>
      }

      @if (!sent()) {
        <form class="flex flex-col gap-5" (ngSubmit)="submit()">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-gray-700 dark:text-gray-300">
              {{ 'auth.contact_email_label' | translate }}
            </label>
            <input pInputText [formControl]="emailCtrl" type="email"
              [placeholder]="'auth.contact_email_ph' | translate" />
          </div>

          @if (error()) {
            <p class="text-red-500 text-sm">{{ error() }}</p>
          }

          <p-button type="submit" styleClass="w-full" [label]="'auth.send_reset_link' | translate"
            [loading]="loading()" [disabled]="emailCtrl.invalid" />
        </form>
      }

      <div class="mt-6 text-center text-sm text-gray-500">
        <a routerLink="/auth/login" class="text-emerald-600 hover:underline">
          {{ 'auth.back_to_login' | translate }}
        </a>
      </div>
    </div>
  `,
})
export class ForgotPasswordPage {
  private readonly http   = inject(HttpClient);
  private readonly config = inject(APP_CONFIG);

  readonly emailCtrl = new FormControl('', [Validators.required, Validators.email]);
  readonly loading   = signal(false);
  readonly sent      = signal(false);
  readonly error     = signal<string | null>(null);

  submit(): void {
    if (this.emailCtrl.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.http.post(`${this.config.apiUrl}/api/v1/auth/forgot-password`, {
      contact_email: this.emailCtrl.value,
    }).subscribe({
      next: () => { this.loading.set(false); this.sent.set(true); },
      error: () => { this.loading.set(false); this.sent.set(true); }, // same UX regardless
    });
  }
}
