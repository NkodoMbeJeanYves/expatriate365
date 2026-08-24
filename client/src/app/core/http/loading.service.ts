import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private counter = signal(0);
  readonly isLoading = computed(() => this.counter() > 0);

  show(): void { this.counter.update((n) => n + 1); }
  hide(): void { this.counter.update((n) => Math.max(0, n - 1)); }
}
