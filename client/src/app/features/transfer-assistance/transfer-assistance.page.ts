import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, OnInit, signal, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { catchError, forkJoin, map, merge, Observable, of, Subject, Subscription, timer } from 'rxjs';
import { finalize, switchMap, takeUntil } from 'rxjs/operators';
import { AuthStore } from '@core/auth/auth.store';
import { COUNTRIES } from '@core/data/countries';
import { environment } from '@env/environment';
import {
  TransferAssistanceAssignment,
  TransferAssistanceProfile,
  TransferAssistanceProfileReviewItem,
  TransferAssistanceBeneficiaryOption,
  TransferAssistanceAvailableRequest,
  TransferAssistanceInvitation,
  TransferAssistanceOffer,
  TransferAssistanceReviewCase,
  TransferAssistanceEvent,
  TransferAssistanceReceiveMethod,
  TransferAssistanceRequest,
  TransferAssistanceBeneficiaryCandidate,
  TransferAssistanceBeneficiaryOpenRequest,
  TransferAssistanceSyncSection,
} from '@models/transfer-assistance.model';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';
import { TransferAssistanceSyncService } from './transfer-assistance-sync.service';

const REVIEW_PERMISSION = 'transfer_assistance.review';
const CREATE_PERMISSION = 'transfer_assistance.create';
const PROFILE_PERMISSION = 'transfer_assistance.profile_manage';
const VERIFY_PERMISSION = 'transfer_assistance.verify_users';
const AVAILABLE_PERMISSION = 'transfer_assistance.view_available';
const OFFER_PERMISSION = 'transfer_assistance.offer';
const PARTICIPATE_PERMISSION = 'transfer_assistance.participate';
const RECEIPT_PERMISSION = 'transfer_assistance.confirm_receipt';
const DISPUTE_PERMISSION = 'transfer_assistance.open_dispute';
const RESOLVE_PERMISSION = 'transfer_assistance.resolve_dispute';
const SUSPEND_PERMISSION = 'transfer_assistance.suspend';
const READ_OWN_PERMISSION = 'transfer_assistance.read_own';
const ACTIVE_CASE_STATUSES = new Set(['submitted', 'matching', 'witness_pending', 'meeting_scheduled', 'transfer_in_progress', 'transfer_sent', 'disputed']);

