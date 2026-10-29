import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { of, Subject, throwError } from 'rxjs';
import { CurrencyValidationService } from '@service/currency-validation.service';
import { CurrencyValidationResponse } from '@models/currency-validation.model';
import { CurrencyValidationComponent } from './currency-validation.component';

describe('CurrencyValidationComponent', () => {
  const validate = vi.fn();
  const covered: CurrencyValidationResponse = {
    country_code: 'MU', base_currency: 'MUR', is_consistent: true, allowed_currencies: ['MUR'],
    coverage_status: 'covered', source: 'Frankfurter', checked_at: '2026-10-22T00:00:00Z',
  };

  beforeEach(() => {
    vi.useFakeTimers();
    validate.mockReset().mockReturnValue(of(covered));
    TestBed.configureTestingModule({
      providers: [{ provide: CurrencyValidationService, useValue: { validate } }],
    });
  });

  afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  function create(country = 'MU', currency = 'MUR') {
    const countryInput = signal<string | null>(country);
    const currencyInput = signal<string | null>(currency);
    const superAdminInput = signal(false);
    const component = TestBed.runInInjectionContext(() => new CurrencyValidationComponent());
    Object.defineProperties(component, {
      country: { value: countryInput },
      currency: { value: currencyInput },
      superAdmin: { value: superAdminInput },
    });
    TestBed.tick();
    return {
      componentInstance: component,
      setCountry: countryInput.set,
      setCurrency: currencyInput.set,
      setSuperAdmin: superAdminInput.set,
      destroy: () => TestBed.resetTestingModule(),
    };
  }

  it('normalizes codes and defers verification by 300ms', () => {
    const fixture = create(' mu ', ' mur ');
    vi.advanceTimersByTime(299);
    expect(validate).not.toHaveBeenCalled();
    vi.advanceTimersByTime(1);
    expect(validate).toHaveBeenCalledWith('MU', 'MUR', false);
    expect(fixture.componentInstance.result()).toEqual(covered);
  });

  it('clears coverage immediately and cancels obsolete results on currency change', () => {
    const previous = new Subject<CurrencyValidationResponse>();
    validate.mockReturnValueOnce(previous).mockReturnValue(of({
      ...covered, country_code: 'RS', base_currency: 'RSD', allowed_currencies: ['RSD'],
    }));
    const fixture = create();
    vi.advanceTimersByTime(300);
    fixture.setCountry('RS');
    fixture.setCurrency('RSD');
    TestBed.tick();
    expect(previous.observed).toBe(false);
    expect(fixture.componentInstance.result()).toBeNull();
    previous.next(covered);
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.result()?.base_currency).toBe('RSD');
  });

  it('supports explicit retry after HTTP failure', () => {
    validate.mockReturnValueOnce(throwError(() => new Error('network')));
    const fixture = create();
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.failed()).toBe(true);
    fixture.componentInstance.retry();
    TestBed.tick();
    vi.advanceTimersByTime(300);
    expect(fixture.componentInstance.failed()).toBe(false);
    expect(fixture.componentInstance.result()?.coverage_status).toBe('covered');
  });

  it('uses the superadmin route when creating an association', () => {
    const fixture = create();
    fixture.setSuperAdmin(true);
    TestBed.tick();
    vi.advanceTimersByTime(300);
    expect(validate).toHaveBeenCalledWith('MU', 'MUR', true);
  });

  it('does not call the provider for incomplete inputs', () => {
    create('M', '');
    vi.advanceTimersByTime(1000);
    expect(validate).not.toHaveBeenCalled();
  });

  it('unsubscribes when destroyed', () => {
    const pending = new Subject<CurrencyValidationResponse>();
    validate.mockReturnValue(pending);
    const fixture = create();
    vi.advanceTimersByTime(300);
    fixture.destroy();
    expect(pending.observed).toBe(false);
  });
});
