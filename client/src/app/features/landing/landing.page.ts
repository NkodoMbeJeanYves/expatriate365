import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { LangSwitcherComponent } from '@shared/components/lang-switcher/lang-switcher.component';
import { BrandLogoComponent } from '@shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-landing',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule, TranslatePipe, LangSwitcherComponent, BrandLogoComponent],
  template: `
    <div class="min-h-screen bg-white dark:bg-gray-950 flex flex-col">

      <!-- ── Navbar ──────────────────────────────────────────────────────────── -->
      <header class="sticky top-0 z-50 bg-white/90 dark:bg-gray-900/90 backdrop-blur
                     border-b border-gray-100 dark:border-gray-800 px-6 py-4
                     flex items-center justify-between">
        <app-brand-logo />
        <div class="flex items-center gap-3">
          <app-lang-switcher />
          <a routerLink="/explore">
            <p-button [label]="'landing.nav_explore' | translate" severity="secondary" size="small" />
          </a>
          <a routerLink="/auth/login">
            <p-button [label]="'landing.nav_login' | translate" icon="pi pi-sign-in" size="small" />
          </a>
        </div>
      </header>

      <!-- ── Hero ───────────────────────────────────────────────────────────── -->
      <section class="flex-1 flex flex-col items-center justify-center text-center
                      px-6 py-20 bg-gradient-to-b from-primary-50 to-white
                      dark:from-gray-900 dark:to-gray-950">
        <div class="inline-flex items-center gap-2 bg-primary-100 dark:bg-primary-900/40
                    text-primary-700 dark:text-primary-300 text-xs font-semibold
                    px-3 py-1.5 rounded-full mb-6">
          <i class="pi pi-map-marker text-xs"></i>
          {{ 'landing.hero_badge' | translate }}
        </div>

        <h1 class="text-4xl sm:text-5xl font-extrabold text-gray-900 dark:text-white
                   leading-tight max-w-2xl mb-5">
          {{ 'landing.hero_title' | translate }}
        </h1>

        <p class="text-lg text-gray-500 dark:text-gray-400 max-w-xl mb-10">
          {{ 'landing.hero_subtitle' | translate }}
        </p>

        <div class="flex flex-wrap items-center justify-center gap-4">
          <a routerLink="/explore">
            <p-button
              [label]="'landing.hero_cta_explore' | translate"
              icon="pi pi-compass"
              size="large"
            />
          </a>
          <a routerLink="/auth/login">
            <p-button
              [label]="'landing.hero_cta_login' | translate"
              icon="pi pi-sign-in"
              severity="secondary"
              size="large"
            />
          </a>
        </div>
      </section>

      <!-- ── Fonctionnalités ─────────────────────────────────────────────────── -->
      <section class="py-20 px-6 bg-white dark:bg-gray-950">
        <div class="max-w-5xl mx-auto">
          <div class="text-center mb-14">
            <h2 class="text-3xl font-bold text-gray-900 dark:text-white mb-3">
              {{ 'landing.features_title' | translate }}
            </h2>
            <p class="text-gray-500 dark:text-gray-400">
              {{ 'landing.features_subtitle' | translate }}
            </p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            @for (f of features; track f.icon) {
              <div class="p-6 rounded-2xl border border-gray-100 dark:border-gray-800
                          bg-gray-50 dark:bg-gray-900 hover:shadow-md transition-shadow">
                <div class="w-11 h-11 rounded-xl flex items-center justify-center mb-4"
                     [style.background]="f.bg">
                  <i [class]="'pi ' + f.icon + ' text-xl'" [style.color]="f.color"></i>
                </div>
                <h3 class="font-semibold text-gray-900 dark:text-white mb-1.5">
                  {{ f.title | translate }}
                </h3>
                <p class="text-sm text-gray-500 dark:text-gray-400 leading-relaxed">
                  {{ f.desc | translate }}
                </p>
              </div>
            }
          </div>
        </div>
      </section>

      <!-- ── Annuaire professionnel ─────────────────────────────────────────── -->
      <section class="py-16 px-6 bg-gray-50 dark:bg-gray-900">
        <div class="max-w-5xl mx-auto flex flex-col lg:flex-row items-center gap-10">
          <div class="flex-1">
            <div class="inline-flex items-center gap-2 bg-amber-100 dark:bg-amber-900/40
                        text-amber-700 dark:text-amber-300 text-xs font-semibold
                        px-3 py-1.5 rounded-full mb-4">
              <i class="pi pi-briefcase text-xs"></i>
              {{ 'landing.directory_badge' | translate }}
            </div>
            <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-3">
              {{ 'landing.directory_title' | translate }}
            </h2>
            <p class="text-gray-500 dark:text-gray-400 mb-6 leading-relaxed">
              {{ 'landing.directory_desc' | translate }}
            </p>
            <a routerLink="/explore">
              <p-button
                [label]="'landing.directory_cta' | translate"
                icon="pi pi-briefcase"
                severity="secondary"
              />
            </a>
          </div>
          <div class="flex-1 grid grid-cols-2 gap-3">
            @for (p of exampleProfessions; track p.icon) {
              <div class="flex items-center gap-3 bg-white dark:bg-gray-800 rounded-xl p-4
                          border border-gray-100 dark:border-gray-700">
                <div class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0"
                     [style.background]="p.bg">
                  <i [class]="'pi ' + p.icon" [style.color]="p.color"></i>
                </div>
                <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
                  {{ p.label | translate }}
                </span>
              </div>
            }
          </div>
        </div>
      </section>

      <!-- ── Communautés (CTA explore) ──────────────────────────────────────── -->
      <section class="py-16 px-6 bg-primary-600 dark:bg-primary-700 text-center">
        <h2 class="text-2xl font-bold text-white mb-3">
          {{ 'landing.cta_title' | translate }}
        </h2>
        <p class="text-primary-100 mb-8 max-w-lg mx-auto">
          {{ 'landing.cta_subtitle' | translate }}
        </p>
        <a routerLink="/explore">
          <p-button
            [label]="'landing.cta_button' | translate"
            icon="pi pi-compass"
            severity="contrast"
            size="large"
          />
        </a>
      </section>

      <!-- ── Footer ─────────────────────────────────────────────────────────── -->
      <footer class="py-8 px-6 border-t border-gray-100 dark:border-gray-800
                     text-center text-sm text-gray-400 dark:text-gray-600">
        © {{ year }} Expatriate365 — {{ 'landing.footer_tagline' | translate }}
      </footer>
    </div>
  `,
})
export class LandingPage {
  readonly year = new Date().getFullYear();

