import '@angular/compiler';
import { HttpErrorResponse } from '@angular/common/http';
import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TransferAssistanceSyncState } from '@models/transfer-assistance.model';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';
import {
  SYNC_ACTIVE_INTERVAL_MS,
  SYNC_IDLE_INTERVAL_MS,
  SYNC_MAX_BACKOFF_MS,
  TransferAssistanceSyncService,
} from './transfer-assistance-sync.service';

function state(sections: TransferAssistanceSyncState['sections']): TransferAssistanceSyncState {
  return { version: JSON.stringify(sections), sections, server_time: '2026-10-23T00:00:00Z' };
}

describe('TransferAssistanceSyncService', () => {
  const getSyncState = vi.fn();
  let hidden = false;
  let service: TransferAssistanceSyncService;
  let onChange: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    hidden = false;
    getSyncState.mockReset().mockReturnValue(of(state({ mine: 'a', available: 'x' })));
    onChange = vi.fn();
    Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden });
    TestBed.configureTestingModule({
      providers: [
        TransferAssistanceSyncService,
        { provide: TransferAssistanceApiService, useValue: { getSyncState } },
        { provide: DOCUMENT, useValue: document },
      ],
    });
    service = TestBed.inject(TransferAssistanceSyncService);
  });

  afterEach(() => {
    service.stop();
    vi.useRealTimers();
  });

  it('records a baseline without notifying, then reports only changed sections', () => {
    service.start(onChange);
    expect(getSyncState).toHaveBeenCalledTimes(1);
    expect(onChange).not.toHaveBeenCalled();
    expect(service.lastSyncedAt()).not.toBeNull();

    getSyncState.mockReturnValue(of(state({ mine: 'b', available: 'x' })));
    vi.advanceTimersByTime(SYNC_IDLE_INTERVAL_MS);
    expect(getSyncState).toHaveBeenCalledTimes(2);
    expect(onChange).toHaveBeenCalledExactlyOnceWith(['mine']);

    vi.advanceTimersByTime(SYNC_IDLE_INTERVAL_MS);
    expect(onChange).toHaveBeenCalledTimes(1);
  });

  it('polls faster while a case is active', () => {
    service.start(onChange);
    service.setActiveCase(true);
    vi.advanceTimersByTime(SYNC_ACTIVE_INTERVAL_MS);
    expect(getSyncState).toHaveBeenCalledTimes(2);
  });

  it('acknowledges silently when started again after a local reload', () => {
    service.start(onChange);
    getSyncState.mockReturnValue(of(state({ mine: 'b', available: 'x' })));
    service.start(onChange);
    expect(getSyncState).toHaveBeenCalledTimes(2);
    expect(onChange).not.toHaveBeenCalled();
  });

  it('pauses while the tab is hidden and refreshes when it becomes visible', () => {
    service.start(onChange);
    hidden = true;
    vi.advanceTimersByTime(SYNC_IDLE_INTERVAL_MS * 2);
    expect(getSyncState).toHaveBeenCalledTimes(1);

    hidden = false;
    getSyncState.mockReturnValue(of(state({ mine: 'a', available: 'y' })));
    document.dispatchEvent(new Event('visibilitychange'));
    expect(getSyncState).toHaveBeenCalledTimes(2);
    expect(onChange).toHaveBeenCalledExactlyOnceWith(['available']);
  });

  it('backs off after errors, capped at the maximum delay', () => {
    service.start(onChange);
    getSyncState.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 503 })));
    vi.advanceTimersByTime(SYNC_IDLE_INTERVAL_MS);
    expect(getSyncState).toHaveBeenCalledTimes(2);
    vi.advanceTimersByTime(SYNC_IDLE_INTERVAL_MS);
    expect(getSyncState).toHaveBeenCalledTimes(2);
    vi.advanceTimersByTime(SYNC_MAX_BACKOFF_MS - SYNC_IDLE_INTERVAL_MS);
    expect(getSyncState).toHaveBeenCalledTimes(3);
  });

  it('stops polling on 401 or 403', () => {
    getSyncState.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 401 })));
    service.start(onChange);
    expect(service.stopped()).toBe(true);
    vi.advanceTimersByTime(SYNC_MAX_BACKOFF_MS * 2);
    expect(getSyncState).toHaveBeenCalledTimes(1);
  });

  it('resets the baseline when stopped so a new session does not report stale changes', () => {
    service.start(onChange);
    service.stop();
    getSyncState.mockReturnValue(of(state({ mine: 'z' })));
    service.start(onChange);
    expect(onChange).not.toHaveBeenCalled();
  });
});
