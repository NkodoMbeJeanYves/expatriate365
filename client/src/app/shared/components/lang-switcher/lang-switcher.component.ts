import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TranslateService } from '@ngx-translate/core';
import { AuthStore } from '@core/auth/auth.store';
import { APP_CONFIG } from '@core/config/app-config.token';

type Lang = 'fr' | 'en';
const LANGS: Lang[] = ['fr', 'en'];

@Component({
  selector: 'app-lang-switcher',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center rounded-lg border border-gray-200 dark:border-gray-700
                overflow-hidden text-xs font-medium">
      @for (lang of langs; track lang) {
        <button type="button"
          (click)="setLang(lang)"
          [class]="currentLang() === lang
            ? 'px-2.5 py-1.5 bg-emerald-600 text-white'
            : 'px-2.5 py-1.5 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors'">
          {{ lang.toUpperCase() }}
        </button>
      }
    </div>
  `,
})
export class LangSwitcherComponent {
  private readonly translate = inject(TranslateService);
  private readonly http      = inject(HttpClient);
  private readonly authStore = inject(AuthStore);
  private readonly config    = inject(APP_CONFIG);

  readonly langs       = LANGS;
  readonly currentLang = signal<Lang>((localStorage.getItem('exp365_lang') ?? 'fr') as Lang);

  setLang(lang: Lang): void {
    this.translate.use(lang);
    this.currentLang.set(lang);
    localStorage.setItem('exp365_lang', lang);

    if (this.authStore.isAuthenticated()) {
      this.http.patch(`${this.config.apiUrl}/api/v1/auth/language`, { preferred_language: lang }).subscribe();
    }
  }
}
