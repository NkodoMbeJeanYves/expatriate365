import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { email, form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthStore } from '@core/auth/auth.store';
import { TranslatePipe } from '@ngx-translate/core';
import { LangSwitcherComponent } from '@shared/components/lang-switcher/lang-switcher.component';
import { ButtonModule } from 'primeng/button';
import { firstValueFrom } from 'rxjs';
import { CommunityShowcase, ShowcasesApiService } from '../services/showcases-api.service';

@Component({
  selector: 'app-showcase-detail-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, RouterLink, ButtonModule, TranslatePipe, LangSwitcherComponent],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">
      <header
        class="flex items-center justify-between border-b border-gray-200 bg-white px-6 py-4 dark:border-gray-800 dark:bg-gray-900"
      >
        <a
          [routerLink]="['/showcases', tenantSlug()]"
          class="font-bold text-gray-900 no-underline dark:text-white"
        >
          {{ 'showcases.title' | translate }}
        </a>
        <div class="flex items-center gap-3">
          <app-lang-switcher />
          @if (authStore.isAuthenticated()) {
            <a routerLink="/profile">
              <p-button
                [label]="'nav.profile' | translate"
                icon="pi pi-user"
                severity="secondary"
                size="small"
              />
            </a>
          } @else {
            <a routerLink="/auth/login">
              <p-button
                [label]="'auth.login' | translate"
                icon="pi pi-sign-in"
                severity="secondary"
                size="small"
              />
            </a>
          }
        </div>
      </header>
      <main class="mx-auto max-w-4xl px-5 py-10">
        @if (loading()) {
          <p class="py-12 text-center text-gray-500">{{ 'common.loading' | translate }}</p>
        } @else if (error() || !showcase()) {
          <p class="rounded-lg bg-red-50 p-4 text-red-700">
            {{ 'showcases.load_error' | translate }}
          </p>
        } @else {
          <a [routerLink]="['/showcases', tenantSlug()]" class="text-sm text-emerald-700">{{
            'showcases.back' | translate
          }}</a>
          @if (showcase()!.photo_url) {
            <img
              [src]="showcase()!.photo_url!"
              [alt]="showcase()!.title"
              class="mt-5 aspect-video max-h-[28rem] w-full rounded-2xl object-cover"
            />
          }
          <div class="mt-6">
            <p class="text-sm font-semibold uppercase tracking-wide text-emerald-700">
              {{ showcase()!.category }}
            </p>
            <h1 class="mt-2 text-3xl font-bold text-gray-900 dark:text-white">
              {{ showcase()!.title }}
            </h1>
            <p class="mt-2 text-gray-500">
              {{ showcase()!.display_name }} · {{ showcase()!.tenant_name }}
            </p>
            <p class="mt-5 whitespace-pre-line text-gray-700 dark:text-gray-300">
              {{ showcase()!.description }}
            </p>
            @if (showcase()!.service_area) {
              <p class="mt-4 text-sm text-gray-500">
                {{ 'showcases.service_area' | translate }}: {{ showcase()!.service_area }}
              </p>
            }
          </div>

          <section class="mt-9">
            <h2 class="text-xl font-semibold text-gray-900 dark:text-white">
              {{ 'showcases.offers' | translate }}
            </h2>
            <div class="mt-4 grid gap-4 sm:grid-cols-2">
              @for (offer of showcase()!.offers; track offer.id) {
                <article
                  class="rounded-xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900"
                >
                  <h3 class="font-semibold text-gray-900 dark:text-white">{{ offer.name }}</h3>
                  <p class="mt-2 whitespace-pre-line text-sm text-gray-600 dark:text-gray-300">
                    {{ offer.description }}
                  </p>
                  @if (offer.details) {
                    <p class="mt-2 text-xs text-gray-500">{{ offer.details }}</p>
                  }
                  <p class="mt-4 font-medium text-emerald-700">
                    @if (offer.price_on_request) {
                      {{ 'showcases.price_on_request' | translate }}
                    } @else {
                      {{ offer.price }} {{ showcase()!.currency_symbol }}
                    }
                  </p>
                </article>
              }
            </div>
          </section>

          <div class="mt-9 grid gap-6 md:grid-cols-2">
            <section
              class="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900"
            >
              <h2 class="text-lg font-semibold text-gray-900 dark:text-white">
                {{ 'showcases.contact_title' | translate }}
              </h2>
              @if (showcase()!.contact_available) {
                <p class="mt-1 text-sm text-gray-500">
                  {{ 'showcases.contact_privacy' | translate }}
                </p>
                @if (contactSent()) {
                  <p class="mt-4 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800">
                    {{ 'showcases.contact_sent' | translate }}
                  </p>
                } @else {
                  <form class="mt-4 space-y-3" (submit)="sendContact(); $event.preventDefault()">
                    <input
                      class="w-full rounded-lg border p-3 dark:bg-gray-950"
                      [formField]="contactForm.name"
                      [placeholder]="'showcases.name' | translate"
                    />
                    <input
                      class="w-full rounded-lg border p-3 dark:bg-gray-950"
                      type="email"
                      [formField]="contactForm.email"
                      [placeholder]="'showcases.email' | translate"
                    />
                    <textarea
                      class="w-full rounded-lg border p-3 dark:bg-gray-950"
                      [formField]="contactForm.message"
                      rows="4"
                      [placeholder]="'showcases.message' | translate"
                    ></textarea>
                    <input
                      class="hidden"
                      [formField]="contactForm.website"
                      tabindex="-1"
                      autocomplete="off"
                      aria-hidden="true"
                    />
                    @if (contactForm().touched() && contactForm().invalid()) {
                      <p class="text-sm text-red-600">{{ 'showcases.form_invalid' | translate }}</p>
                    }
                    @if (contactError()) {
                      <p class="text-sm text-red-600">
                        {{ 'showcases.contact_error' | translate }}
                      </p>
                    }
                    <button
                      class="rounded-lg bg-emerald-700 px-4 py-2 font-medium text-white disabled:opacity-50"
                      [disabled]="sending()"
                    >
                      {{ 'showcases.send' | translate }}
                    </button>
                  </form>
                }
              } @else {
                <p class="mt-2 text-sm text-gray-500">
                  {{ 'showcases.contact_unavailable' | translate }}
                </p>
              }
              @if (showcase()!.contact_phone) {
                <p class="mt-4 text-sm">
                  {{ 'showcases.phone' | translate }}: {{ showcase()!.contact_phone }}
                </p>
              }
              @if (showcase()!.contact_link) {
                <a
                  class="mt-2 inline-block text-sm text-emerald-700"
                  [href]="showcase()!.contact_link"
                  target="_blank"
                  rel="noopener noreferrer"
                  >{{ 'showcases.external_contact' | translate }}</a
                >
              }
            </section>

            <section
              class="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900"
            >
              <h2 class="text-lg font-semibold text-gray-900 dark:text-white">
                {{ 'showcases.report_title' | translate }}
              </h2>
              @if (reportSent()) {
                <p class="mt-4 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-800">
                  {{ 'showcases.report_sent' | translate }}
                </p>
              } @else {
                <form class="mt-4 space-y-3" (submit)="sendReport(); $event.preventDefault()">
                  <textarea
                    class="w-full rounded-lg border p-3 dark:bg-gray-950"
                    [formField]="reportForm.reason"
                    rows="4"
                    [placeholder]="'showcases.report_reason' | translate"
                  ></textarea>
                  <input
                    class="hidden"
                    [formField]="reportForm.website"
                    tabindex="-1"
                    autocomplete="off"
                    aria-hidden="true"
                  />
                  @if (reportForm().touched() && reportForm().invalid()) {
                    <p class="text-sm text-red-600">{{ 'showcases.form_invalid' | translate }}</p>
                  }
                  @if (reportError()) {
                    <p class="text-sm text-red-600">{{ 'showcases.report_error' | translate }}</p>
                  }
                  <button
                    class="rounded-lg border border-gray-300 px-4 py-2 text-gray-700 dark:border-gray-700 dark:text-gray-200"
                    [disabled]="reporting()"
                  >
                    {{ 'showcases.report_send' | translate }}
                  </button>
                </form>
              }
            </section>
          </div>
        }
      </main>
    </div>
  `,
})
export class ShowcaseDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ShowcasesApiService);
  readonly authStore = inject(AuthStore);
  readonly tenantSlug = signal('');
  readonly showcase = signal<CommunityShowcase | null>(null);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly sending = signal(false);
  readonly contactSent = signal(false);
  readonly contactError = signal(false);
  readonly reporting = signal(false);
  readonly reportSent = signal(false);
  readonly reportError = signal(false);
  private readonly contactModel = signal({ name: '', email: '', message: '', website: '' });
  readonly contactForm = form(this.contactModel, (path) => {
    required(path.name);
    maxLength(path.name, 160);
    required(path.email);
    email(path.email);
    maxLength(path.email, 254);
    required(path.message);
    maxLength(path.message, 3000);
  });
  private readonly reportModel = signal({ reason: '', website: '' });
  readonly reportForm = form(this.reportModel, (path) => {
    required(path.reason);
    maxLength(path.reason, 1000);
  });

  ngOnInit(): void {
    const tenantSlug = this.route.snapshot.paramMap.get('tenantSlug') ?? '';
    const slug = this.route.snapshot.paramMap.get('showcaseSlug') ?? '';
    this.tenantSlug.set(tenantSlug);
    this.api.get(tenantSlug, slug).subscribe({
      next: (showcase) => {
        this.showcase.set(showcase);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  sendContact(): void {
    submit(this.contactForm, async () => {
      this.sending.set(true);
      this.contactError.set(false);
      try {
        await firstValueFrom(
          this.api.contact(this.tenantSlug(), this.showcase()!.slug, this.contactModel()),
        );
        this.contactSent.set(true);
      } catch {
        this.contactError.set(true);
      } finally {
        this.sending.set(false);
      }
    });
  }

  sendReport(): void {
    submit(this.reportForm, async () => {
      this.reporting.set(true);
      this.reportError.set(false);
      try {
        await firstValueFrom(
          this.api.report(
            this.tenantSlug(),
            this.showcase()!.slug,
            this.reportModel().reason,
            this.reportModel().website,
          ),
        );
        this.reportSent.set(true);
      } catch {
        this.reportError.set(true);
      } finally {
        this.reporting.set(false);
      }
    });
  }
}
