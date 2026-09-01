import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '@core/auth/auth.service';
import { TenantService } from '@core/tenant/tenant.service';
import { environment } from '@env/environment';

@Component({
  selector: 'app-login-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule, TranslatePipe],
  template: `
    <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-xl border border-gray-100 dark:border-gray-800 p-8">

      <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-1">{{ 'auth.login' | translate }}</h2>
      <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">{{ 'auth.subtitle' | translate }}</p>

      @if (errorMessage()) {
        <div class="mb-4 rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3 text-sm text-red-700 dark:text-red-400">
          <i class="pi pi-exclamation-circle mr-2"></i>{{ errorMessage() }}
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-5">
        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ 'auth.email' | translate }}</label>
          <input pInputText formControlName="email" type="email"
                 placeholder="vous@exemple.com"
                 class="w-full rounded-lg border-gray-300 dark:border-gray-700" />
        </div>

        <div class="flex flex-col gap-1.5">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ 'auth.password' | translate }}</label>
          <p-password formControlName="password" [feedback]="false" [toggleMask]="true"
                      placeholder="••••••••"
                      styleClass="w-full" inputStyleClass="w-full rounded-lg border-gray-300 dark:border-gray-700" />
        </div>

        <p-button
          type="submit"
          [label]="'auth.sign_in' | translate"
          icon="pi pi-sign-in"
          styleClass="w-full justify-center mt-1"
          [loading]="loading()" />
      </form>

      @if (quickAccounts.length) {
        <div class="mt-6 pt-5 border-t border-gray-100 dark:border-gray-800">
          <p class="text-xs font-medium text-gray-400 uppercase tracking-wider mb-3">{{ 'auth.quick_access' | translate }}</p>
          <div class="grid grid-cols-2 gap-2">
            @for (acc of quickAccounts; track acc.email) {
              <button type="button"
                (click)="quickLogin(acc)"
                class="text-left px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-700
                       hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors cursor-pointer">
                <div class="text-xs font-medium text-gray-800 dark:text-gray-200">{{ acc.label }}</div>
                <div class="text-xs text-gray-400 truncate">{{ acc.email }}</div>
              </button>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class LoginPageComponent {
  private readonly auth          = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  private readonly router        = inject(Router);

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly quickAccounts = environment.quickLoginAccounts;

  readonly form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(6)]),
  });

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.errorMessage.set(null);
    const { email, password } = this.form.getRawValue();
    this.auth.login({ email: email!, password: password! }).subscribe({
      next: async (res) => {
        const isSuperAdmin = res.user.roles?.includes('super_admin');
        const hasTenant   = !!res.user.tenant_id;
        if (hasTenant) await this.tenantService.bootstrap();
        this.router.navigateByUrl(isSuperAdmin && !hasTenant ? '/select-tenant' : '/dashboard');
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.error ?? 'Identifiants incorrects');
        this.loading.set(false);
      },
    });
  }

  quickLogin(acc: { email: string; password: string }): void {
    this.form.setValue({ email: acc.email, password: acc.password });
    this.submit();
  }
}
