import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: `
    <div class="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-teal-50
                dark:from-gray-950 dark:via-gray-900 dark:to-gray-950
                flex items-center justify-center p-4">

      <!-- Background decoration -->
      <div class="absolute inset-0 overflow-hidden pointer-events-none" aria-hidden="true">
        <div class="absolute -top-40 -right-40 w-80 h-80 rounded-full
                    bg-emerald-100 dark:bg-emerald-900/20 blur-3xl opacity-60"></div>
        <div class="absolute -bottom-40 -left-40 w-80 h-80 rounded-full
                    bg-teal-100 dark:bg-teal-900/20 blur-3xl opacity-60"></div>
      </div>

      <div class="relative z-10 w-full max-w-md">
        <!-- Logo + title -->
        <div class="text-center mb-8">
          <div class="inline-flex items-center justify-center w-14 h-14 rounded-2xl
                      bg-gradient-to-br from-emerald-500 to-teal-600 shadow-lg mb-4">
            <span class="text-xl font-bold text-white">E</span>
          </div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Expatriate365</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Gérez votre communauté en toute transparence</p>
        </div>

        <router-outlet />
      </div>
    </div>
  `,
})
export class AuthLayoutComponent {}
