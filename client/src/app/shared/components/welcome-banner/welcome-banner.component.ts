import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { OnboardingService } from '@core/services/onboarding.service';

@Component({
  selector: 'app-welcome-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TranslatePipe],
  template: `
    @if (visible()) {
      <div class="relative rounded-2xl bg-gradient-to-r from-emerald-500 to-teal-600 p-5 text-white mb-6 overflow-hidden">

        <!-- Background decoration -->
        <div class="absolute -right-8 -top-8 w-32 h-32 rounded-full bg-white/10"></div>
        <div class="absolute -right-2 top-8 w-16 h-16 rounded-full bg-white/10"></div>

        <div class="relative flex items-center justify-between gap-4">
          <div class="flex-1">
            <div class="flex items-center gap-2 mb-1">
              <i class="pi pi-sparkle text-yellow-200 text-sm"></i>
              <span class="text-xs font-semibold text-emerald-100 uppercase tracking-wide">
                {{ 'onboarding.banner_tag' | translate }}
              </span>
            </div>
            <h2 class="font-bold text-lg mb-0.5">
              {{ 'onboarding.' + onboarding.roleFamily() + '.banner_title' | translate }}
            </h2>
            <p class="text-sm text-emerald-100">
              {{ 'onboarding.' + onboarding.roleFamily() + '.banner_desc' | translate }}
            </p>
          </div>
          <div class="flex items-center gap-2 shrink-0">
            <p-button
              [label]="'onboarding.banner_cta' | translate"
              icon="pi pi-arrow-right"
              iconPos="right"
              severity="contrast"
              size="small"
              (onClick)="openGuide()" />
            <button type="button" (click)="dismiss()"
              class="w-7 h-7 rounded-full flex items-center justify-center hover:bg-white/20 transition-colors">
              <i class="pi pi-times text-xs"></i>
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class WelcomeBannerComponent {
  protected readonly onboarding = inject(OnboardingService);
  private  readonly router      = inject(Router);

  readonly visible = signal(!this.onboarding.isSeen());

  openGuide(): void {
    this.router.navigateByUrl('/getting-started');
  }

  dismiss(): void {
    this.onboarding.markSeen();
    this.visible.set(false);
  }
}
