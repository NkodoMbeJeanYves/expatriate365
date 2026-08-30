import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

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

  readonly langs      = LANGS;
  readonly currentLang = signal<Lang>((localStorage.getItem('exp365_lang') ?? 'fr') as Lang);

  setLang(lang: Lang): void {
    this.translate.use(lang);
    this.currentLang.set(lang);
    localStorage.setItem('exp365_lang', lang);
  }
}
