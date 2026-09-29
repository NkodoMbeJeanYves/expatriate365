import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4">
      <h1 class="text-4xl font-bold text-color">403</h1>
      <p class="text-color-secondary">Vous n'avez pas accès à cette page.</p>
      <p-button label="Retour au tableau de bord" (onClick)="router.navigateByUrl('/dashboard')" />
    </div>
  `,
})
export class ForbiddenComponent {
  protected readonly router = inject(Router);
}