  readonly exampleProfessions = [
    { icon: 'pi-heart', label: 'landing.prof_doctor',   bg: '#FFF1F2', color: '#E11D48' },
    { icon: 'pi-briefcase', label: 'landing.prof_lawyer', bg: '#EEF2FF', color: '#4F46E5' },
    { icon: 'pi-wrench', label: 'landing.prof_engineer', bg: '#F0FDF4', color: '#16A34A' },
    { icon: 'pi-building', label: 'landing.prof_accountant', bg: '#FFFBEB', color: '#D97706' },
  ];

  readonly features = [
    {
      icon: 'pi-users',
      title: 'landing.feat_community_title',
      desc: 'landing.feat_community_desc',
      bg: '#EEF2FF', color: '#4F46E5',
    },
    {
      icon: 'pi-check-circle',
      title: 'landing.feat_vote_title',
      desc: 'landing.feat_vote_desc',
      bg: '#F0FDF4', color: '#16A34A',
    },
    {
      icon: 'pi-calendar',
      title: 'landing.feat_meetings_title',
      desc: 'landing.feat_meetings_desc',
      bg: '#FFF7ED', color: '#EA580C',
    },
    {
      icon: 'pi-wallet',
      title: 'landing.feat_contributions_title',
      desc: 'landing.feat_contributions_desc',
      bg: '#FFFBEB', color: '#D97706',
    },
    {
      icon: 'pi-heart',
      title: 'landing.feat_welfare_title',
      desc: 'landing.feat_welfare_desc',
      bg: '#FFF1F2', color: '#E11D48',
    },
    {
      icon: 'pi-file',
      title: 'landing.feat_docs_title',
      desc: 'landing.feat_docs_desc',
      bg: '#F0F9FF', color: '#0284C7',
    },
  ];
}
