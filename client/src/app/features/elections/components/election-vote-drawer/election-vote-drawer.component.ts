import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { CheckboxModule } from 'primeng/checkbox';
import { FormsModule } from '@angular/forms';
import { ElectionDto, ElectionCandidateDto } from '@models/election.model';
import { ElectionsApiService } from '../../services/elections-api.service';

@Component({
  selector: 'app-election-vote-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DrawerModule, ButtonModule, CheckboxModule, TranslatePipe],
  template: `
    <p-drawer [(visible)]="visible" [position]="'right'" [style]="{ width: '520px' }" [header]="election()?.title">
      <div class="p-4 flex flex-col gap-6">
        @if (voted()) {
          <div class="flex flex-col items-center gap-4 py-10 text-center">
            <i class="pi pi-check-circle text-green-500 text-5xl"></i>
            <h3 class="text-lg font-semibold text-green-700">{{ 'elections.vote_success' | translate }}</h3>
            <p class="text-sm text-gray-500">Votre vote a été pris en compte de manière anonyme.</p>
          </div>
        } @else {
          <div class="flex flex-col gap-2">
            <p class="text-sm text-gray-600">
              Sélectionnez jusqu'à <strong>{{ election()?.max_choices }}</strong> candidat(s).
              <span class="ml-2 font-medium" [class.text-red-500]="selectedIds().length > (election()?.max_choices ?? 1)">
                {{ selectedIds().length }} / {{ election()?.max_choices }} sélectionné(s)
              </span>
            </p>
          </div>

          <div class="flex flex-col gap-3">
            @for (c of candidates(); track c.id) {
              <label class="flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition-colors"
                [class.border-indigo-400]="isSelected(c.id)"
                [class.bg-indigo-50]="isSelected(c.id)"
                [class.border-gray-100]="!isSelected(c.id)"
                [class.bg-white]="!isSelected(c.id)">
                <p-checkbox [binary]="true" [ngModel]="isSelected(c.id)" (ngModelChange)="toggleCandidate(c.id, $event)" />
                <div class="flex flex-col gap-0.5">
                  <div class="font-medium text-sm">{{ c.member_name }}</div>
                  <div class="font-mono text-xs text-gray-400">{{ c.membership_number }}</div>
                  @if (c.statement) { <p class="text-xs text-gray-500 mt-1">{{ c.statement }}</p> }
                </div>
              </label>
            }
          </div>

          @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }

          <div class="flex justify-end">
            <p-button
              [label]="'common.confirm' | translate"
              icon="pi pi-check"
              [loading]="voting()"
              [disabled]="selectedIds().length === 0 || selectedIds().length > (election()?.max_choices ?? 1)"
              (onClick)="confirmVote()" />
          </div>
        }
      </div>
    </p-drawer>
  `,
})
export class ElectionVoteDrawerComponent {
  private readonly api = inject(ElectionsApiService);

  readonly votecast = output<void>();

  protected visible = false;
  protected readonly election = signal<ElectionDto | null>(null);
  protected readonly candidates = signal<ElectionCandidateDto[]>([]);
  protected readonly selectedIds = signal<string[]>([]);
  protected readonly voting = signal(false);
  protected readonly voted = signal(false);
  protected readonly error = signal<string | null>(null);

  open(e: ElectionDto, candidates: ElectionCandidateDto[]): void {
    this.election.set(e);
    this.candidates.set(candidates);
    this.selectedIds.set([]);
    this.voted.set(false);
    this.error.set(null);
    this.visible = true;
  }

  protected isSelected(id: string): boolean { return this.selectedIds().includes(id); }

  protected toggleCandidate(id: string, checked: boolean): void {
    this.selectedIds.update(ids => checked ? [...ids, id] : ids.filter(x => x !== id));
  }

  protected confirmVote(): void {
    if (!this.election() || this.selectedIds().length === 0) return;
    this.voting.set(true);
    this.api.castVote(this.election()!.id, { candidate_ids: this.selectedIds() }).subscribe({
      next: () => { this.voting.set(false); this.voted.set(true); this.votecast.emit(); },
      error: () => { this.voting.set(false); this.error.set('Erreur lors de l\'enregistrement du vote.'); },
    });
  }
}
