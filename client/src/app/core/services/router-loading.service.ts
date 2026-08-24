import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class RouterLoadingService {
  private readonly router = inject(Router);
  private readonly _loading = signal(false);

  readonly loading = computed(() => this._loading());

  constructor() {
    this.router.events.pipe(
      filter(e =>
        e instanceof NavigationStart ||
        e instanceof NavigationEnd ||
        e instanceof NavigationCancel ||
        e instanceof NavigationError,
      ),
      takeUntilDestroyed(),
    ).subscribe(e => {
      console.log('[RouterLoadingService] Navigation event', e);
      this._loading.set(e instanceof NavigationStart);
    });
  }
}
