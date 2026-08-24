import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, output, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { ElectionDto, ElectionCandidateDto } from '@models/election.model';
import { ElectionsApiService } from '../../services/elections-api.service';
import { MembersApiService } from '@members/services/members-api.service';
import { MemberListItem } from '@models/member.model';
import { PagedResult } from '@shared/models/pagination.model';

@Component({
  selector: 'app-election-candidates-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, DrawerModule, ButtonModule, SelectModule, TextareaModule, InputNumberModule, TranslatePipe],
  template: `
    <p-drawer #drawerEl [(visible)]="visible" [position]="'right'" [style]="{ width: '600px' }" [header]="election()?.title + ' — Candidats'">
      <div class="p-4 flex flex-col gap-4">
        @if (election()?.status === 'draft') {
          <div class="bg-gray-50 rounded-xl border p-4 flex flex-col gap-3">
            <h4 class="font-medium text-sm">{{ 'elections.add_candidate' | translate }}</h4>
            <div class="flex flex-col gap-1">
              <label class="text-xs text-gray-600">{{ 'elections.candidate_member' | translate }} *</label>
              <p-select [options]="members()" optionLabel="label" optionValue="value"
                [(ngModel)]="selectedMemberId"
                [filter]="true" filterBy="label"
                [placeholder]="'Rechercher un membre…'" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="text-xs text-gray-600">{{ 'elections.candidate_statement' | translate }}</label>
              <textarea pTextarea [(ngModel)]="newStatement" rows="2" placeholder="Présentation du candidat..."></textarea>
            </div>
            <div class="flex items-center justify-between gap-3">
              <div class="flex flex-col gap-1 w-32">
                <label class="text-xs text-gray-600">{{ 'elections.candidate_order' | translate }}</label>
                <p-inputnumber [(ngModel)]="newOrder" [min]="0" />
              </div>
              <p-button [label]="'common.add' | translate" icon="pi pi-plus" [loading]="adding()" (onClick)="addCandidate()" />
            </div>
          </div>
        }

        <div class="flex flex-col gap-2">
          @for (c of candidates(); track c.id) {
            <div class="bg-white border border-gray-100 rounded-xl p-3 flex items-start justify-between gap-3">
              <div class="flex flex-col gap-0.5">
                <div class="font-medium text-sm">{{ c.member_name }}</div>
                <div class="font-mono text-xs text-gray-400">{{ c.membership_number }}</div>
                @if (c.statement) { <p class="text-xs text-gray-500 mt-1">{{ c.statement }}</p> }
              </div>
              <div class="flex items-center gap-2 shrink-0">
                <span class="text-xs text-gray-400">Ordre {{ c.display_order }}</span>
                @if (election()?.status === 'draft') {
                  <p-button icon="pi pi-times" severity="danger" [text]="true" size="small" (onClick)="removeCandidate(c)" />
                }
              </div>
            </div>
          } @empty {
            <div class="text-center py-8 text-gray-400 text-sm">{{ 'elections.no_candidates' | translate }}</div>
          }
        </div>
        @if (error()) { <p class="text-red-500 text-sm">{{ error() }}</p> }
      </div>
    </p-drawer>
  `,
})
export class ElectionCandidatesDrawerComponent {
  private readonly api = inject(ElectionsApiService);
  private readonly membersApi = inject(MembersApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly drawerRef = viewChild<Drawer>('drawerEl');

  readonly candidateAdded = output<void>();

  protected visible = false;
  protected readonly election = signal<ElectionDto | null>(null);
  protected readonly candidates = signal<ElectionCandidateDto[]>([]);
  protected readonly adding = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly members = signal<{ label: string; value: string }[]>([]);

  protected selectedMemberId = '';
  protected newStatement = '';
  protected newOrder = 0;

  open(e: ElectionDto, candidates: ElectionCandidateDto[]): void {
    this.election.set(e);
    this.candidates.set([...candidates]);
    this.error.set(null);
    this.selectedMemberId = '';
    this.newStatement = '';
    this.newOrder = candidates.length;
    this.visible = true;
    this.cdr.detectChanges();
    this.membersApi.list({ page: 1, limit: 500, status: 'active' }).subscribe({
      next: (res: PagedResult<MemberListItem>) => this.members.set(res.data.map((m: MemberListItem) => ({
        label: `${m.first_name} ${m.last_name} (${m.membership_number})`,
        value: m.id,
      }))),
    });
  }

  protected addCandidate(): void {
    if (!this.selectedMemberId || !this.election()) return;
    this.adding.set(true);
    this.api.addCandidate(this.election()!.id, {
      member_id: this.selectedMemberId,
      statement: this.newStatement.trim() || undefined,
      display_order: this.newOrder,
    }).subscribe({
      next: c => {
        this.candidates.update(list => [...list, c]);
        this.selectedMemberId = '';
        this.newStatement = '';
        this.newOrder = this.candidates().length;
        this.adding.set(false);
        this.candidateAdded.emit();
      },
      error: () => { this.adding.set(false); this.error.set("Erreur lors de l'ajout."); },
    });
  }

  protected removeCandidate(c: ElectionCandidateDto): void {
    this.api.removeCandidate(this.election()!.id, c.id).subscribe({
      next: () => { this.candidates.update(list => list.filter(x => x.id !== c.id)); this.candidateAdded.emit(); },
      error: () => this.error.set('Erreur lors de la suppression.'),
    });
  }
}
