import { HttpErrorResponse } from '@angular/common/http';
import { DOCUMENT, inject, Injectable, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { TransferAssistanceSyncSection, TransferAssistanceSyncState } from '@models/transfer-assistance.model';
import { TransferAssistanceApiService } from './transfer-assistance-api.service';

export const SYNC_ACTIVE_INTERVAL_MS = 15_000;
export const SYNC_IDLE_INTERVAL_MS = 60_000;
export const SYNC_MAX_BACKOFF_MS = 120_000;

/**
 * Polls the lightweight sync-state endpoint and reports which sections changed,
 * so the page reloads only what is needed. Polling pauses while the tab is hidden.
 */
@Injectable()
export class TransferAssistanceSyncService implements OnDestroy {
  private readonly api = inject(TransferAssistanceApiService);
  private readonly document = inject(DOCUMENT);

  readonly lastSyncedAt = signal<Date | null>(null);
  readonly stopped = signal(false);

  private tokens: TransferAssistanceSyncState['sections'] | null = null;
  private onChange: ((sections: TransferAssistanceSyncSection[]) => void) | null = null;
  private timer: ReturnType<typeof setTimeout> | null = null;
  private request: Subscription | null = null;
  private failures = 0;
  private activeCase = false;
  private running = false;

  private readonly onVisibility = () => {
    if (!this.document.hidden) this.refreshNow();
  };
  private readonly onOnline = () => this.refreshNow();

  start(onChange: (sections: TransferAssistanceSyncSection[]) => void): void {
    this.onChange = onChange;
    if (this.running) {
      this.tick(true);
      return;
    }
    this.running = true;
    this.stopped.set(false);
    this.document.addEventListener('visibilitychange', this.onVisibility);
    this.document.defaultView?.addEventListener('online', this.onOnline);
    this.tick();
  }

  setActiveCase(active: boolean): void {
    if (this.activeCase === active) return;
    this.activeCase = active;
    if (this.running && active && this.failures === 0) this.schedule(SYNC_ACTIVE_INTERVAL_MS);
  }

  refreshNow(silent = false): void {
    if (!this.running) return;
    this.tick(silent);
  }

  stop(): void {
    this.running = false;
    this.tokens = null;
    this.failures = 0;
    this.clearTimer();
    this.request?.unsubscribe();
    this.request = null;
    this.document.removeEventListener('visibilitychange', this.onVisibility);
    this.document.defaultView?.removeEventListener('online', this.onOnline);
  }

  ngOnDestroy(): void {
    this.stop();
  }

  private tick(silent = false): void {
    this.clearTimer();
    if (!this.running) return;
    if (this.document.hidden) {
      this.schedule(this.interval());
      return;
    }
    this.request?.unsubscribe();
    this.request = this.api.getSyncState().subscribe({
      next: (state) => {
        this.failures = 0;
        const changed = this.diff(state.sections);
        this.tokens = { ...state.sections };
        this.lastSyncedAt.set(new Date());
        if (!silent && changed.length > 0) this.onChange?.(changed);
        this.schedule(this.interval());
      },
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && (error.status === 401 || error.status === 403)) {
          this.stopped.set(true);
          this.stop();
          return;
        }
        this.failures++;
        this.schedule(Math.min(this.interval() * 2 ** this.failures, SYNC_MAX_BACKOFF_MS));
      },
    });
  }

  private diff(next: TransferAssistanceSyncState['sections']): TransferAssistanceSyncSection[] {
    if (this.tokens === null) return [];
    const keys = new Set([...Object.keys(this.tokens), ...Object.keys(next)]) as Set<TransferAssistanceSyncSection>;
    return [...keys].filter((key) => this.tokens?.[key] !== next[key]);
  }

  private interval(): number {
    return this.activeCase ? SYNC_ACTIVE_INTERVAL_MS : SYNC_IDLE_INTERVAL_MS;
  }

  private schedule(delay: number): void {
    this.clearTimer();
    if (!this.running) return;
    this.timer = setTimeout(() => this.tick(), delay);
  }

  private clearTimer(): void {
    if (this.timer !== null) clearTimeout(this.timer);
    this.timer = null;
  }
}
