import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppSidebarComponent } from './components/app-sidebar';
import { AppNavbarComponent } from './components/app-navbar';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, AppSidebarComponent, AppNavbarComponent, ToastModule],
  template: `
    <div class="flex h-screen overflow-hidden bg-gray-50 dark:bg-gray-950">
      <app-sidebar [collapsed]="sidebarCollapsed()" />
      <div class="flex flex-col flex-1 overflow-hidden min-w-0">
        <app-navbar (toggleSidebar)="sidebarCollapsed.update((v) => !v)" />
        <main class="flex-1 overflow-y-auto p-4 sm:p-6">
          <router-outlet />
        </main>
      </div>
    </div>
    <p-toast position="bottom-left" />
  `,
})
export class MainLayoutComponent {
  readonly sidebarCollapsed = signal(false);
}