@Component({
  selector: 'app-transfer-assistance-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, DatePipe, ButtonModule, InputTextModule, SelectModule, ConfirmDialog],
  providers: [ConfirmationService, TransferAssistanceSyncService],
  template: `
    <p-confirmdialog />
    <main class="min-h-full space-y-6 bg-gray-50 p-4 md:p-8">
      <header class="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ 'transfer_assistance.title' | translate }}</h1>
          <p class="mt-1 max-w-3xl text-sm text-gray-600">{{ 'transfer_assistance.disclaimer' | translate }}</p>
        </div>
        @if (featureEnabled && !sessionLoading()) {
          <div class="flex items-center gap-2 text-xs text-gray-500">
            @if (sync.lastSyncedAt(); as syncedAt) {
              <span data-testid="sync-last-updated">{{ 'transfer_assistance.sync.last_updated' | translate: { time: (syncedAt | date:'mediumTime') } }}</span>
            }
            <p-button icon="pi pi-refresh" [text]="true" size="small" data-testid="sync-refresh"
              [ariaLabel]="'transfer_assistance.sync.refresh' | translate"
              [label]="'transfer_assistance.sync.refresh' | translate"
              [disabled]="loading()" (onClick)="refresh()" />
          </div>
        }
      </header>
      <p class="sr-only" aria-live="polite" data-testid="sync-announcement">{{ syncAnnouncement() | translate }}</p>

      @if (!featureEnabled) {
        <p role="status" class="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
          {{ 'transfer_assistance.feature_disabled' | translate }}
        </p>
      } @else {
      @if (error()) {
        <p role="alert" class="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
          {{ error() | translate }}
        </p>
      }

      @if (sessionLoading()) {
        <p role="status">{{ 'transfer_assistance.session_loading' | translate }}</p>
      } @else if (!hasMemberEntity()) {
        <p role="status">{{ 'transfer_assistance.member_account_required' | translate }}</p>
      } @else if (!canCreateRequest()) {
        <p role="status">{{ 'transfer_assistance.create_permission_required' | translate }}</p>
      }

      @if (!sessionLoading() && hasMemberEntity() && !hasContactEmail()) {
        <p role="status" data-testid="contact-email-banner"
          class="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-900">
          <i class="pi pi-envelope mr-1" aria-hidden="true"></i>{{ 'transfer_assistance.contact_email_missing' | translate }}
          <a routerLink="/profile" class="ml-1 font-medium underline">{{ 'transfer_assistance.contact_email_link' | translate }}</a>
        </p>
      }

      @if (!sessionLoading() && canCreateRequest()) {
        <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.new_request' | translate }}</h2>
          <p class="mt-1 text-sm text-gray-500">{{ 'transfer_assistance.no_financial_details' | translate }}</p>

          <form class="mt-5 grid gap-4 md:grid-cols-2" [formGroup]="form" (ngSubmit)="createAndSubmit()">
            <label class="flex items-start gap-2 text-sm text-gray-700 md:col-span-2">
              <input type="checkbox" formControlName="beneficiary_later" data-testid="beneficiary-later" class="mt-1" />
              <span>
                <span class="font-medium">{{ 'transfer_assistance.beneficiary_later' | translate }}</span>
                <span class="block text-xs text-gray-500">{{ 'transfer_assistance.beneficiary_later_hint' | translate }}</span>
              </span>
            </label>
            @if (!beneficiaryLater()) {
            <div class="grid gap-1 text-sm text-gray-700">
              <label for="beneficiary-search">{{ 'transfer_assistance.beneficiary_search' | translate }}</label>
              <input id="beneficiary-search" pInputText
                [value]="beneficiarySearchValue()"
                (input)="onBeneficiarySearch($event)" autocomplete="off" />
              <p-select formControlName="beneficiary_member_id"
                [options]="beneficiaryOptions()"
                optionLabel="member_name"
                optionValue="member_id"
                [placeholder]="'transfer_assistance.select_beneficiary' | translate"
                appendTo="body"
                styleClass="w-full" />
            </div>
            }
            <div class="flex flex-col gap-1 text-sm text-gray-700">
              <label for="destination-country">{{ 'transfer_assistance.destination_country' | translate }}</label>
              <p-select inputId="destination-country" formControlName="destination_country"
                [options]="countryOptions()"
                optionLabel="label"
                optionValue="code"
                [filter]="true"
                filterBy="label"
                [placeholder]="'transfer_assistance.country_placeholder' | translate"
                appendTo="body"
                styleClass="w-full"
                (onChange)="onDestinationCountryChange($event.value)" />
            </div>
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.amount_mur' | translate }}
              <input pInputText type="number" min="0.01" step="0.01" formControlName="amount_mur" />
            </label>
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.receive_currency' | translate }}
              <input pInputText formControlName="receive_currency" readonly />
            </label>
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.receive_method' | translate }}
              <p-select formControlName="receive_method"
                [options]="receiveMethodOptions"
                optionLabel="label"
                optionValue="value"
                appendTo="body"
                styleClass="w-full">
                <ng-template #item let-option>
                  {{ option.label | translate }}
                </ng-template>
                <ng-template #selectedItem let-option>
                  {{ option.label | translate }}
                </ng-template>
              </p-select>
            </label>
            @if (!beneficiaryLater()) {
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.beneficiary_relationship' | translate }}
              <input pInputText formControlName="beneficiary_relationship" maxlength="100" />
            </label>
            }
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.available_from' | translate }}
              <input pInputText type="date" formControlName="available_from" />
            </label>
            <label class="grid gap-1 text-sm text-gray-700">
              {{ 'transfer_assistance.available_to' | translate }}
              <input pInputText type="date" formControlName="available_to" />
            </label>
            <label class="grid gap-1 text-sm text-gray-700 md:col-span-2">
              {{ 'transfer_assistance.purpose' | translate }}
              <textarea pInputText rows="2" maxlength="500" formControlName="purpose"></textarea>
            </label>
            <label class="grid gap-1 text-sm text-gray-700 md:col-span-2">
              {{ 'transfer_assistance.meeting_area' | translate }}
              <input pInputText formControlName="meeting_area" maxlength="200" />
            </label>
            <div class="flex flex-wrap gap-2 md:col-span-2">
              <p-button type="button" severity="secondary" [outlined]="true"
                [label]="'transfer_assistance.save_draft' | translate"
                [loading]="saving()" [disabled]="form.invalid || saving()"
                (onClick)="saveDraft()" />
              <p-button type="submit" icon="pi pi-send"
                [label]="(saving() ? 'transfer_assistance.saving' : 'transfer_assistance.submit_request') | translate"
                [loading]="saving()" [disabled]="form.invalid || saving()" />
            </div>
          </form>
        </section>
      }

      @if (isOrgAdmin()) {
        <p role="status" class="rounded-lg bg-blue-50 p-4 text-sm text-blue-900">
          {{ 'transfer_assistance.admin_participation_forbidden' | translate }}
          <a routerLink="/transfer-assistance/info" class="ml-1 font-medium underline">{{ 'transfer_assistance.info.link' | translate }}</a>
        </p>
      }
      @if (canManageProfile()) {
        <section class="rounded-2xl border border-gray-100 bg-white p-5 shadow-sm">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.participate_title' | translate }}</h2>
          <p class="mt-1 text-sm text-gray-500">{{ 'transfer_assistance.manual_verification' | translate }}</p>
          <a routerLink="/transfer-assistance/info" data-testid="info-link-accreditation"
            class="mt-1 inline-flex items-center gap-1 text-sm text-primary-600 hover:underline">
            <i class="pi pi-question-circle" aria-hidden="true"></i>{{ 'transfer_assistance.info.link' | translate }}
          </a>
          <div class="mt-4 grid gap-3 sm:grid-cols-2">
            @for (participation of participationRoles(); track participation.role) {
              <div class="space-y-3 rounded-lg border border-gray-200 p-3 text-sm">
                <p class="font-medium text-gray-800">{{ ('transfer_assistance.' + participation.role + '_role') | translate }}</p>
                <p class="text-gray-500">{{ ('transfer_assistance.verification_' + participation.status) | translate }}</p>
                @if (!participation.optedIn) {
                  <p-button [label]="'transfer_assistance.request_accreditation' | translate"
                    [disabled]="loading() || profileSaving()" (onClick)="changeAccreditation(participation.role, 'request')" />
                } @else {
                  <p-button severity="secondary" [outlined]="true"
                    [label]="(participation.status === 'verified' ? 'transfer_assistance.withdraw_participation' : 'transfer_assistance.cancel_accreditation') | translate"
                    [disabled]="loading() || profileSaving()" (onClick)="changeAccreditation(participation.role, 'cancel')" />
                }
              </div>
            }
          </div>
        </section>
      }

      <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
        <div class="border-b border-gray-100 p-5">
          <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.my_requests' | translate }}</h2>
        </div>
        @if (loading()) {
          <p class="p-6 text-sm text-gray-500">{{ 'common.loading' | translate }}</p>
        } @else if (!requests().length) {
          <p class="p-6 text-sm text-gray-500">{{ 'transfer_assistance.no_requests' | translate }}</p>
        } @else {
          <div class="divide-y divide-gray-100">
            @for (request of requests(); track request.id) {
              <article class="grid gap-2 p-5 md:grid-cols-[1fr_auto]">
                <div>
                  <p class="font-medium text-gray-900">
                    {{ request.destination_country }} · {{ request.amount_mur }} MUR
                  </p>
                  <p class="text-sm text-gray-600">
                    {{ request.receive_currency }} · {{ ('transfer_assistance.' + request.receive_method) | translate }}
                  </p>
                  @if (request.beneficiary_pending) {
                    <p class="mt-1 inline-flex items-center gap-1 rounded-full bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-800"
                      data-testid="beneficiary-pending-badge">
                      <i class="pi pi-user-plus" aria-hidden="true"></i>
                      {{ 'transfer_assistance.beneficiary_pending' | translate: { count: request.beneficiary_candidate_count ?? 0 } }}
                    </p>
                    @if (canDesignateStatus(request.status)) {
                      <button type="button" (click)="loadCandidates(request)" data-testid="view-candidates"
                        class="mt-2 block rounded-lg bg-amber-50 px-3 py-2 text-sm font-medium text-amber-800">
                        {{ 'transfer_assistance.view_candidates' | translate }}
                      </button>
                      @if (hasLoadedCandidates(request.id)) {
                        @for (candidate of candidatesFor(request.id); track candidate.id; let first = $first) {
                          <div class="mt-2 rounded-lg border p-3 text-sm"
                            [class.border-emerald-300]="first && candidate.status === 'pending'"
                            [class.bg-emerald-50]="first && candidate.status === 'pending'"
                            [class.border-gray-200]="!(first && candidate.status === 'pending')">
                            <p class="font-medium">
                              {{ candidate.member_name }}
                              @if (first && candidate.status === 'pending') {
                                <span class="ml-1 text-xs text-emerald-700">{{ 'transfer_assistance.first_candidate' | translate }}</span>
                              }
                            </p>
                            <p class="text-xs text-gray-500">{{ candidate.created_at | date:'short' }}</p>
                            @if (candidate.message) {
                              <p class="text-gray-600">{{ candidate.message }}</p>
                            }
                            @if (candidate.status === 'pending') {
                              <button type="button" (click)="designateCandidate(request, candidate)"
                                class="mt-2 rounded-lg bg-emerald-700 px-3 py-2 text-white" [disabled]="saving()">
                                {{ 'transfer_assistance.designate_beneficiary' | translate }}
                              </button>
                            } @else {
                              <p class="text-xs text-gray-500">{{ ('transfer_assistance.candidate_status_' + candidate.status) | translate }}</p>
                            }
                          </div>
                        } @empty {
                          <p class="mt-2 text-sm text-gray-500">{{ 'transfer_assistance.no_candidates' | translate }}</p>
                        }
                      }
                    }
                  }
                  @if (request.review_reason) {
                    <p class="mt-2 text-sm text-red-700">{{ request.review_reason }}</p>
                  }
                  @if (request.status === 'draft') {
                    <button type="button" (click)="editDraft(request)"
                      class="mt-2 rounded-lg bg-gray-100 px-3 py-2 text-sm font-medium text-gray-700">
                      {{ 'transfer_assistance.edit_draft' | translate }}
                    </button>
                    <button type="button" (click)="submitExisting(request)"
                      class="mt-2 rounded-lg bg-blue-50 px-3 py-2 text-sm font-medium text-blue-700">
                      {{ 'transfer_assistance.submit_request' | translate }}
                    </button>
                  }
                  @if (request.status === 'matching') {
                    <button type="button" (click)="loadOffers(request)"
                      class="mt-2 rounded-lg bg-indigo-50 px-3 py-2 text-sm font-medium text-indigo-700">
                      {{ 'transfer_assistance.view_offers' | translate }}
                    </button>
                    @if (hasLoadedOffers(request.id)) {
                      @for (offer of offersFor(request.id); track offer.id) {
                        <div class="mt-2 rounded-lg border border-gray-200 p-3 text-sm">
                          <p class="font-medium">{{ offer.helper_name }}</p>
                          <p>{{ offer.availability }}</p>
                          <p class="text-gray-600">{{ offer.message }}</p>
                          @if (offer.status === 'pending') {
                            <button type="button" (click)="selectOffer(request, offer)"
                              class="mt-2 rounded-lg bg-emerald-700 px-3 py-2 text-white">
                              {{ 'transfer_assistance.select_helper' | translate }}
                            </button>
                          }
                        </div>
                      } @empty {
                        <p class="mt-2 text-sm text-gray-500">{{ 'transfer_assistance.no_offers' | translate }}</p>
                      }
                    }
                  }
                  @if (request.status === 'witness_pending' && !request.beneficiary_member_id) {
                    <p class="mt-3 text-sm text-amber-800" data-testid="witness-requires-beneficiary">
                      {{ 'transfer_assistance.beneficiary_required_before_witness' | translate }}
                    </p>
                  }
                  @if (request.status === 'witness_pending' && request.beneficiary_member_id) {
                    <div class="mt-3 grid gap-2 md:max-w-lg">
                      <label class="grid gap-1 text-sm">
                        {{ 'transfer_assistance.search_witness' | translate }}
                        <input class="rounded-lg border border-gray-300 px-3 py-2"
                          (input)="onWitnessSearch(request.id, $event)" autocomplete="off" />
                      </label>
                      <a routerLink="/transfer-assistance/info" class="text-xs text-primary-600 hover:underline">
                        {{ 'transfer_assistance.info.link_witness' | translate }}
                      </a>
                      @for (witness of witnessOptions(); track witness.member_id) {
                        <button type="button" (click)="inviteWitness(request, witness)"
                          class="rounded-lg border border-gray-200 px-3 py-2 text-left text-sm hover:bg-gray-50">
                          {{ 'transfer_assistance.invite_witness' | translate }}: {{ witness.member_name }}
                        </button>
                      }
                    </div>
                  }
                  @if (request.status === 'witness_pending' && request.witness_member_id) {
                    <label class="mt-3 grid gap-1 text-sm md:max-w-sm">
                      {{ 'transfer_assistance.meeting_time' | translate }}
                      <input type="datetime-local" class="rounded-lg border border-gray-300 px-3 py-2"
                        [value]="meetingTimeFor(request.id)"
                        (input)="setInputValue('meetingTimes', request.id, $event)" />
                    </label>
                    <button type="button" (click)="scheduleMeeting(request)"
                      class="mt-2 rounded-lg bg-blue-700 px-3 py-2 text-sm text-white">
                      {{ 'transfer_assistance.schedule_meeting' | translate }}
                    </button>
                  }
                  @if (request.status === 'meeting_scheduled' && request.meeting_at) {
                    <p class="mt-2 text-sm text-gray-600">
                      {{ 'transfer_assistance.meeting_time' | translate }}: {{ request.meeting_at | date:'medium' }}
                    </p>
                    @if (!request.requester_handover_confirmed && isMeetingTimeReached(request.meeting_at)) {
                      <button type="button" (click)="confirmHandover(request.id)"
                        class="mt-2 rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                        {{ 'transfer_assistance.confirm_handover' | translate }}
                      </button>
                    }
                  }
                  @if (request.status === 'draft' || request.status === 'submitted' ||
                       request.status === 'matching' || request.status === 'witness_pending' ||
                       request.status === 'meeting_scheduled') {
                    <button type="button" (click)="cancel(request)"
                      class="mt-2 ml-2 rounded-lg bg-gray-100 px-3 py-2 text-sm font-medium text-gray-700">
                      {{ 'transfer_assistance.cancel_request' | translate }}
                    </button>
                  }
                  @if (canOpenDispute() && canDisputeStatus(request.status)) {
                    <label class="mt-3 grid gap-1 text-sm">
                      {{ 'transfer_assistance.dispute_reason' | translate }}
                      <input class="rounded-lg border border-gray-300 px-3 py-2"
                        [value]="reasonFor(request.id)"
                        (input)="setInputValue('workflowReasons', request.id, $event)" maxlength="1000" />
                    </label>
                    <button type="button" (click)="openDispute(request.id)"
                      class="mt-2 rounded-lg border border-red-300 px-3 py-2 text-sm text-red-700">
                      {{ 'transfer_assistance.open_dispute' | translate }}
                    </button>
                  }
                  <button type="button" (click)="loadEvents(request.id)"
                    class="mt-2 rounded-lg px-3 py-2 text-sm text-gray-600 underline">
                    {{ 'transfer_assistance.view_timeline' | translate }}
                  </button>
                  @if (eventsFor(request.id).length) {
                    <ol class="mt-2 space-y-1 text-xs text-gray-500">
                      @for (event of eventsFor(request.id); track event.created_at + event.event_type) {
                        <li>{{ event.created_at | date:'short' }} — {{ ('transfer_assistance.event_' + event.event_type) | translate }}</li>
                      }
                    </ol>
                  }
                </div>
                <p class="mt-2 rounded-lg bg-blue-50 p-3 text-sm text-blue-900" data-testid="request-next-action">
                  {{ nextActionForRequest(request) | translate }}
                </p>
                <span class="h-fit rounded-full bg-blue-50 px-3 py-1 text-xs font-semibold text-blue-800">
                  {{ ('transfer_assistance.status_' + request.status) | translate }}
                </span>
              </article>
            }
          </div>
        }
      </section>

      @if (canReview()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.review_queue' | translate }}</h2>
          </div>
          @if (!reviewQueue().length) {
            <p class="p-6 text-sm text-gray-500">{{ 'transfer_assistance.no_reviews' | translate }}</p>
          } @else {
            <div class="divide-y divide-gray-100">
              @for (request of reviewQueue(); track request.id) {
                <article class="space-y-3 p-5">
                  <div class="flex flex-wrap items-start justify-between gap-2">
                    <div>
                      <p class="font-medium text-gray-900">{{ request.beneficiary_name || ('transfer_assistance.beneficiary_to_designate' | translate) }}</p>
                      <p class="text-sm text-gray-600">
                        {{ request.destination_country }} · {{ request.amount_mur }} MUR ·
                        {{ ('transfer_assistance.' + request.receive_method) | translate }}
                      </p>
                      <p class="mt-1 text-xs text-gray-500">{{ request.purpose }}</p>
                    </div>
                    <span class="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-800">
                      {{ ('transfer_assistance.status_' + request.status) | translate }}
                    </span>
                  </div>
                  <label class="grid gap-1 text-sm text-gray-700">
                    {{ 'transfer_assistance.verification_reason' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="reviewReasonFor(request.id)"
                      (input)="onReviewReasonInput(request.id, $event)" maxlength="1000" />
                  </label>
                  <div class="flex flex-wrap gap-2">
                    <button type="button" (click)="review(request, 'approve')"
                      class="rounded-lg bg-emerald-700 px-3 py-2 text-sm font-medium text-white">
                      {{ 'transfer_assistance.approve' | translate }}
                    </button>
                    <button type="button" (click)="review(request, 'reject')"
                      class="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                      {{ 'transfer_assistance.reject' | translate }}
                    </button>
                    <button type="button" (click)="review(request, 'suspend')"
                      class="rounded-lg border border-amber-300 px-3 py-2 text-sm font-medium text-amber-800">
                      {{ 'transfer_assistance.suspend_request' | translate }}
                    </button>
                  </div>
                </article>
              }
            </div>
          }
        </section>
      }

      @if (canBrowseBeneficiaryOpen()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm" data-testid="beneficiary-open-section">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold">{{ 'transfer_assistance.beneficiary_open_title' | translate }}</h2>
            <p class="mt-1 text-sm text-gray-500">{{ 'transfer_assistance.beneficiary_open_hint' | translate }}</p>
          </div>
          @if (!beneficiaryOpenRequests().length) {
            <p class="p-5 text-sm text-gray-500">{{ 'transfer_assistance.no_beneficiary_open' | translate }}</p>
          } @else {
            <div class="divide-y divide-gray-100">
              @for (open of beneficiaryOpenRequests(); track open.id) {
                <article class="space-y-2 p-5">
                  <p class="font-medium">{{ open.destination_country }} · {{ open.amount_mur }} MUR</p>
                  <p class="text-sm text-gray-600">{{ open.receive_currency }} ·
                    {{ ('transfer_assistance.' + open.receive_method) | translate }}</p>
                  <p class="text-sm">{{ open.purpose }}</p>
                  @if (open.expires_at) {
                    <p class="text-xs text-gray-500">{{ 'transfer_assistance.expires_at' | translate: { date: (open.expires_at | date:'short') } }}</p>
                  }
                  @if (open.my_candidate_status === 'pending') {
                    <p class="text-sm text-emerald-700">{{ 'transfer_assistance.candidate_status_pending' | translate }}</p>
                    <button type="button" (click)="withdrawCandidacy(open)" [disabled]="saving()"
                      class="rounded-lg border border-gray-300 px-3 py-2 text-sm text-gray-700">
                      {{ 'transfer_assistance.withdraw_candidacy' | translate }}
                    </button>
                  } @else {
                    <label class="grid gap-1 text-sm">
                      {{ 'transfer_assistance.candidacy_message' | translate }}
                      <input class="rounded-lg border border-gray-300 px-3 py-2" maxlength="300"
                        [value]="candidacyMessageFor(open.id)"
                        (input)="onCandidacyMessageInput(open.id, $event)" />
                    </label>
                    <button type="button" (click)="applyAsBeneficiary(open)" [disabled]="saving()"
                      class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                      {{ 'transfer_assistance.apply_as_beneficiary' | translate }}
                    </button>
                  }
                </article>
              }
            </div>
          }
        </section>
      }

      @if (canBrowseAvailable()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold">{{ 'transfer_assistance.available_requests' | translate }}</h2>
            <p class="mt-1 text-sm text-gray-500">{{ 'transfer_assistance.anonymous_requests' | translate }}</p>
          </div>
          @if (!availableRequests().length) {
            <p class="p-5 text-sm text-gray-500">{{ 'transfer_assistance.no_available_requests' | translate }}</p>
          } @else {
            <div class="divide-y divide-gray-100">
              @for (request of availableRequests(); track request.id) {
                <article class="space-y-3 p-5">
                  <p class="font-medium">{{ request.destination_country }} · {{ request.amount_mur }} MUR</p>
                  <p class="text-sm text-gray-600">{{ request.receive_currency }} ·
                    {{ ('transfer_assistance.' + request.receive_method) | translate }} · {{ request.meeting_area }}</p>
                  <p class="text-sm">{{ request.purpose }}</p>
                  <label class="grid gap-1 text-sm">
                    {{ 'transfer_assistance.availability_offer' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="offerInputFor(request.id, 'availability')"
                      (input)="setInputValue('offerAvailability', request.id, $event)" maxlength="200" />
                  </label>
                  <label class="grid gap-1 text-sm">
                    {{ 'transfer_assistance.offer_message' | translate }}
                    <textarea class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="offerInputFor(request.id, 'message')"
                      (input)="setInputValue('offerMessages', request.id, $event)" maxlength="500"></textarea>
                  </label>
                  <button type="button" (click)="submitOffer(request)"
                    class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                    {{ 'transfer_assistance.make_offer' | translate }}
                  </button>
                </article>
              }
            </div>
          }
        </section>
      }

      @if (canParticipate()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold">{{ 'transfer_assistance.witness_invitations' | translate }}</h2>
            <a routerLink="/transfer-assistance/info" class="mt-1 inline-block text-sm text-primary-600 hover:underline">
              {{ 'transfer_assistance.info.link_witness' | translate }}
            </a>
          </div>
          @if (!witnessInvitations().length) {
            <p class="p-5 text-sm text-gray-500">{{ 'transfer_assistance.no_witness_invitations' | translate }}</p>
          } @else {
            @for (invitation of witnessInvitations(); track invitation.invitation_id) {
              <article class="space-y-3 border-b border-gray-100 p-5">
                <p>{{ invitation.destination_country }} · {{ invitation.amount_mur }} MUR · {{ invitation.receive_currency }}</p>
                <label class="flex items-center gap-2 text-sm">
                  <input type="checkbox" [checked]="conflictFor(invitation.invitation_id)"
                    (change)="setConflict(invitation.invitation_id, $event)" />
                  {{ 'transfer_assistance.declare_conflict' | translate }}
                </label>
                @if (conflictFor(invitation.invitation_id)) {
                  <label class="grid gap-1 text-sm">
                    {{ 'transfer_assistance.conflict_reason' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="reasonFor(invitation.invitation_id)"
                      (input)="setInputValue('workflowReasons', invitation.invitation_id, $event)" maxlength="500" />
                  </label>
                }
                <div class="flex gap-2">
                  @if (!conflictFor(invitation.invitation_id)) {
                    <button type="button" (click)="respondToInvitation(invitation, true)"
                      class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                      {{ 'transfer_assistance.accept_invitation' | translate }}
                    </button>
                  }
                  <button type="button" (click)="respondToInvitation(invitation, false)"
                    class="rounded-lg border border-gray-300 px-3 py-2 text-sm">
                    {{ 'transfer_assistance.decline_invitation' | translate }}
                  </button>
                </div>
              </article>
            }
          }
        </section>

        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold">{{ 'transfer_assistance.my_assignments' | translate }}</h2>
          </div>
          @if (!assignments().length) {
            <p class="p-5 text-sm text-gray-500">{{ 'transfer_assistance.no_assignments' | translate }}</p>
          } @else {
            @for (assignment of assignments(); track assignment.request_id + assignment.role) {
              <article class="space-y-3 border-b border-gray-100 p-5">
                <p class="font-medium">{{ assignment.destination_country }} · {{ assignment.amount_mur }} MUR · {{ assignment.role }}</p>
                <p class="text-sm text-gray-600">{{ assignment.receive_currency }} ·
                  {{ ('transfer_assistance.' + assignment.receive_method) | translate }} · {{ assignment.meeting_area }}</p>
                <p class="text-sm">{{ assignment.purpose }}</p>
                <p class="text-sm font-semibold">{{ ('transfer_assistance.status_' + assignment.status) | translate }}</p>
                <p class="rounded-lg bg-blue-50 p-3 text-sm text-blue-900" data-testid="assignment-next-action">
                  {{ nextActionForAssignment(assignment) | translate }}
                </p>
                @if (assignment.meeting_at) {
                  <p class="text-sm">{{ 'transfer_assistance.meeting_time' | translate }}: {{ assignment.meeting_at | date:'medium' }}</p>
                }
                @if (assignment.role === 'helper' && assignment.status === 'witness_pending') {
                  <label class="grid gap-1 text-sm md:max-w-sm">
                    {{ 'transfer_assistance.meeting_time' | translate }}
                    <input type="datetime-local" class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="meetingTimeFor(assignment.request_id)"
                      (input)="setInputValue('meetingTimes', assignment.request_id, $event)" />
                  </label>
                  <button type="button" (click)="scheduleAssignment(assignment)"
                    class="rounded-lg bg-blue-700 px-3 py-2 text-sm text-white">
                    {{ 'transfer_assistance.schedule_meeting' | translate }}
                  </button>
                }
                @if (assignment.role === 'helper' && assignment.status === 'meeting_scheduled' &&
                     !assignment.helper_handover_confirmed && isMeetingTimeReached(assignment.meeting_at)) {
                  <button type="button" (click)="confirmHandover(assignment.request_id)"
                    class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                    {{ 'transfer_assistance.confirm_handover' | translate }}
                  </button>
                }
                @if (assignment.role === 'witness' && assignment.status === 'meeting_scheduled' &&
                     !assignment.witness_attested && isMeetingTimeReached(assignment.meeting_at)) {
                  <button type="button" (click)="attestWitness(assignment.request_id)"
                    class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                    {{ 'transfer_assistance.attest_handover' | translate }}
                  </button>
                }
                @if (assignment.role === 'helper' && assignment.status === 'transfer_in_progress') {
                  <label class="grid gap-1 text-sm md:max-w-sm">
                    {{ 'transfer_assistance.provider_name' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="evidenceValue(assignment.request_id, 'provider')"
                      (input)="setInputValue('providers', assignment.request_id, $event)" maxlength="100" />
                  </label>
                  <label class="grid gap-1 text-sm md:max-w-sm">
                    {{ 'transfer_assistance.provider_reference' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="evidenceValue(assignment.request_id, 'reference')"
                      (input)="setInputValue('evidenceReferences', assignment.request_id, $event)" maxlength="120" />
                  </label>
                  <button type="button" (click)="submitEvidence(assignment.request_id)"
                    class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                    {{ 'transfer_assistance.record_evidence' | translate }}
                  </button>
                }
                @if (assignment.role === 'beneficiary' && assignment.status === 'transfer_sent' &&
                     !assignment.beneficiary_confirmation) {
                  <label class="grid gap-1 text-sm">
                    {{ 'transfer_assistance.receipt_reason' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="reasonFor(assignment.request_id)"
                      (input)="setInputValue('workflowReasons', assignment.request_id, $event)" maxlength="1000" />
                  </label>
                  <div class="flex flex-wrap gap-2">
                    <button type="button" (click)="confirmReceipt(assignment, 'full')"
                      class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">{{ 'transfer_assistance.received_full' | translate }}</button>
                    <button type="button" (click)="confirmReceipt(assignment, 'partial')"
                      class="rounded-lg bg-amber-600 px-3 py-2 text-sm text-white">{{ 'transfer_assistance.received_partial' | translate }}</button>
                    <button type="button" (click)="confirmReceipt(assignment, 'not_received')"
                      class="rounded-lg bg-red-600 px-3 py-2 text-sm text-white">{{ 'transfer_assistance.not_received' | translate }}</button>
                  </div>
                }
                @if (assignment.dispute_reason) {
                  <p role="alert" class="text-sm text-red-700">{{ assignment.dispute_reason }}</p>
                }
                @if (canOpenDispute() && canDisputeStatus(assignment.status)) {
                  <label class="grid gap-1 text-sm">
                    {{ 'transfer_assistance.dispute_reason' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="reasonFor(assignment.request_id)"
                      (input)="setInputValue('workflowReasons', assignment.request_id, $event)" maxlength="1000" />
                  </label>
                  <button type="button" (click)="openDispute(assignment.request_id)"
                    class="rounded-lg border border-red-300 px-3 py-2 text-sm text-red-700">
                    {{ 'transfer_assistance.open_dispute' | translate }}
                  </button>
                }
              </article>
            }
          }
        </section>
      }

      @if (canVerifyProfiles()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold text-gray-900">{{ 'transfer_assistance.profile_review_queue' | translate }}</h2>
          </div>
          @if (!profileReviewQueue().length) {
            <p class="p-6 text-sm text-gray-500">{{ 'transfer_assistance.no_profile_reviews' | translate }}</p>
          } @else {
            <div class="divide-y divide-gray-100">
              @for (item of profileReviewQueue(); track item.member_id) {
                <article class="space-y-3 p-5">
                  <p class="font-medium text-gray-900">{{ item.member_name }}</p>
                  <label class="grid gap-1 text-sm text-gray-700">
                    {{ 'transfer_assistance.review_reason' | translate }}
                    <input class="rounded-lg border border-gray-300 px-3 py-2"
                      [value]="profileReviewReasonFor(item.member_id)"
                      (input)="onProfileReviewReasonInput(item.member_id, $event)" maxlength="1000" />
                  </label>
                  @if (item.helper_opted_in && item.helper_verification_status === 'pending') {
                    <div class="flex flex-wrap gap-2">
                      <button type="button" (click)="reviewProfile(item, 'helper', 'verify')"
                        class="rounded-lg bg-emerald-700 px-3 py-2 text-sm font-medium text-white">
                        {{ 'transfer_assistance.verify_helper' | translate }}
                      </button>
                      <button type="button" (click)="reviewProfile(item, 'helper', 'reject')"
                        class="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                        {{ 'transfer_assistance.reject_helper' | translate }}
                      </button>
                    </div>
                  }
                  @if (item.witness_opted_in && item.witness_verification_status === 'pending') {
                    <div class="flex flex-wrap gap-2">
                      <button type="button" (click)="reviewProfile(item, 'witness', 'verify')"
                        class="rounded-lg bg-emerald-700 px-3 py-2 text-sm font-medium text-white">
                        {{ 'transfer_assistance.verify_witness' | translate }}
                      </button>
                      <button type="button" (click)="reviewProfile(item, 'witness', 'reject')"
                        class="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                        {{ 'transfer_assistance.reject_witness' | translate }}
                      </button>
                    </div>
                  }
                </article>
              }
            </div>
          }
        </section>
      }
      @if (canResolveCases()) {
        <section class="rounded-2xl border border-gray-100 bg-white shadow-sm">
          <div class="border-b border-gray-100 p-5">
            <h2 class="text-lg font-semibold">{{ 'transfer_assistance.incident_queue' | translate }}</h2>
          </div>
          @for (item of reviewCases(); track item.id) {
            <article class="space-y-3 border-b border-gray-100 p-5">
              <p class="font-medium">{{ item.destination_country }} · {{ item.amount_mur }} MUR · {{ item.status }}</p>
              @if (item.dispute_reason || item.review_reason) {
                <p class="text-sm text-red-700">{{ item.dispute_reason || item.review_reason }}</p>
              }
              <label class="grid gap-1 text-sm">
                {{ 'transfer_assistance.resolution_reason' | translate }}
                <input class="rounded-lg border border-gray-300 px-3 py-2"
                  [value]="reasonFor(item.id)"
                  (input)="setInputValue('workflowReasons', item.id, $event)" maxlength="1000" />
              </label>
              @if (item.status === 'disputed') {
                <button type="button" (click)="resolveDispute(item, 'resume')"
                  class="rounded-lg bg-emerald-700 px-3 py-2 text-sm text-white">
                  {{ 'transfer_assistance.resume_for_confirmation' | translate }}
                </button>
                <button type="button" (click)="resolveDispute(item, 'cancel')"
                  class="ml-2 rounded-lg border border-gray-300 px-3 py-2 text-sm">
                  {{ 'transfer_assistance.close_dispute' | translate }}
                </button>
              } @else if (item.status === 'suspended' && canSuspend()) {
                <button type="button" (click)="resumeRequest(item)"
                  class="rounded-lg bg-blue-700 px-3 py-2 text-sm text-white">
                  {{ 'transfer_assistance.resume_request' | translate }}
                </button>
              }
            </article>
          } @empty {
            <p class="p-5 text-sm text-gray-500">{{ 'transfer_assistance.no_incidents' | translate }}</p>
          }
        </section>
      }
      }
    </main>
  `,
})
export class TransferAssistancePage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(TransferAssistanceApiService);
  private readonly auth = inject(AuthStore);
  private readonly confirmation = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly beneficiarySearch = new Subject<string>();
  private readonly witnessSearch = new Subject<{ requestId: string; value: string }>();
  private loadSubscription?: Subscription;
  private readonly contextChanged = new Subject<void>();
  readonly sync = inject(TransferAssistanceSyncService);
  private sectionSubscription?: Subscription;

  readonly beneficiaryOpenRequests = signal<TransferAssistanceBeneficiaryOpenRequest[]>([]);
  readonly candidatesByRequest = signal<Record<string, TransferAssistanceBeneficiaryCandidate[]>>({});
  readonly candidacyMessages = signal<Record<string, string>>({});
  readonly syncAnnouncement = signal('');

  readonly beneficiaryOptions = signal<TransferAssistanceBeneficiaryOption[]>([]);
  readonly beneficiarySearchValue = signal('');
  readonly countryOptions = signal(this.buildCountryOptions());
  readonly receiveMethodOptions = [
    { value: 'bank_transfer' as const, label: 'transfer_assistance.bank_transfer' },
    { value: 'mobile_money' as const, label: 'transfer_assistance.mobile_money' },
    { value: 'orange_money' as const, label: 'transfer_assistance.orange_money' },
  ];
  readonly witnessOptions = signal<TransferAssistanceBeneficiaryOption[]>([]);
  readonly requests = signal<TransferAssistanceRequest[]>([]);
  readonly reviewQueue = signal<TransferAssistanceRequest[]>([]);
  readonly profileReviewQueue = signal<TransferAssistanceProfileReviewItem[]>([]);
  readonly availableRequests = signal<TransferAssistanceAvailableRequest[]>([]);
  readonly assignments = signal<TransferAssistanceAssignment[]>([]);
  readonly witnessInvitations = signal<TransferAssistanceInvitation[]>([]);
  readonly reviewCases = signal<TransferAssistanceReviewCase[]>([]);
  readonly offersByRequest = signal<Record<string, TransferAssistanceOffer[]>>({});
  readonly loadedOffers = signal<Record<string, boolean>>({});
  readonly eventsByRequest = signal<Record<string, TransferAssistanceEvent[]>>({});
  readonly profile = signal<TransferAssistanceProfile | null>(null);
  readonly reviewReasons = signal<Record<string, string>>({});
  readonly profileReviewReasons = signal<Record<string, string>>({});
  readonly offerAvailability = signal<Record<string, string>>({});
  readonly offerMessages = signal<Record<string, string>>({});
  readonly meetingTimes = signal<Record<string, string>>({});
  readonly providers = signal<Record<string, string>>({});
  readonly evidenceReferences = signal<Record<string, string>>({});
  readonly workflowReasons = signal<Record<string, string>>({});
  readonly conflicts = signal<Record<string, boolean>>({});
  readonly editingRequestId = signal<string | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly profileSaving = signal(false);
  readonly error = signal('');
  readonly sessionLoading = computed(() => this.auth.isLoading() || !this.auth.user());
  readonly canReview = computed(() => this.auth.hasPermission(REVIEW_PERMISSION));
  readonly hasMemberEntity = computed(() =>
    ['member', 'board_member'].includes(this.auth.user()?.entity_type ?? '') && !!this.auth.user()?.entity_id);
  readonly isOrgAdmin = computed(() => this.auth.user()?.roles.includes('org_admin') ?? false);
  readonly canManageProfile = computed(() => !this.isOrgAdmin() && this.hasMemberEntity() && this.auth.hasPermission(PROFILE_PERMISSION));
  readonly participationRoles = computed(() => (['helper', 'witness'] as const).map(role => ({
    role,
    optedIn: this.profile()?.[`${role}_opted_in`] ?? false,
    status: this.profile()?.[`${role}_verification_status`] ?? 'not_requested',
  })));
  readonly canVerifyProfiles = computed(() => this.auth.hasPermission(VERIFY_PERMISSION));
  readonly canBrowseAvailable = computed(() => !this.isOrgAdmin() && this.hasMemberEntity() && this.auth.hasPermission(AVAILABLE_PERMISSION) &&
    this.auth.hasPermission(OFFER_PERMISSION));
  readonly canParticipate = computed(() => this.hasMemberEntity() && this.auth.hasPermission(PARTICIPATE_PERMISSION));
  readonly canOpenDispute = computed(() => this.hasMemberEntity() && this.auth.hasPermission(DISPUTE_PERMISSION));
  readonly canCreateRequest = computed(() => this.hasMemberEntity() && this.auth.hasPermission(CREATE_PERMISSION));
  readonly canResolveCases = computed(() => this.auth.hasPermission(RESOLVE_PERMISSION));
  readonly canSuspend = computed(() => this.auth.hasPermission(SUSPEND_PERMISSION));
  readonly canBrowseBeneficiaryOpen = computed(() => !this.isOrgAdmin() && this.hasMemberEntity() &&
    this.auth.hasPermission(READ_OWN_PERMISSION));
  readonly hasContactEmail = computed(() => !!this.auth.user()?.contact_email?.trim());
  readonly hasActiveCase = computed(() =>
    this.requests().some(request => ACTIVE_CASE_STATUSES.has(request.status)) ||
    this.assignments().length > 0 || this.witnessInvitations().length > 0);
  readonly featureEnabled = environment.features.transferAssistance;
  readonly form = this.fb.nonNullable.group({
    beneficiary_later: [false],
    beneficiary_member_id: ['', Validators.required],
    destination_country: ['', [Validators.required, Validators.pattern(/^[a-zA-Z]{2}$/)]],
    amount_mur: [0, [Validators.required, Validators.min(0.01)]],
    receive_currency: ['', [Validators.required, Validators.pattern(/^[a-zA-Z]{3}$/)]],
    receive_method: ['mobile_money' as TransferAssistanceReceiveMethod, Validators.required],
    purpose: ['', [Validators.required, Validators.maxLength(500)]],
    beneficiary_relationship: ['', [Validators.required, Validators.maxLength(100)]],
    available_from: ['', Validators.required],
    available_to: ['', Validators.required],
    meeting_area: ['', [Validators.required, Validators.maxLength(200)]],
  });
  readonly beneficiaryLater = toSignal(this.form.controls.beneficiary_later.valueChanges, { initialValue: false });

  constructor() {
    this.form.controls.beneficiary_later.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(later => this.applyBeneficiaryMode(later));
    effect(() => this.sync.setActiveCase(this.hasActiveCase()));
    let previousContext = '';
    effect(() => {
      const user = this.auth.user();
      const context = JSON.stringify([
        user?.id, user?.tenant_id, user?.entity_type, user?.entity_id,
        user?.permissions, user?.roles, this.sessionLoading(),
      ]);
      if (context === previousContext) return;
      previousContext = context;
      untracked(() => {
        this.contextChanged.next();
        this.loadSubscription?.unsubscribe();
        this.sectionSubscription?.unsubscribe();
        this.sync.stop();
        this.clearContext();
        if (user && !this.sessionLoading()) this.load();
      });
    });
    this.destroyRef.onDestroy(() => {
      this.loadSubscription?.unsubscribe();
      this.sectionSubscription?.unsubscribe();
      this.sync.stop();
      this.confirmation.close();
    });
  }

  private clearContext(): void {
    this.confirmation.close();
    this.loading.set(false);
    this.saving.set(false);
    this.profileSaving.set(false);
    this.requests.set([]);
    this.reviewQueue.set([]);
    this.profileReviewQueue.set([]);
    this.availableRequests.set([]);
    this.assignments.set([]);
    this.witnessInvitations.set([]);
    this.reviewCases.set([]);
    this.beneficiaryOpenRequests.set([]);
    this.candidatesByRequest.set({});
    this.candidacyMessages.set({});
    this.syncAnnouncement.set('');
    this.beneficiaryOptions.set([]);
    this.beneficiarySearchValue.set('');
    this.witnessOptions.set([]);
    this.offersByRequest.set({});
    this.loadedOffers.set({});
    this.eventsByRequest.set({});
    this.reviewReasons.set({});
    this.profileReviewReasons.set({});
    this.workflowReasons.set({});
    this.offerAvailability.set({});
    this.offerMessages.set({});
    this.meetingTimes.set({});
    this.providers.set({});
    this.evidenceReferences.set({});
    this.conflicts.set({});
    this.profile.set(null);
    this.editingRequestId.set(null);
    this.form.reset({ receive_method: 'mobile_money', amount_mur: 0, beneficiary_later: false });
    this.error.set('');
    this.beneficiarySearch.next('');
    this.witnessSearch.next({ requestId: '', value: '' });
  }

  ngOnInit(): void {
    if (!this.featureEnabled) {
      this.loading.set(false);
      return;
    }
    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.countryOptions.set(this.buildCountryOptions()));
    this.beneficiarySearch
      .pipe(
        switchMap(search => timer(1000).pipe(
          switchMap(() => search.trim().length >= 3
            ? this.api.searchBeneficiaries(search.trim())
            : of([])),
          takeUntil(this.contextChanged),
        )),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: options => this.beneficiaryOptions.set(options),
        error: () => this.error.set('transfer_assistance.beneficiary_search_failed'),
      });
    this.witnessSearch
      .pipe(
        switchMap(search => search.value.trim().length >= 3
          ? timer(1000).pipe(
            switchMap(() => this.api.searchWitnesses(search.value.trim())),
            takeUntil(this.contextChanged),
          )
          : of([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: options => this.witnessOptions.set(options),
        error: () => this.error.set('transfer_assistance.witness_search_failed'),
      });
  }

  onBeneficiarySearch(event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.beneficiarySearchValue.set(target.value);
      this.beneficiaryOptions.set([]);
      this.beneficiarySearch.next(target.value);
    }
  }

  onDestinationCountryChange(code: string): void {
    const country = COUNTRIES.find(item => item.code === code);
    if (!country) {
      this.form.controls.receive_currency.setValue('');
      return;
    }
    this.form.patchValue({
      destination_country: country.code,
      receive_currency: country.currency,
    });
  }

  private buildCountryOptions() {
    return COUNTRIES.map(country => ({
      label: `${country.flag}  ${this.translate.instant(`countries.${country.code}`)} (${country.code})`,
      code: country.code,
      currency: country.currency,
    }));
  }

  createAndSubmit(): void {
    if (this.form.invalid || this.saving()) return;
    this.error.set('');
    this.saving.set(true);
    const editingId = this.editingRequestId();
    const save$ = editingId
      ? this.api.updateDraft(editingId, this.buildPayload())
      : this.api.create(this.buildPayload());
    save$
      .pipe(
        switchMap(request => this.api.submit(request.id)),
        finalize(() => this.saving.set(false)),
      )
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.editingRequestId.set(null);
          this.form.reset({ receive_method: 'mobile_money', amount_mur: 0 });
          this.load();
        },
        error: () => {
          this.error.set('transfer_assistance.save_failed');
          this.load();
        },
      });
  }

  saveDraft(): void {
      if (this.form.invalid || this.saving()) return;
      this.saving.set(true);
      this.error.set('');
      const editingId = this.editingRequestId();
      const save$ = editingId
        ? this.api.updateDraft(editingId, this.buildPayload())
        : this.api.create(this.buildPayload());
      save$.pipe(finalize(() => this.saving.set(false))).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: request => {
          this.editingRequestId.set(null);
          this.form.reset({ receive_method: 'mobile_money', amount_mur: 0 });
          this.load();
          this.error.set('');
        },
        error: () => this.error.set('transfer_assistance.save_failed'),
      });
  }

  editDraft(request: TransferAssistanceRequest): void {
      this.editingRequestId.set(request.id);
      const later = !request.beneficiary_member_id;
      if (!later) {
        this.beneficiaryOptions.update(options => [
          ...options.filter(option => option.member_id !== request.beneficiary_member_id),
          { member_id: request.beneficiary_member_id!, member_name: request.beneficiary_name },
        ]);
      }
      this.form.patchValue({
        beneficiary_later: later,
        beneficiary_member_id: request.beneficiary_member_id ?? '',
        destination_country: request.destination_country,
        amount_mur: request.amount_mur,
        receive_currency: COUNTRIES.find(country => country.code === request.destination_country)?.currency
          ?? request.receive_currency,
        receive_method: request.receive_method,
        purpose: request.purpose,
        beneficiary_relationship: request.beneficiary_relationship,
        available_from: request.available_from,
        available_to: request.available_to,
        meeting_area: request.meeting_area,
      });
  }

  submitExisting(request: TransferAssistanceRequest): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    this.api.submit(request.id)
      .pipe(finalize(() => this.saving.set(false)))
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => this.load(),
        error: () => this.error.set('transfer_assistance.submit_failed'),
      });
  }

  review(request: TransferAssistanceRequest, decision: 'approve' | 'reject' | 'suspend'): void {
    const reason = (this.reviewReasons()[request.id] ?? '').trim();
    if (decision !== 'approve' && !reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.error.set('');
    this.api.review(request.id, { decision, reason: reason || undefined }).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: () => this.error.set('transfer_assistance.review_failed'),
    });
  }

  cancel(request: TransferAssistanceRequest): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_cancel'), () => {
      this.api.cancel(request.id).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => this.load(),
        error: () => this.error.set('transfer_assistance.cancel_failed'),
      });
    });
  }

  private confirmAction(message: string, accept: () => void): void {
    this.confirmation.confirm({
      header: this.translate.instant('common.confirmation'),
      message,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept,
    });
  }

  setReviewReason(id: string, value: string): void {
    this.reviewReasons.update(reasons => ({ ...reasons, [id]: value }));
  }

  reviewReasonFor(id: string): string {
    const reason = this.reviewReasons()[id];
    return typeof reason === 'string' ? reason : '';
  }

  onReviewReasonInput(id: string, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) this.setReviewReason(id, target.value);
  }

  private load(): void {
    if (!this.featureEnabled) return;
    this.loadSubscription?.unsubscribe();
    this.loading.set(true);
    this.loadSubscription = forkJoin({
      mine: this.hasMemberEntity()
        ? this.api.getMine()
        : of({ data: [], pagination: { page: 1, limit: 20, total: 0 } }),
      review: this.canReview() ? this.api.getReviewQueue() : of(null),
      profile: this.canManageProfile() ? this.api.getProfile() : of(null),
      profileReviews: this.canVerifyProfiles() ? this.api.getProfileReviewQueue() : of(null),
      available: this.canBrowseAvailable() ? this.api.getAvailableRequests() : of(null),
      assignments: this.canParticipate() ? this.api.getAssignments() : of(null),
      invitations: this.canParticipate() ? this.api.getMyWitnessInvitations() : of(null),
      disputes: this.canResolveCases() ? this.api.getReviewCases('disputed') : of(null),
      suspensions: this.canSuspend() ? this.api.getReviewCases('suspended') : of(null),
      beneficiaryOpen: this.canBrowseBeneficiaryOpen() ? this.api.getBeneficiaryOpenRequests() : of(null),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: result => {
          this.requests.set(result.mine.data);
          this.reviewQueue.set(result.review?.data ?? []);
          this.profile.set(result.profile);
          this.profileReviewQueue.set(result.profileReviews?.data ?? []);
          this.availableRequests.set(result.available ?? []);
          this.assignments.set(result.assignments ?? []);
          this.witnessInvitations.set(result.invitations ?? []);
          this.reviewCases.set([
            ...(result.disputes?.data ?? []),
            ...(result.suspensions?.data ?? []),
          ]);
          this.beneficiaryOpenRequests.set(result.beneficiaryOpen ?? []);
          this.refreshLoadedCandidates();
          this.error.set('');
          this.startOrAcknowledgeSync();
        },
        error: () => this.error.set('transfer_assistance.load_failed'),
      });
  }

  refresh(): void {
    this.load();
  }

  private startOrAcknowledgeSync(): void {
    this.sync.start(sections => this.reloadSections(sections));
  }

  /** Reloads only the sections reported as changed by the sync service; form inputs are kept untouched. */
  reloadSections(sections: TransferAssistanceSyncSection[]): void {
    const loaders: Observable<unknown>[] = [];
    const ignoreErrors = <T>(source: Observable<T>) => source.pipe(catchError(() => of(null)));
    if (sections.includes('mine') && this.hasMemberEntity()) {
      loaders.push(ignoreErrors(this.api.getMine()).pipe(map(result => {
        if (result) this.requests.set(result.data);
        this.refreshLoadedCandidates();
      })));
    }
    if (sections.includes('available') && this.canBrowseAvailable()) {
      loaders.push(ignoreErrors(this.api.getAvailableRequests()).pipe(map(result => {
        if (result) this.availableRequests.set(result);
      })));
    }
    if (sections.includes('assignments') && this.canParticipate()) {
      loaders.push(ignoreErrors(this.api.getAssignments()).pipe(map(result => {
        if (result) this.assignments.set(result);
      })));
    }
    if (sections.includes('witness_invitations') && this.canParticipate()) {
      loaders.push(ignoreErrors(this.api.getMyWitnessInvitations()).pipe(map(result => {
        if (result) this.witnessInvitations.set(result);
      })));
    }
    if (sections.includes('beneficiary_open') && this.canBrowseBeneficiaryOpen()) {
      loaders.push(ignoreErrors(this.api.getBeneficiaryOpenRequests()).pipe(map(result => {
        if (result) this.beneficiaryOpenRequests.set(result);
      })));
    }
    if (sections.includes('review_queue') && this.canReview()) {
      loaders.push(ignoreErrors(this.api.getReviewQueue()).pipe(map(result => {
        if (result) this.reviewQueue.set(result.data);
      })));
    }
    if (sections.includes('review_queue') && (this.canResolveCases() || this.canSuspend())) {
      loaders.push(ignoreErrors(forkJoin({
        disputes: this.canResolveCases() ? this.api.getReviewCases('disputed') : of(null),
        suspensions: this.canSuspend() ? this.api.getReviewCases('suspended') : of(null),
      })).pipe(map(result => {
        if (result) this.reviewCases.set([...(result.disputes?.data ?? []), ...(result.suspensions?.data ?? [])]);
      })));
    }
    if (!loaders.length) return;
    this.syncAnnouncement.set('');
    this.sectionSubscription?.unsubscribe();
    this.sectionSubscription = merge(...loaders)
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef))
      .subscribe({ complete: () => this.syncAnnouncement.set('transfer_assistance.sync.updated') });
  }

  private refreshLoadedCandidates(): void {
    const loaded = this.candidatesByRequest();
    for (const request of this.requests()) {
      if (loaded[request.id] && request.beneficiary_pending && this.canDesignateStatus(request.status)) {
        this.loadCandidates(request);
      }
    }
  }

  private applyBeneficiaryMode(later: boolean): void {
    const member = this.form.controls.beneficiary_member_id;
    const relationship = this.form.controls.beneficiary_relationship;
    if (later) {
      member.setValue('');
      member.disable({ emitEvent: false });
      relationship.setValue('');
      relationship.setValidators([Validators.maxLength(100)]);
    } else {
      member.enable({ emitEvent: false });
      relationship.setValidators([Validators.required, Validators.maxLength(100)]);
    }
    relationship.updateValueAndValidity();
  }

  private buildPayload() {
    const { beneficiary_later, ...value } = this.form.getRawValue();
    return {
      ...value,
      beneficiary_member_id: beneficiary_later ? null : value.beneficiary_member_id,
      beneficiary_relationship: beneficiary_later ? '' : value.beneficiary_relationship,
    };
  }

  canDesignateStatus(status: TransferAssistanceRequest['status']): boolean {
    return status === 'matching' || status === 'witness_pending';
  }

  loadCandidates(request: TransferAssistanceRequest): void {
    this.api.getBeneficiaryCandidates(request.id)
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: candidates => this.candidatesByRequest.update(current => ({ ...current, [request.id]: candidates })),
        error: () => this.error.set('transfer_assistance.load_failed'),
      });
  }

  candidatesFor(requestId: string): TransferAssistanceBeneficiaryCandidate[] {
    return this.candidatesByRequest()[requestId] ?? [];
  }

  hasLoadedCandidates(requestId: string): boolean {
    return requestId in this.candidatesByRequest();
  }

  designateCandidate(request: TransferAssistanceRequest, candidate: TransferAssistanceBeneficiaryCandidate): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_designate_beneficiary', {
      name: candidate.member_name,
    }), () => this.runAction(this.api.designateBeneficiary(request.id, { candidate_id: candidate.id }),
      'transfer_assistance.workflow_failed'));
  }

  candidacyMessageFor(requestId: string): string {
    return this.candidacyMessages()[requestId] ?? '';
  }

  onCandidacyMessageInput(requestId: string, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.candidacyMessages.update(current => ({ ...current, [requestId]: target.value }));
    }
  }

  applyAsBeneficiary(open: TransferAssistanceBeneficiaryOpenRequest): void {
    const message = this.candidacyMessageFor(open.id).trim();
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_apply_beneficiary'),
      () => this.runAction(this.api.applyAsBeneficiary(open.id, message || undefined),
        'transfer_assistance.workflow_failed'));
  }

  withdrawCandidacy(open: TransferAssistanceBeneficiaryOpenRequest): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_withdraw_candidacy'),
      () => this.runAction(this.api.withdrawBeneficiaryCandidacy(open.id), 'transfer_assistance.workflow_failed'));
  }

  changeAccreditation(role: 'helper' | 'witness', action: 'request' | 'cancel'): void {
    if (!this.canManageProfile() || this.loading() || this.profileSaving()) return;
    if (action === 'cancel') {
      this.confirmAction(this.translate.instant('transfer_assistance.confirm_cancel_accreditation'),
        () => this.saveAccreditation(role, action));
    } else {
      this.saveAccreditation(role, action);
    }
  }

  private saveAccreditation(role: 'helper' | 'witness', action: 'request' | 'cancel'): void {
    if (this.profileSaving()) return;
    const optedIn = this.profile()?.[`${role}_opted_in`] ?? false;
    if (action === 'request' && optedIn) {
      this.error.set('errors.transfer_assistance.accreditation_exists');
      return;
    }
    this.profileSaving.set(true);
    this.error.set('');
    this.api.updateProfile({
      participant_role: role,
      action,
    })
      .pipe(finalize(() => this.profileSaving.set(false)))
      .pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
        next: profile => {
          this.profile.set(profile);
          this.load();
        },
        error: () => this.error.set('transfer_assistance.profile_save_failed'),
      });
  }

  reviewProfile(
    item: TransferAssistanceProfileReviewItem,
    participantRole: 'helper' | 'witness',
    decision: 'verify' | 'reject',
  ): void {
    const reason = (this.profileReviewReasons()[item.member_id] ?? '').trim();
    if (!reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.error.set('');
    this.api.reviewProfile(item.member_id, {
      participant_role: participantRole,
      decision,
      reason: reason || undefined,
    }).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: () => this.error.set('transfer_assistance.profile_review_failed'),
    });
  }

  profileReviewReasonFor(memberId: string): string {
    const reason = this.profileReviewReasons()[memberId];
    return typeof reason === 'string' ? reason : '';
  }

  onProfileReviewReasonInput(memberId: string, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.profileReviewReasons.update(reasons => ({ ...reasons, [memberId]: target.value }));
    }
  }

  onWitnessSearch(requestId: string, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement) {
      this.witnessOptions.set([]);
      this.witnessSearch.next({ requestId, value: target.value });
    }
  }

  loadOffers(request: TransferAssistanceRequest): void {
    this.api.getOffers(request.id).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: offers => {
        this.offersByRequest.update(current => ({ ...current, [request.id]: offers }));
        this.loadedOffers.update(current => ({ ...current, [request.id]: true }));
      },
      error: () => this.error.set('transfer_assistance.load_failed'),
    });
  }

  offersFor(requestId: string): TransferAssistanceOffer[] {
    return this.offersByRequest()[requestId] ?? [];
  }

  hasLoadedOffers(requestId: string): boolean {
    return this.loadedOffers()[requestId] ?? false;
  }

  isMeetingTimeReached(meetingAt?: string): boolean {
    const meetingTime = Date.parse(meetingAt ?? '');
    return Number.isFinite(meetingTime) && meetingTime <= Date.now();
  }

  nextActionForRequest(request: TransferAssistanceRequest): string {
    switch (request.status) {
      case 'draft':
        return 'transfer_assistance.next_action_draft';
      case 'submitted':
        return 'transfer_assistance.next_action_submitted';
      case 'matching':
        return 'transfer_assistance.next_action_matching';
      case 'witness_pending':
        if (!request.beneficiary_member_id) return 'transfer_assistance.next_action_designate_beneficiary';
        return request.witness_member_id
          ? 'transfer_assistance.next_action_schedule_meeting'
          : 'transfer_assistance.next_action_invite_witness';
      case 'meeting_scheduled':
        if (!request.requester_handover_confirmed && !this.isMeetingTimeReached(request.meeting_at)) {
          return 'transfer_assistance.next_action_wait_meeting_time';
        }
        return request.requester_handover_confirmed
          ? 'transfer_assistance.next_action_wait_handover'
          : 'transfer_assistance.next_action_confirm_handover';
      case 'transfer_in_progress':
        return 'transfer_assistance.next_action_wait_transfer_evidence';
      case 'transfer_sent':
        return 'transfer_assistance.next_action_wait_receipt';
      case 'disputed':
        return 'transfer_assistance.next_action_disputed';
      case 'suspended':
        return 'transfer_assistance.next_action_suspended';
      default:
        return 'transfer_assistance.next_action_closed';
    }
  }

  nextActionForAssignment(assignment: TransferAssistanceAssignment): string {
    if (assignment.status === 'disputed') return 'transfer_assistance.next_action_disputed';
    if (assignment.status === 'suspended') return 'transfer_assistance.next_action_suspended';
    if (assignment.status === 'completed' || assignment.status === 'cancelled' ||
        assignment.status === 'rejected' || assignment.status === 'expired') {
      return 'transfer_assistance.next_action_closed';
    }

    if (assignment.role === 'helper') {
      if (assignment.status === 'witness_pending') return 'transfer_assistance.next_helper_wait_witness';
      if (assignment.status === 'meeting_scheduled') {
        if (!assignment.helper_handover_confirmed && !this.isMeetingTimeReached(assignment.meeting_at)) {
          return 'transfer_assistance.next_action_wait_meeting_time';
        }
        return assignment.helper_handover_confirmed
          ? 'transfer_assistance.next_action_wait_handover'
          : 'transfer_assistance.next_action_confirm_handover';
      }
      if (assignment.status === 'transfer_in_progress') return 'transfer_assistance.next_helper_submit_evidence';
      if (assignment.status === 'transfer_sent') return 'transfer_assistance.next_helper_wait_receipt';
    }

    if (assignment.role === 'witness') {
      if (assignment.status === 'witness_pending') return 'transfer_assistance.next_witness_wait_meeting';
      if (assignment.status === 'meeting_scheduled') {
        if (!assignment.witness_attested && !this.isMeetingTimeReached(assignment.meeting_at)) {
          return 'transfer_assistance.next_action_wait_meeting_time';
        }
        return assignment.witness_attested
          ? 'transfer_assistance.next_action_wait_handover'
          : 'transfer_assistance.next_witness_attest';
      }
    }

    if (assignment.role === 'beneficiary' && assignment.status === 'transfer_sent') {
      return assignment.beneficiary_confirmation
        ? 'transfer_assistance.next_action_closed'
        : 'transfer_assistance.next_beneficiary_confirm_receipt';
    }

    return 'transfer_assistance.next_action_wait';
  }

  selectOffer(request: TransferAssistanceRequest, offer: TransferAssistanceOffer): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_select_helper', {
      name: offer.helper_name,
    }), () => this.runAction(this.api.selectOffer(request.id, offer.id), 'transfer_assistance.workflow_failed'));
  }

  submitOffer(request: TransferAssistanceAvailableRequest): void {
    const availability = this.offerAvailability()[request.id]?.trim() ?? '';
    const message = this.offerMessages()[request.id]?.trim() ?? '';
    if (!availability || !message) {
      this.error.set('transfer_assistance.offer_fields_required');
      return;
    }
    this.runAction(this.api.submitOffer(request.id, { availability, message }),
      'transfer_assistance.workflow_failed');
  }

  inviteWitness(request: TransferAssistanceRequest, witness: TransferAssistanceBeneficiaryOption): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_invite_witness', {
      name: witness.member_name,
    }), () => this.runAction(this.api.inviteWitness(request.id, witness.member_id),
      'transfer_assistance.workflow_failed'));
  }

  respondToInvitation(invitation: TransferAssistanceInvitation, accept: boolean): void {
    const conflictDeclared = this.conflicts()[invitation.invitation_id] ?? false;
    const reason = this.reasonFor(invitation.invitation_id);
    if (conflictDeclared && !reason.trim()) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.runAction(this.api.respondToWitnessInvitation(
      invitation.invitation_id, accept, conflictDeclared, reason || undefined),
    'transfer_assistance.workflow_failed');
  }

  scheduleMeeting(request: TransferAssistanceRequest): void {
    const value = this.meetingTimeFor(request.id);
    if (!value) {
      this.error.set('transfer_assistance.meeting_time_required');
      return;
    }
    this.runAction(this.api.scheduleMeeting(request.id, new Date(value).toISOString()),
      'transfer_assistance.workflow_failed');
  }

  scheduleAssignment(assignment: TransferAssistanceAssignment): void {
    const value = this.meetingTimeFor(assignment.request_id);
    if (!value) {
      this.error.set('transfer_assistance.meeting_time_required');
      return;
    }
    this.runAction(this.api.scheduleMeeting(assignment.request_id, new Date(value).toISOString()),
      'transfer_assistance.workflow_failed');
  }

  confirmHandover(requestId: string): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_handover_prompt'),
      () => this.runAction(this.api.confirmHandover(requestId), 'transfer_assistance.workflow_failed'));
  }

  attestWitness(requestId: string): void {
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_attestation'),
      () => this.runAction(this.api.attestWitness(requestId), 'transfer_assistance.workflow_failed'));
  }

  submitEvidence(requestId: string): void {
    const provider = this.providers()[requestId]?.trim() ?? '';
    const reference = this.evidenceReferences()[requestId]?.trim() ?? '';
    if (!provider || !reference) {
      this.error.set('transfer_assistance.evidence_fields_required');
      return;
    }
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_evidence'),
      () => this.runAction(this.api.submitEvidence(requestId, provider, reference),
        'transfer_assistance.workflow_failed'));
  }

  confirmReceipt(
    assignment: TransferAssistanceAssignment,
    outcome: 'full' | 'partial' | 'not_received',
  ): void {
    const reason = this.reasonFor(assignment.request_id).trim();
    if (outcome !== 'full' && !reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.confirmAction(this.translate.instant(`transfer_assistance.confirm_receipt_${outcome}`),
      () => this.runAction(this.api.confirmReceipt(assignment.request_id, outcome, reason || undefined),
        'transfer_assistance.workflow_failed'));
  }

  openDispute(requestId: string): void {
    const reason = this.reasonFor(requestId).trim();
    if (!reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_open_dispute'),
      () => this.runAction(this.api.openDispute(requestId, reason), 'transfer_assistance.workflow_failed'));
  }

  resolveDispute(item: TransferAssistanceReviewCase, resolution: 'resume' | 'cancel'): void {
    const reason = this.reasonFor(item.id).trim();
    if (!reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.confirmAction(this.translate.instant('transfer_assistance.confirm_resolve_dispute'),
      () => this.runAction(this.api.resolveDispute(item.id, resolution, reason),
        'transfer_assistance.workflow_failed'));
  }

  resumeRequest(item: TransferAssistanceReviewCase): void {
    const reason = this.reasonFor(item.id).trim();
    if (!reason) {
      this.error.set('transfer_assistance.reason_required');
      return;
    }
    this.runAction(this.api.resume(item.id, reason), 'transfer_assistance.workflow_failed');
  }

  loadEvents(requestId: string): void {
    this.api.getEvents(requestId).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: events => this.eventsByRequest.update(current => ({ ...current, [requestId]: events })),
      error: () => this.error.set('transfer_assistance.timeline_failed'),
    });
  }

  eventsFor(requestId: string): TransferAssistanceEvent[] {
    return this.eventsByRequest()[requestId] ?? [];
  }

  meetingTimeFor(requestId: string): string {
    return this.meetingTimes()[requestId] ?? '';
  }

  offerInputFor(requestId: string, field: 'availability' | 'message'): string {
    return field === 'availability'
      ? this.offerAvailability()[requestId] ?? ''
      : this.offerMessages()[requestId] ?? '';
  }

  evidenceValue(requestId: string, field: 'provider' | 'reference'): string {
    return field === 'provider'
      ? this.providers()[requestId] ?? ''
      : this.evidenceReferences()[requestId] ?? '';
  }

  reasonFor(id: string): string {
    return this.workflowReasons()[id] ?? '';
  }

  conflictFor(id: string): boolean {
    return this.conflicts()[id] ?? false;
  }

  setConflict(id: string, event: Event): void {
    const target = event.target;
    if (target instanceof HTMLInputElement)
      this.conflicts.update(current => ({ ...current, [id]: target.checked }));
  }

  canDisputeStatus(status: TransferAssistanceAssignment['status']): boolean {
    return ['witness_pending', 'meeting_scheduled', 'transfer_in_progress', 'transfer_sent'].includes(status);
  }

  setInputValue(
    field: 'offerAvailability' | 'offerMessages' | 'meetingTimes' | 'providers' | 'evidenceReferences' | 'workflowReasons',
    id: string,
    event: Event,
  ): void {
    const target = event.target;
    if (!(target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement)) return;
    const signals = {
      offerAvailability: this.offerAvailability,
      offerMessages: this.offerMessages,
      meetingTimes: this.meetingTimes,
      providers: this.providers,
      evidenceReferences: this.evidenceReferences,
      workflowReasons: this.workflowReasons,
    };
    signals[field].update(current => ({ ...current, [id]: target.value }));
  }

  private runAction<T>(action: Observable<T>, errorKey: string): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    action.pipe(finalize(() => this.saving.set(false))).pipe(takeUntil(this.contextChanged), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.load(),
      error: () => this.error.set(errorKey),
    });
  }
}
