import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { TagModule } from 'primeng/tag';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@env/environment';
import { TransferAssistanceProfile, TransferAssistanceRules } from '@models/transfer-assistance.model';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';

type CheckItem = { key: string; ok: boolean };

@Component({
  selector: 'app-transfer-assistance-info-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslatePipe, TagModule, DecimalPipe],
  template: `
    <main class="min-h-full space-y-6 bg-gray-50 p-4 md:p-8">
      <header class="space-y-2">
        <a routerLink="/transfer-assistance" class="inline-flex items-center gap-1 text-sm text-primary-600 hover:underline">
          <i class="pi pi-arrow-left" aria-hidden="true"></i>{{ 'transfer_assistance.info.back' | translate }}
        </a>
        <h1 class="text-2xl font-bold text-gray-900">{{ 'transfer_assistance.info.title' | translate }}</h1>
        <p class="max-w-3xl text-sm text-gray-600">{{ 'transfer_assistance.info.subtitle' | translate }}</p>
      </header>

      @if (!featureEnabled) {
        <p role="status" class="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
          {{ 'transfer_assistance.feature_disabled' | translate }}
        </p>
      } @else {
        @if (loading()) {
          <p role="status" class="text-sm text-gray-500">{{ 'transfer_assistance.info.loading' | translate }}</p>
        } @else if (!rules()) {
          <p role="status" class="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
            {{ 'transfer_assistance.info.rules_unavailable' | translate }}
          </p>
        }

        @if (rules(); as r) {
          <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm" aria-labelledby="ta-info-status">
            <h2 id="ta-info-status" class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.my_status_title' | translate }}</h2>
            @if (r.my_eligibility.is_community_admin) {
              <p class="mt-3 rounded-lg bg-blue-50 p-3 text-sm text-blue-900" data-testid="admin-excluded">
                {{ 'transfer_assistance.info.my_status_admin' | translate }}
              </p>
            } @else {
              <ul class="mt-3 grid gap-2 sm:grid-cols-2">
                @for (item of checklist(); track item.key) {
                  <li class="flex items-center gap-2 text-sm text-gray-700" [attr.data-check]="item.key">
                    <i [class]="item.ok ? 'pi pi-check-circle text-green-600' : 'pi pi-times-circle text-red-500'" aria-hidden="true"></i>
                    <span>{{ ('transfer_assistance.info.check_' + item.key) | translate }}</span>
                    <span class="sr-only">{{ (item.ok ? 'transfer_assistance.info.status_ok' : 'transfer_assistance.info.status_missing') | translate }}</span>
                  </li>
                }
              </ul>
              @if (profile(); as p) {
                <div class="mt-4 flex flex-wrap gap-3 text-sm">
                  <span>{{ 'transfer_assistance.info.helper_title' | translate }} :
                    <p-tag [value]="('transfer_assistance.verification_' + p.helper_verification_status) | translate"
                      [severity]="severity(p.helper_verification_status)" /></span>
                  <span>{{ 'transfer_assistance.info.witness_title' | translate }} :
                    <p-tag [value]="('transfer_assistance.verification_' + p.witness_verification_status) | translate"
                      [severity]="severity(p.witness_verification_status)" /></span>
                </div>
              }
              <p class="mt-4 text-sm" [class]="r.my_eligibility.can_request_accreditation ? 'text-green-700' : 'text-amber-800'">
                {{ (r.my_eligibility.can_request_accreditation
                  ? 'transfer_assistance.info.can_request'
                  : 'transfer_assistance.info.cannot_request') | translate }}
              </p>
              <a routerLink="/transfer-assistance" class="mt-3 inline-flex items-center gap-1 text-sm font-medium text-primary-600 hover:underline">
                {{ 'transfer_assistance.info.manage_accreditations' | translate }}<i class="pi pi-arrow-right" aria-hidden="true"></i>
              </a>
            }
          </section>
        }

        <section class="grid gap-4 md:grid-cols-2">
          <article class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
            <h2 class="flex items-center gap-2 text-lg font-semibold text-gray-900">
              <i class="pi pi-user-plus text-primary-600" aria-hidden="true"></i>{{ 'transfer_assistance.info.helper_title' | translate }}
            </h2>
            <p class="mt-2 text-sm text-gray-600">{{ 'transfer_assistance.info.helper_desc' | translate }}</p>
          </article>
          <article class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
            <h2 class="flex items-center gap-2 text-lg font-semibold text-gray-900">
              <i class="pi pi-eye text-primary-600" aria-hidden="true"></i>{{ 'transfer_assistance.info.witness_title' | translate }}
            </h2>
            <p class="mt-2 text-sm text-gray-600">{{ 'transfer_assistance.info.witness_desc' | translate }}</p>
          </article>
        </section>

        <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.who_title' | translate }}</h2>
          <p class="mt-2 text-sm text-gray-600">{{ 'transfer_assistance.info.who_desc' | translate }}</p>
          <p class="mt-2 text-sm font-medium text-gray-800">{{ 'transfer_assistance.info.who_admin_excluded' | translate }}</p>
          <h3 class="mt-4 font-semibold text-gray-900">{{ 'transfer_assistance.info.conditions_title' | translate }}</h3>
          <ul class="mt-2 list-disc space-y-1 pl-5 text-sm text-gray-600">
            <li>{{ 'transfer_assistance.info.cond_active' | translate }}</li>
            <li>{{ 'transfer_assistance.info.cond_email' | translate }}</li>
            <li data-testid="mfa-condition">{{ mfaConditionKey() | translate }}</li>
            <li>{{ 'transfer_assistance.info.cond_accreditation' | translate }}</li>
            <li>{{ 'transfer_assistance.info.cond_single' | translate }}</li>
          </ul>
        </section>

        <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.validation_title' | translate }}</h2>
          <p class="mt-2 text-sm text-gray-600">{{ 'transfer_assistance.info.validation_desc' | translate }}</p>
          @if (rules()?.verifier_roles?.length) {
            <p class="mt-2 text-sm text-gray-700">{{ 'transfer_assistance.info.validation_roles' | translate }}</p>
            <div class="mt-2 flex flex-wrap gap-2" data-testid="verifier-roles">
              @for (role of rules()!.verifier_roles; track role) {
                <p-tag severity="info" [value]="('roles.' + role) | translate" />
              }
            </div>
          }
          <p class="mt-2 text-sm text-gray-600">{{ 'transfer_assistance.info.validation_self' | translate }}</p>
        </section>

        <section class="grid gap-4 md:grid-cols-2">
          <article class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.helper_rules_title' | translate }}</h2>
            <ul class="mt-2 list-disc space-y-1 pl-5 text-sm text-gray-600">
              <li>{{ 'transfer_assistance.info.helper_rule_conflict' | translate }}</li>
              <li>{{ 'transfer_assistance.info.helper_rule_one_offer' | translate }}</li>
              <li>{{ 'transfer_assistance.info.helper_rule_recheck' | translate }}</li>
            </ul>
          </article>
          <article class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
            <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.witness_rules_title' | translate }}</h2>
            <ul class="mt-2 list-disc space-y-1 pl-5 text-sm text-gray-600">
              <li>{{ 'transfer_assistance.info.witness_rule_independent' | translate }}</li>
              <li>{{ 'transfer_assistance.info.witness_rule_one_pending' | translate }}</li>
              <li>{{ 'transfer_assistance.info.witness_rule_recheck' | translate }}</li>
              @if (rules(); as r) {
                <li data-testid="invitation-window">{{ 'transfer_assistance.info.witness_rule_window' | translate: { days: r.invitation_window_days } }}</li>
              }
            </ul>
          </article>
        </section>

        <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.info.steps_title' | translate }}</h2>
          <ol class="mt-3 space-y-3">
            @for (step of steps; track step; let i = $index) {
              <li class="flex gap-3 text-sm text-gray-700">
                <span class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary-100 text-xs font-semibold text-primary-700">{{ i + 1 }}</span>
                <span>{{ ('transfer_assistance.info.step_' + step) | translate }}</span>
              </li>
            }
          </ol>
          @if (rules(); as r) {
            <div class="mt-4 space-y-1 text-sm text-gray-600">
              <p data-testid="matching-window">{{ 'transfer_assistance.info.matching_window' | translate: { days: r.matching_window_days } }}</p>
              @if (r.requests_open) {
                <p data-testid="amount-range">{{ 'transfer_assistance.info.amount_range' | translate: {
                  min: (r.minimum_request_amount | number: '1.0-2'),
                  max: (r.maximum_request_amount | number: '1.0-2'),
                  currency: r.amount_currency } }}</p>
              } @else {
                <p data-testid="requests-closed" class="text-amber-800">{{ 'transfer_assistance.info.requests_closed' | translate }}</p>
              }
            </div>
          }
          <div class="mt-4 rounded-lg bg-gray-50 p-3 text-sm text-gray-700" data-testid="open-beneficiary-info">
            <h3 class="font-medium text-gray-900">{{ 'transfer_assistance.info.open_beneficiary_title' | translate }}</h3>
            <p class="mt-1">{{ 'transfer_assistance.info.open_beneficiary_desc' | translate }}</p>
          </div>
        </section>

        <section class="rounded-2xl border border-amber-100 bg-amber-50 p-5">
          <h2 class="flex items-center gap-2 font-semibold text-amber-900">
            <i class="pi pi-info-circle" aria-hidden="true"></i>{{ 'transfer_assistance.info.reminder_title' | translate }}
          </h2>
          <p class="mt-2 text-sm text-amber-900">{{ 'transfer_assistance.info.reminder_desc' | translate }}</p>
        </section>
      }
    </main>
  `,
})
export class TransferAssistanceInfoPage implements OnInit {
  private readonly api = inject(TransferAssistanceApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly featureEnabled = environment.features.transferAssistance;
  readonly steps = ['accreditation', 'validation', 'matching', 'handover', 'confirmation'] as const;
  readonly loading = signal(true);
  readonly rules = signal<TransferAssistanceRules | null>(null);
  readonly profile = signal<TransferAssistanceProfile | null>(null);

  readonly mfaConditionKey = computed(() => {
    const r = this.rules();
    if (!r) return 'transfer_assistance.info.cond_mfa_if_required';
    return r.require_mfa ? 'transfer_assistance.info.cond_mfa_required' : 'transfer_assistance.info.cond_mfa_optional';
  });

  readonly checklist = computed<CheckItem[]>(() => {
    const r = this.rules();
    if (!r) return [];
    const me = r.my_eligibility;
    const items: CheckItem[] = [
      { key: 'active', ok: me.member_active },
      { key: 'email', ok: me.email_verified },
    ];
    if (r.require_mfa) items.push({ key: 'mfa', ok: me.mfa_enabled });
    return items;
  });

  ngOnInit(): void {
    if (!this.featureEnabled) {
      this.loading.set(false);
      return;
    }
    forkJoin({
      rules: this.api.getRules().pipe(catchError(() => of(null))),
      profile: this.api.getProfile().pipe(catchError(() => of(null))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ rules, profile }) => {
        this.rules.set(rules);
        this.profile.set(profile);
        this.loading.set(false);
      });
  }

  severity(status: TransferAssistanceProfile['helper_verification_status']): 'success' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'verified': return 'success';
      case 'pending': return 'warn';
      case 'rejected': return 'danger';
      default: return 'secondary';
    }
  }
}
