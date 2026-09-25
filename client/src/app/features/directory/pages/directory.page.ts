import {
  ChangeDetectionStrategy, Component, OnInit,
  inject, input, signal, computed,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { LangSwitcherComponent } from '@shared/components/lang-switcher/lang-switcher.component';
import { DirectoryApiService, DirectoryMember } from '../services/directory-api.service';

@Component({
  selector: 'app-directory',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink, FormsModule, ButtonModule,
    InputTextModule, IconFieldModule, InputIconModule, ProgressSpinnerModule,
    TranslatePipe, LangSwitcherComponent,
  ],
  template: `
    <div class="min-h-screen bg-gray-50 dark:bg-gray-950">

      <!-- ── Navbar ── -->
      <header class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800
                     px-6 py-4 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <a routerLink="/" class="flex items-center gap-2 no-underline">
            <div class="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center">
              <i class="pi pi-globe text-white text-sm"></i>
            </div>
            <span class="font-bold text-gray-900 dark:text-white">Expatriate<span class="text-primary-600">365</span></span>
          </a>
        </div>
        <div class="flex items-center gap-3">
          <app-lang-switcher />
          <a routerLink="/auth/login">
            <p-button [label]="'auth.login' | translate" icon="pi pi-sign-in" severity="secondary" size="small" />
          </a>
        </div>
      </header>

      <main class="max-w-5xl mx-auto px-6 py-10">

        <!-- ── En-tête ── -->
        <div class="mb-8">
          <div class="flex items-center gap-2 text-sm text-gray-400 mb-3">
            <a routerLink="/explore" class="hover:text-primary-600">{{ 'explore.title' | translate }}</a>
            <i class="pi pi-chevron-right text-xs"></i>
            <span class="text-gray-600 dark:text-gray-300 font-medium">{{ slug() }}</span>
            <i class="pi pi-chevron-right text-xs"></i>
            <span>{{ 'directory.title' | translate }}</span>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
            {{ 'directory.title' | translate }}
          </h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1">{{ 'directory.subtitle' | translate }}</p>
        </div>

        <!-- ── Filtres ── -->
        <div class="flex flex-col sm:flex-row gap-3 mb-8">
          <p-iconfield class="flex-1">
            <p-inputicon styleClass="pi pi-search" />
            <input pInputText [(ngModel)]="search" (ngModelChange)="onFilter()"
                   [placeholder]="'directory.search_placeholder' | translate"
                   class="w-full" />
          </p-iconfield>
          @if (professions().length) {
            <select
              [(ngModel)]="professionFilter"
              (ngModelChange)="onFilter()"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-700
                     bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 text-sm">
              <option value="">{{ 'directory.all_professions' | translate }}</option>
              @for (p of professions(); track p) {
                <option [value]="p">{{ p }}</option>
              }
            </select>
          }
        </div>

        <!-- ── Contenu ── -->
        @if (loading()) {
          <div class="flex justify-center py-20">
            <p-progressspinner strokeWidth="4" />
          </div>
        } @else if (filtered().length === 0) {
          <div class="text-center py-20 text-gray-400">
            <i class="pi pi-users text-4xl mb-3 block"></i>
            <p>{{ 'directory.empty' | translate }}</p>
          </div>
        } @else {
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            @for (m of filtered(); track m.id) {
              <div class="bg-white dark:bg-gray-900 border border-gray-100 dark:border-gray-800
                          rounded-2xl p-5 flex gap-4 items-start hover:shadow-md transition-shadow">
                <!-- Avatar -->
                @if (m.photo_url) {
                  <img [src]="m.photo_url" [alt]="m.first_name"
                       class="w-14 h-14 rounded-xl object-cover shrink-0" />
                } @else {
                  <div class="w-14 h-14 rounded-xl bg-primary-100 dark:bg-primary-900/30
                              flex items-center justify-center shrink-0 text-xl font-bold
                              text-primary-600 dark:text-primary-400">
                    {{ m.first_name[0] }}{{ m.last_name[0] }}
                  </div>
                }
                <!-- Infos -->
                <div class="flex-1 min-w-0">
                  <p class="font-semibold text-gray-900 dark:text-white truncate">
                    {{ m.first_name }} {{ m.last_name }}
                  </p>
                  @if (m.profession) {
                    <p class="text-sm text-primary-600 dark:text-primary-400 font-medium mt-0.5 truncate">
                      {{ m.profession }}
                    </p>
                  }
                  @if (m.address) {
                    <p class="text-xs text-gray-400 mt-1 flex items-center gap-1 truncate">
                      <i class="pi pi-map-marker text-xs"></i> {{ m.address }}
                    </p>
                  }
                </div>
              </div>
            }
          </div>

          <p class="text-center text-sm text-gray-400 mt-8">
            {{ filtered().length }} {{ 'directory.results' | translate }}
          </p>
        }
      </main>
    </div>
  `,
})
export class DirectoryPage implements OnInit {
  private readonly api = inject(DirectoryApiService);

  readonly slug = input.required<string>();

  readonly members  = signal<DirectoryMember[]>([]);
  readonly loading  = signal(true);
  search            = '';
  professionFilter  = '';

  readonly professions = computed(() =>
    [...new Set(this.members().map(m => m.profession).filter(Boolean) as string[])].sort()
  );

  readonly filtered = computed(() => {
    const s = this.search.toLowerCase();
    const p = this.professionFilter.toLowerCase();
    return this.members().filter(m => {
      const matchSearch = !s ||
        `${m.first_name} ${m.last_name}`.toLowerCase().includes(s) ||
        (m.profession ?? '').toLowerCase().includes(s);
      const matchProf = !p || (m.profession ?? '').toLowerCase() === p;
      return matchSearch && matchProf;
    });
  });

  ngOnInit(): void {
    this.api.getDirectory(this.slug()).subscribe({
      next: (data) => { this.members.set(data); this.loading.set(false); },
      error: ()    => this.loading.set(false),
    });
  }

  onFilter(): void {
    // le computed réagit automatiquement — rien à faire ici
    // mais on force la détection pour ngModel
  }
}
