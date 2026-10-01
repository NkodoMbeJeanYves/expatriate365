import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { AuthStore } from '@core/auth/auth.store';
import { OnboardingService } from '@core/services/onboarding.service';

interface Spotlight {
  icon: string;
  color: string;
  titleKey: string;
  descKey: string;
}

const SPOTLIGHTS: Spotlight[] = [
  { icon: 'pi pi-id-card',         color: 'indigo',   titleKey: 'onboarding.spot1_title', descKey: 'onboarding.spot1_desc' },
  { icon: 'pi pi-comments',        color: 'violet',   titleKey: 'onboarding.spot2_title', descKey: 'onboarding.spot2_desc' },
  { icon: 'pi pi-check-square',    color: 'sky',      titleKey: 'onboarding.spot3_title', descKey: 'onboarding.spot3_desc' },
  { icon: 'pi pi-bell',            color: 'orange',   titleKey: 'onboarding.spot4_title', descKey: 'onboarding.spot4_desc' },
];

@Component({
  selector: 'app-getting-started',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TranslatePipe],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-indigo-50 dark:from-gray-900 dark:via-gray-900 dark:to-gray-800 overflow-y-auto">
      <div class="max-w-3xl mx-auto px-6 py-12 flex flex-col gap-10">

        <!-- Header -->
        <div class="text-center">
          <div class="w-16 h-16 rounded-2xl bg-emerald-500 flex items-center justify-center mx-auto mb-5 shadow-lg shadow-emerald-200 dark:shadow-emerald-900">
            <i class="pi pi-sparkle text-white text-2xl"></i>
          </div>
          <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-2">
            {{ 'onboarding.welcome' | translate:{name: firstName()} }}
          </h1>
          <p class="text-gray-500 dark:text-gray-400 max-w-md mx-auto">
            {{ 'onboarding.' + onboarding.roleFamily() + '.subtitle' | translate }}
          </p>
          <!-- Role badge -->
          <span class="inline-block mt-3 px-3 py-1 bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-400 text-xs font-semibold rounded-full uppercase tracking-wide">
            {{ ('onboarding.role_' + onboarding.roleFamily()) | translate }}
          </span>
        </div>

        <!-- What's new spotlight -->
        <div>
          <div class="flex items-center gap-2 mb-4">
            <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
            <h2 class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-widest">
              {{ 'onboarding.whats_new' | translate }}
            </h2>
          </div>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
            @for (s of spotlights; track s.titleKey) {
              <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-100 dark:border-gray-700 p-4 flex flex-col gap-2 text-center shadow-sm">
                <div class="w-9 h-9 rounded-lg mx-auto flex items-center justify-center"
                  [class]="'bg-' + s.color + '-100 dark:bg-' + s.color + '-900/30'">
                  <i [class]="s.icon + ' text-' + s.color + '-600 dark:text-' + s.color + '-400'"></i>
                </div>
                <p class="text-xs font-semibold text-gray-800 dark:text-gray-200 leading-tight">{{ s.titleKey | translate }}</p>
                <p class="text-xs text-gray-400 leading-snug">{{ s.descKey | translate }}</p>
              </div>
            }
          </div>
        </div>

        <!-- Steps -->
        <div>
          <div class="flex items-center gap-2 mb-4">
            <h2 class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-widest">
              {{ 'onboarding.your_steps' | translate }}
            </h2>
          </div>
          <div class="flex flex-col gap-3">
            @for (step of onboarding.steps(); track step.route; let i = $index; let first = $first) {
              <div
                class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-100 dark:border-gray-700 p-4 flex items-center gap-4 shadow-sm hover:shadow-md hover:border-emerald-200 dark:hover:border-emerald-700 transition-all cursor-pointer group"
                (click)="go(step.route)">

                <!-- Step number + icon -->
                <div class="relative shrink-0">
                  <div class="w-11 h-11 rounded-xl flex items-center justify-center transition-colors"
                    [class]="first ? 'bg-emerald-500' : 'bg-emerald-50 dark:bg-emerald-900/30 group-hover:bg-emerald-100 dark:group-hover:bg-emerald-900/50'">
                    <i [class]="step.icon + ' text-lg ' + (first ? 'text-white' : 'text-emerald-600 dark:text-emerald-400')"></i>
                  </div>
                  <span class="absolute -top-1.5 -right-1.5 w-5 h-5 rounded-full bg-gray-200 dark:bg-gray-600 text-gray-600 dark:text-gray-300 text-xs font-bold flex items-center justify-center">
                    {{ i + 1 }}
                  </span>
                </div>

                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <h3 class="text-sm font-semibold text-gray-900 dark:text-white">
                      {{ step.titleKey | translate }}
                    </h3>
                    @if (step.badge) {
                      <span class="px-1.5 py-0.5 bg-emerald-100 dark:bg-emerald-900/40 text-emerald-700 dark:text-emerald-400 text-xs font-semibold rounded">
                        {{ step.badge | translate }}
                      </span>
                    }
                  </div>
                  <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ step.descKey | translate }}</p>
                </div>

                <!-- Arrow -->
                <i class="pi pi-arrow-right text-gray-300 dark:text-gray-600 group-hover:text-emerald-500 group-hover:translate-x-0.5 transition-all shrink-0"></i>
              </div>
            }
          </div>
        </div>

        <!-- Actions -->
        <div class="flex flex-col sm:flex-row gap-3 justify-center pb-4">
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

  protected readonly spotlights = SPOTLIGHTS;
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
