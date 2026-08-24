import { ChangeDetectionStrategy, Component, OnInit, inject, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TooltipModule } from 'primeng/tooltip';
import { ElectionDto, ELECTION_STATUSES, ELECTION_TYPES } from '@models/election.model';
import { ElectionsStore } from '../../store/elections.store';
import { ElectionsApiService } from '../../services/elections-api.service';
import { ElectionFormDrawerComponent } from '../../components/election-form-drawer/election-form-drawer.component';
import { ElectionCandidatesDrawerComponent } from '../../components/election-candidates-drawer/election-candidates-drawer.component';
import { ElectionVoteDrawerComponent } from '../../components/election-vote-drawer/election-vote-drawer.component';
import { ElectionResultsDrawerComponent } from '../../components/election-results-drawer/election-results-drawer.component';
import { ToastService } from '@service/toast.service';
import { AppPaginatorComponent, PageChangeEvent } from '@shared/components/paginator/app-paginator.component';

@Component({
  selector: 'app-election-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule, FormsModule, ButtonModule, TagModule,
    SelectModule, ProgressSpinnerModule, TooltipModule,
    ElectionFormDrawerComponent, ElectionCandidatesDrawerComponent,
    ElectionVoteDrawerComponent, ElectionResultsDrawerComponent,
    AppPaginatorComponent,
    TranslatePipe,
  ],
  template: `
    <div class="p-6 flex flex-col gap-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'elections.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'elections.subtitle' | translate }}</p>
        </div>
        <p-button [label]="'elections.new' | translate" icon="pi pi-plus" (onClick)="openForm()" />
      </div>

      <!-- Stats -->
      @if (store.stats(); as s) {
        <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-800">{{ s.total }}</div>
            <div class="text-xs text-gray-500 mt-1">Total</div>
          </div>
          <div class="bg-gray-50 rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-600">{{ s.draft }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'elections.status_draft' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ s.open }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'elections.status_open' | translate }}</div>
          </div>
          <div class="bg-orange-50 rounded-xl p-4 border border-orange-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-orange-700">{{ s.closed }}</div>
            <div class="text-xs text-orange-600 mt-1">{{ 'elections.status_closed' | translate }}</div>
          </div>
          <div class="bg-indigo-50 rounded-xl p-4 border border-indigo-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-indigo-700">{{ s.results_published }}</div>
            <div class="text-xs text-indigo-600 mt-1">{{ 'elections.status_results_published' | translate }}</div>
          </div>
        </div>
      }

      <!-- Filtres -->
      <div class="flex gap-3 flex-wrap">
        <p-select [options]="statusOptions" [(ngModel)]="filterStatus" optionLabel="label" optionValue="value"
          placeholder="Tous les statuts" [showClear]="true" (onChange)="applyFilters()" />
        <p-select [options]="typeOptions" [(ngModel)]="filterType" optionLabel="label" optionValue="value"
          placeholder="Tous les types" [showClear]="true" (onChange)="applyFilters()" />
      </div>

      <!-- Liste -->
      @if (store.loading()) {
        <div class="flex justify-center py-12"><p-progressspinner strokeWidth="4" /></div>
      } @else if (store.elections().length === 0) {
        <div class="text-center py-16 text-gray-400">
          <i class="pi pi-chart-bar text-4xl mb-3 block"></i>
          <p>{{ 'elections.no_elections' | translate }}</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          @for (e of store.elections(); track e.id) {
            <div class="bg-white rounded-xl border border-gray-100 shadow-sm hover:shadow-md transition-shadow p-4 flex flex-col gap-3">
              <div class="flex items-start justify-between gap-2">
                <div class="flex flex-col gap-1">
                  <h3 class="font-semibold text-gray-800">{{ e.title }}</h3>
                  @if (e.description) { <p class="text-xs text-gray-400 line-clamp-2">{{ e.description }}</p> }
                </div>
                <p-tag [value]="'elections.status_' + e.status | translate" [severity]="statusSeverity(e.status)" class="shrink-0" />
              </div>

              <div class="flex gap-4 text-xs text-gray-500 flex-wrap">
                <span><i class="pi pi-users mr-1"></i>{{ e.candidate_count }} candidat(s)</span>
                <span><i class="pi pi-check-square mr-1"></i>{{ e.vote_count }} vote(s)</span>
                <span><i class="pi pi-list mr-1"></i>Max {{ e.max_choices }} choix</span>
              </div>

              @if (e.start_date || e.end_date) {
                <div class="text-xs text-gray-400">
                  @if (e.start_date) { <span>Du {{ e.start_date | date:'d MMM y' }}</span> }
                  @if (e.end_date) { <span class="ml-2">au {{ e.end_date | date:'d MMM y' }}</span> }
                </div>
              }

              <!-- Actions -->
              <div class="flex gap-1 border-t border-gray-50 pt-3 flex-wrap">
                <p-button icon="pi pi-users" size="small" severity="secondary" [text]="true"
                  [pTooltip]="'elections.candidates' | translate" (onClick)="openCandidates(e)" />
                @if (e.status === 'results_published') {
                  <p-button icon="pi pi-chart-bar" size="small" severity="info" [text]="true"
                    [pTooltip]="'elections.results' | translate" (onClick)="openResults(e)" />
                }
                @if (e.status === 'open') {
                  <p-button icon="pi pi-check-square" size="small" severity="success" [text]="true"
                    [pTooltip]="'elections.vote' | translate" (onClick)="openVote(e)" />
                  <p-button icon="pi pi-stop-circle" size="small" severity="warn" [text]="true"
                    [pTooltip]="'elections.close_election' | translate" (onClick)="closeElection(e)" />
                }
                @if (e.status === 'draft') {
                  <p-button icon="pi pi-send" size="small" severity="success" [text]="true"
                    [pTooltip]="'elections.open_voting' | translate" (onClick)="openElection(e)" />
                  <p-button icon="pi pi-pencil" size="small" severity="secondary" [text]="true"
                    [pTooltip]="'common.edit' | translate" (onClick)="openForm(e)" />
                }
                @if (e.status === 'closed') {
                  <p-button icon="pi pi-chart-bar" size="small" severity="info" [text]="true"
                    [pTooltip]="'elections.publish_results' | translate" (onClick)="publishResults(e)" />
                }
              </div>
            </div>
          }
        </div>

        @if (store.pagination().total > store.pagination().limit) {
          <app-paginator [page]="store.pagination().page" [limit]="store.pagination().limit" [total]="store.pagination().total" (pageChange)="goToPage($event)" />
        }
      }
    </div>

    <app-election-form-drawer #formDrawer (saved)="onSaved($event)" />
    <app-election-candidates-drawer #candidatesDrawer (candidateAdded)="refreshElection()" />
    <app-election-vote-drawer #voteDrawer (votecast)="onVoteCast()" />
    <app-election-results-drawer #resultsDrawer />
  `,
})
export class ElectionListPage implements OnInit {
  protected readonly store = inject(ElectionsStore);
  private readonly api = inject(ElectionsApiService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);

