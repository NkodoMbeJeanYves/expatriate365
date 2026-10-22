import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TransferAssistanceInfoPage } from './transfer-assistance-info.page';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';
import { TransferAssistanceProfile, TransferAssistanceRules } from '@models/transfer-assistance.model';

vi.mock('@env/environment', () => ({
  environment: { features: { transferAssistance: true } },
}));

function rules(overrides: Partial<TransferAssistanceRules> = {}): TransferAssistanceRules {
  return {
    require_mfa: false, matching_window_days: 7, invitation_window_days: 3,
    minimum_request_amount: 500, maximum_request_amount: 20000, amount_currency: 'MUR',
    requests_open: true, verifier_roles: ['org_admin', 'auditor'],
    my_eligibility: {
      member_active: true, email_verified: true, mfa_enabled: false,
      is_community_admin: false, can_request_accreditation: true,
    },
    ...overrides,
  };
}

const profile: TransferAssistanceProfile = {
  helper_opted_in: true, helper_verification_status: 'verified',
  witness_opted_in: false, witness_verification_status: 'not_requested',
};

describe('TransferAssistanceInfoPage', () => {
  const getRules = vi.fn();
  const getProfile = vi.fn();

  function create(): TransferAssistanceInfoPage {
    const page = TestBed.runInInjectionContext(() => new TransferAssistanceInfoPage());
    page.ngOnInit();
    return page;
  }

  beforeEach(() => {
    getRules.mockReset().mockReturnValue(of(rules()));
    getProfile.mockReset().mockReturnValue(of(profile));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [{ provide: TransferAssistanceApiService, useValue: { getRules, getProfile } }],
    });
  });

  it('loads rules and profile together', () => {
    const page = create();

    expect(getRules).toHaveBeenCalledTimes(1);
    expect(getProfile).toHaveBeenCalledTimes(1);
    expect(page.loading()).toBe(false);
    expect(page.rules()?.verifier_roles).toEqual(['org_admin', 'auditor']);
    expect(page.profile()?.helper_verification_status).toBe('verified');
  });

  it('omits the MFA check and shows it as optional when not required', () => {
    const page = create();

    expect(page.checklist().map((c) => c.key)).toEqual(['active', 'email']);
    expect(page.mfaConditionKey()).toBe('transfer_assistance.info.cond_mfa_optional');
  });

  it('adds the MFA check and marks it required when the association requires MFA', () => {
    getRules.mockReturnValue(of(rules({
      require_mfa: true,
      my_eligibility: {
        member_active: true, email_verified: false, mfa_enabled: false,
        is_community_admin: false, can_request_accreditation: false,
      },
    })));
    const page = create();

    expect(page.checklist()).toEqual([
      { key: 'active', ok: true },
      { key: 'email', ok: false },
      { key: 'mfa', ok: false },
    ]);
    expect(page.mfaConditionKey()).toBe('transfer_assistance.info.cond_mfa_required');
  });

  it('keeps general information when the rules endpoint fails', () => {
    getRules.mockReturnValue(throwError(() => new Error('boom')));
    const page = create();

    expect(page.loading()).toBe(false);
    expect(page.rules()).toBeNull();
    expect(page.profile()).toEqual(profile);
    expect(page.checklist()).toEqual([]);
    expect(page.mfaConditionKey()).toBe('transfer_assistance.info.cond_mfa_if_required');
  });

  it('exposes the community admin exclusion from the eligibility payload', () => {
    getRules.mockReturnValue(of(rules({
      my_eligibility: {
        member_active: true, email_verified: true, mfa_enabled: true,
        is_community_admin: true, can_request_accreditation: false,
      },
    })));
    const page = create();

    expect(page.rules()?.my_eligibility.is_community_admin).toBe(true);
  });

  it('maps verification statuses to tag severities', () => {
    const page = create();

    expect(page.severity('verified')).toBe('success');
    expect(page.severity('pending')).toBe('warn');
    expect(page.severity('rejected')).toBe('danger');
    expect(page.severity('not_requested')).toBe('secondary');
  });
});
