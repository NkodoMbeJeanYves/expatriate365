import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { AuthStore } from '@core/auth/auth.store';
import { OnboardingService } from '@core/services/onboarding.service';

@Component({
  selector: 'app-getting-started',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-emerald-50 to-white dark:from-gray-900 dark:to-gray-800 flex items-center justify-center p-6">
      <div class="w-full max-w-2xl">

        <!-- Header -->
        <div class="text-center mb-10">
          <div class="w-16 h-16 rounded-2xl bg-emerald-500 flex items-center justify-center mx-auto mb-4">
            <i class="pi pi-sparkle text-white text-2xl"></i>
          </div>
          <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            {{ 'onboarding.welcome' | translate:{name: firstName()} }}
          </h1>
          <p class="text-gray-500 dark:text-gray-400">
            {{ 'onboarding.' + onboarding.roleFamily() + '.subtitle' | translate }}
          </p>
        </div>

        <!-- Steps -->
        <div class="flex flex-col gap-4 mb-10">
          @for (step of onboarding.steps(); track step.route; let i = $index) {
            <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-5 flex items-start gap-4 shadow-sm">
              <div class="w-12 h-12 rounded-xl bg-emerald-50 dark:bg-emerald-900/30 flex items-center justify-center shrink-0">
                <i [class]="step.icon + ' text-emerald-600 dark:text-emerald-400 text-lg'"></i>
              </div>
              <div class="flex-1">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs font-semibold text-emerald-600 dark:text-emerald-400 uppercase tracking-wide">
                    {{ 'onboarding.step' | translate }} {{ i + 1 }}
                  </span>
                </div>
                <h3 class="font-semibold text-gray-900 dark:text-white text-sm mb-0.5">
                  {{ step.titleKey | translate }}
                </h3>
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ step.descKey | translate }}</p>
              </div>
              <p-button
                icon="pi pi-arrow-right"
                [text]="true"
                size="small"
                severity="secondary"
                (onClick)="go(step.route)" />
            </div>
          }
        </div>

        <!-- Actions -->
        <div class="flex flex-col sm:flex-row gap-3 justify-center">
          <p-button
            [label]="'onboarding.start' | translate"
            icon="pi pi-arrow-right"
            iconPos="right"
            (onClick)="start()" />
          <p-button
            [label]="'onboarding.skip' | translate"
            severity="secondary"
            [text]="true"
            (onClick)="skip()" />
        </div>

      </div>
    </div>
  `,
})
export class GettingStartedPage {
  protected readonly onboarding = inject(OnboardingService);
  private readonly auth         = inject(AuthStore);
  private readonly router       = inject(Router);

  readonly firstName = () => this.auth.user()?.full_name?.split(' ')[0] ?? '';

  go(route: string): void {
    this.onboarding.markSeen();
    this.router.navigateByUrl(route);
  }

  start(): void {
    const first = this.onboarding.steps()[0];
    this.go(first?.route ?? '/dashboard');
  }

  skip(): void {
    this.onboarding.markSeen();
    const family = this.onboarding.roleFamily();
    if (family === 'super_admin') {
      this.router.navigateByUrl('/admin/tenants');
    } else if (family === 'member') {
      this.router.navigateByUrl('/member-dashboard');
    } else {
      this.router.navigateByUrl('/dashboard');
    }
  }
}
