import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ButtonModule],
  template: `
    <div class="flex flex-col items-center justify-center min-h-screen gap-4">
      <h1 class="text-4xl font-bold text-color">403</h1>
      <p class="text-color-secondary">Vous n'avez pas accès à cette page.</p>
      <p-button label="Retour au tableau de bord" routerLink="/dashboard" />
    </div>
  `,
})
export class ForbiddenComponent {}