  private readonly formDrawer = viewChild.required<ElectionFormDrawerComponent>('formDrawer');
  private readonly candidatesDrawer = viewChild.required<ElectionCandidatesDrawerComponent>('candidatesDrawer');
  private readonly voteDrawer = viewChild.required<ElectionVoteDrawerComponent>('voteDrawer');
  private readonly resultsDrawer = viewChild.required<ElectionResultsDrawerComponent>('resultsDrawer');

  protected filterStatus: string | null = null;
  protected filterType: string | null = null;

  protected readonly statusOptions = [...ELECTION_STATUSES].map(s => ({ label: this.statusLabelFn(s), value: s }));
  protected readonly typeOptions = [...ELECTION_TYPES].map(t => ({ label: this.typeLabelFn(t), value: t }));


  ngOnInit(): void { this.store.loadElections(); this.store.loadStats(); }

  protected statusSeverity(s: string): 'success' | 'warn' | 'danger' | 'secondary' | 'info' {
    return s === 'open' ? 'success' : s === 'closed' ? 'warn' : s === 'results_published' ? 'info' : 'secondary';
  }

  protected statusLabel(s: string): string { return this.statusLabelFn(s); }
  protected typeLabel(t: string): string { return this.typeLabelFn(t); }

  private statusLabelFn(s: string): string {
    return this.translate.instant('elections.status_' + s);
  }
  private typeLabelFn(t: string): string {
    return t === 'board' ? this.translate.instant('elections.type_board') : t === 'custom' ? this.translate.instant('elections.type_custom') : t;
  }

  protected openForm(e?: ElectionDto): void { this.formDrawer().open(e); }

  protected openCandidates(e: ElectionDto): void {
    this.api.getById(e.id).subscribe({ next: res => this.candidatesDrawer().open(e, res.candidates) });
  }

  protected openVote(e: ElectionDto): void {
    this.api.getById(e.id).subscribe({ next: res => this.voteDrawer().open(e, res.candidates) });
  }

  protected openResults(e: ElectionDto): void {
    this.api.getById(e.id).subscribe({ next: res => this.resultsDrawer().open(e, res.results) });
  }

  protected applyFilters(): void {
    this.store.loadElections({ page: 1, status: this.filterStatus ?? undefined, type: this.filterType ?? undefined });
  }
  protected goToPage(event: PageChangeEvent): void { this.store.loadElections({ page: event.page }); }
  protected onSaved(e: ElectionDto): void { this.store.upsertElection(e); this.store.loadStats(); }
  protected refreshElection(): void { this.store.loadElections(this.store.filters()); }
  protected onVoteCast(): void { this.toast.success('Votre vote a bien été enregistré.'); this.store.loadElections(this.store.filters()); }

  protected openElection(e: ElectionDto): void {
    this.api.open(e.id).subscribe({
      next: updated => { this.store.upsertElection(updated); this.store.loadStats(); this.toast.success('Vote ouvert.'); },
      error: () => this.toast.error('Erreur lors de l\'ouverture du vote.'),
    });
  }
  protected closeElection(e: ElectionDto): void {
    this.api.close(e.id).subscribe({
      next: updated => { this.store.upsertElection(updated); this.store.loadStats(); this.toast.success('Vote clôturé.'); },
      error: () => this.toast.error('Erreur lors de la clôture du vote.'),
    });
  }
  protected publishResults(e: ElectionDto): void {
    this.api.publishResults(e.id).subscribe({
      next: updated => { this.store.upsertElection(updated); this.store.loadStats(); this.toast.success('Résultats publiés.'); },
      error: () => this.toast.error('Erreur lors de la publication des résultats.'),
    });
  }
}
