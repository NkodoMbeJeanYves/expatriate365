import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';
import { Confirmation, ConfirmationService } from 'primeng/api';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthStore } from '@core/auth/auth.store';
import { MeResponse } from '@core/auth/models/user.model';
import { TransferAssistancePage } from './transfer-assistance.page';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';
import { TransferAssistanceOffer, TransferAssistanceRequest } from '@models/transfer-assistance.model';

vi.mock('@env/environment', () => ({
  environment: { features: { transferAssistance: true } },
}));

function member(overrides: Partial<MeResponse> = {}): MeResponse {
  return {
    id: 'user-a', email: 'member@example.test', full_name: 'Test Member', roles: [],
    tenant_id: 'tenant-a', entity_type: 'member', entity_id: 'member-a',
    permissions: ['transfer_assistance.create'], ...overrides,
  };
}

describe('TransferAssistancePage session context', () => {
  const user = signal<MeResponse | null>(null);
  const loading = signal(false);
  const getMine = vi.fn();
  const cancel = vi.fn();
  const selectOffer = vi.fn();
  const confirmHandover = vi.fn();
  const attestWitness = vi.fn();
  const searchWitnesses = vi.fn();
  const updateProfile = vi.fn();
  const offer: TransferAssistanceOffer = {
    id: 'offer-a', request_id: 'request-a', helper_member_id: 'helper-a', helper_name: 'Test Helper',
    availability: 'Tomorrow', message: 'I can help', status: 'pending', created_at: '2026-10-20T00:00:00Z',
  };
  const instant = vi.fn((key: string, _params?: Record<string, string>) => key);
  const confirm = vi.fn<(confirmation: Confirmation) => void>();
  const close = vi.fn();
  const request: TransferAssistanceRequest = {
    id: 'request-a', tenant_id: 'tenant-a', requester_member_id: 'member-a',
    beneficiary_member_id: 'beneficiary-a', beneficiary_name: 'Beneficiary',
    destination_country: 'CM', amount_mur: 1000, receive_currency: 'XAF',
    receive_method: 'mobile_money', purpose: 'Family support', beneficiary_relationship: 'Parent',
    available_from: '2026-10-20', available_to: '2026-10-23', meeting_area: 'Ebene',
    status: 'draft', requester_handover_confirmed: false, helper_handover_confirmed: false,
    witness_attested: false, created_at: '2026-10-20T00:00:00Z',
  };
  let page: TransferAssistancePage;

  beforeEach(() => {
    user.set(member());
    loading.set(false);
    getMine.mockReset().mockReturnValue(of({ data: [] }));
    cancel.mockReset().mockReturnValue(of(request));
    selectOffer.mockReset().mockReturnValue(of(request));
    confirmHandover.mockReset().mockReturnValue(of(request));
    attestWitness.mockReset().mockReturnValue(of(request));
    searchWitnesses.mockReset().mockReturnValue(of([]));
    updateProfile.mockReset().mockReturnValue(of({
      helper_opted_in: true, helper_verification_status: 'pending',
      witness_opted_in: false, witness_verification_status: 'not_requested',
    }));
    instant.mockClear();
    confirm.mockReset();
    close.mockReset();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthStore, useValue: {
          user, isLoading: loading,
          hasPermission: (permission: string) => user()?.permissions.includes(permission) ?? false,
        } },
        { provide: TransferAssistanceApiService, useValue: {
          getMine, cancel, selectOffer, confirmHandover, attestWitness, searchWitnesses, updateProfile,
          getProfile: () => of(null),
        } },
        { provide: ConfirmationService, useValue: { confirm, close } },
        { provide: TranslateService, useValue: {
          instant, onLangChange: new Subject(),
        } },
      ],
    });
    page = TestBed.runInInjectionContext(() => new TransferAssistancePage());
    TestBed.tick();
  });

  afterEach(() => vi.useRealTimers());

  it('hides accreditation and available offers for org_admin even with permissions', () => {
    user.set(member({ roles: ['org_admin'], permissions: [
      'transfer_assistance.profile_manage', 'transfer_assistance.view_available', 'transfer_assistance.offer',
    ] }));
    expect(page.isOrgAdmin()).toBe(true);
    expect(page.canManageProfile()).toBe(false);
    expect(page.canBrowseAvailable()).toBe(false);
    page.changeAccreditation('helper', 'request');
    expect(updateProfile).not.toHaveBeenCalled();
  });

  it('requests only the selected accreditation and prevents another pending request', () => {
    user.set(member({ permissions: ['transfer_assistance.profile_manage'] }));
    page.changeAccreditation('helper', 'request');
    expect(updateProfile).toHaveBeenCalledExactlyOnceWith({ participant_role: 'helper', action: 'request' });
    page.profile.set({
      helper_opted_in: true, helper_verification_status: 'pending',
      witness_opted_in: false, witness_verification_status: 'not_requested',
    });
    page.changeAccreditation('helper', 'request');
    expect(updateProfile).toHaveBeenCalledTimes(1);
    expect(page.error()).toBe('errors.transfer_assistance.accreditation_exists');
  });

  it('requires confirmation to cancel or withdraw an accreditation', () => {
    user.set(member({ permissions: ['transfer_assistance.profile_manage'] }));
    page.profile.set({
      helper_opted_in: false, helper_verification_status: 'not_requested',
      witness_opted_in: true, witness_verification_status: 'verified',
    });
    page.changeAccreditation('witness', 'cancel');
    expect(updateProfile).not.toHaveBeenCalled();
    confirm.mock.calls[0][0].accept?.();
    expect(updateProfile).toHaveBeenCalledExactlyOnceWith({ participant_role: 'witness', action: 'cancel' });
  });

  function searchWitness(value: string): void {
    const input = document.createElement('input');
    input.value = value;
    input.addEventListener('input', event => page.onWitnessSearch(request.id, event));
    input.dispatchEvent(new Event('input'));
  }

  it('searches witnesses only after 1000ms without further input', () => {
    vi.useFakeTimers();
    page.ngOnInit();
    searchWitness('Eric');
    vi.advanceTimersByTime(999);
    expect(searchWitnesses).not.toHaveBeenCalled();
    searchWitness('Erica');
    vi.advanceTimersByTime(999);
    expect(searchWitnesses).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(searchWitnesses).toHaveBeenCalledExactlyOnceWith('Erica');
  });

  it('cancels pending searches and clears obsolete witness results immediately', () => {
    vi.useFakeTimers();
    page.ngOnInit();
    const pending = new Subject<[]>();
    searchWitnesses.mockReturnValue(pending);
    searchWitness('Eric');
    vi.advanceTimersByTime(1000);
    expect(pending.observed).toBe(true);
    page.witnessOptions.set([{ member_id: 'old', member_name: 'Old result' }]);
    searchWitness('Erica');
    expect(pending.observed).toBe(false);
    expect(page.witnessOptions()).toEqual([]);
    searchWitness('Er');
    vi.advanceTimersByTime(1000);
    expect(searchWitnesses).toHaveBeenCalledTimes(1);
  });

  it('selects a helper only after accepting the shared confirmation', () => {
    page.selectOffer(request, offer);
    expect(selectOffer).not.toHaveBeenCalled();
    expect(instant).toHaveBeenCalledWith('transfer_assistance.confirm_select_helper', { name: offer.helper_name });
    expect(confirm.mock.calls[0][0].message).toBe('transfer_assistance.confirm_select_helper');
    confirm.mock.calls[0][0].accept?.();
    expect(selectOffer).toHaveBeenCalledExactlyOnceWith(request.id, offer.id);
    expect(getMine).toHaveBeenCalledTimes(2);
  });

  it('does not select a helper when confirmation is rejected', () => {
    page.selectOffer(request, offer);
    confirm.mock.calls[0][0].reject?.();
    expect(selectOffer).not.toHaveBeenCalled();
  });

  it('reports helper selection failures', () => {
    selectOffer.mockReturnValue(throwError(() => new Error('Selection failed')));
    page.selectOffer(request, offer);
    confirm.mock.calls[0][0].accept?.();
    expect(page.error()).toBe('transfer_assistance.workflow_failed');
    expect(page.saving()).toBe(false);
  });

  it.each(['handover', 'attestation'] as const)('confirms %s using the shared dialog', action => {
    const api = action === 'handover' ? confirmHandover : attestWitness;
    if (action === 'handover') page.confirmHandover(request.id);
    else page.attestWitness(request.id);
    expect(api).not.toHaveBeenCalled();
    confirm.mock.calls[0][0].accept?.();
    expect(api).toHaveBeenCalledExactlyOnceWith(request.id);
  });

  it('opens the shared confirmation and cancels only after acceptance', () => {
    page.cancel(request);
    expect(cancel).not.toHaveBeenCalled();
    const dialog = confirm.mock.calls[0][0];
    expect(dialog).toMatchObject({
      header: 'common.confirmation', message: 'transfer_assistance.confirm_cancel',
      acceptLabel: 'common.yes', rejectLabel: 'common.no',
    });
    dialog.accept?.();
    expect(cancel).toHaveBeenCalledExactlyOnceWith(request.id);
    expect(getMine).toHaveBeenCalledTimes(2);
  });

  it('does not cancel when the confirmation is rejected', () => {
    page.cancel(request);
    confirm.mock.calls[0][0].reject?.();
    expect(cancel).not.toHaveBeenCalled();
    expect(getMine).toHaveBeenCalledTimes(1);
  });

  it('reports cancellation errors without reloading the list', () => {
    cancel.mockReturnValue(throwError(() => new Error('Cancellation failed')));
    page.cancel(request);
    confirm.mock.calls[0][0].accept?.();
    expect(page.error()).toBe('transfer_assistance.cancel_failed');
    expect(getMine).toHaveBeenCalledTimes(1);
  });

  it('closes the confirmation when the session context changes', () => {
    page.cancel(request);
    close.mockClear();
    user.set(member({ tenant_id: 'tenant-b' }));
    TestBed.tick();
    expect(close).toHaveBeenCalledOnce();
    expect(cancel).not.toHaveBeenCalled();
  });

  it('updates form eligibility when identity or permissions change', () => {
    expect(page.canCreateRequest()).toBe(true);
    user.set(member({ entity_type: 'user' }));
    expect(page.canCreateRequest()).toBe(false);
    user.set(member({ permissions: [] }));
    expect(page.canCreateRequest()).toBe(false);
    user.set(member({ entity_type: 'board_member' }));
    expect(page.canCreateRequest()).toBe(true);
  });

  it('does not reload for an equivalent session refresh', () => {
    expect(getMine).toHaveBeenCalledTimes(1);
    user.set(member());
    TestBed.tick();
    expect(getMine).toHaveBeenCalledTimes(1);
  });

  it('clears drafts and cancels old responses when the account or tenant changes', () => {
    const pending = new Subject<{ data: [] }>();
    getMine.mockReturnValueOnce(pending);
    user.set(member({ tenant_id: 'tenant-b' }));
    TestBed.tick();
    expect(pending.observed).toBe(true);
    page.editingRequestId.set('old-draft');
    page.form.controls.purpose.setValue('Previous private purpose');
    page.beneficiarySearchValue.set('Previous beneficiary');
    page.eventsByRequest.set({ old: [] });

    user.set(member({ id: 'user-b', tenant_id: 'tenant-c', entity_id: 'member-b' }));
    TestBed.tick();

    expect(pending.observed).toBe(false);
    expect(page.editingRequestId()).toBeNull();
    expect(page.form.controls.purpose.value).toBe('');
    expect(page.beneficiarySearchValue()).toBe('');
    expect(page.eventsByRequest()).toEqual({});
    expect(getMine).toHaveBeenCalledTimes(3);
    expect(page.requests()).toEqual([]);
  });

  it('waits for session initialization and clears data on logout', () => {
    loading.set(true);
    TestBed.tick();
    expect(page.sessionLoading()).toBe(true);
    expect(getMine).toHaveBeenCalledTimes(1);
    loading.set(false);
    TestBed.tick();
    expect(getMine).toHaveBeenCalledTimes(2);
    user.set(null);
    TestBed.tick();
    expect(page.canCreateRequest()).toBe(false);
    expect(page.requests()).toEqual([]);
    expect(getMine).toHaveBeenCalledTimes(2);
  });
});
