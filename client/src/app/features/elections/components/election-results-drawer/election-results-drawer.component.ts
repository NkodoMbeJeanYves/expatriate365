import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DrawerModule } from 'primeng/drawer';
import { ElectionDto, ElectionResultDto } from '@models/election.model';

@Component({
  selector: 'app-election-results-drawer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DrawerModule, ButtonModule, TranslatePipe],
  template: `
    <p-drawer [(visible)]="visible" [position]="'right'" [style]="{ width: '560px' }" [header]="election()?.title + ' — Résultats'">
      <div class="p-4 flex flex-col gap-4">
        <div class="text-sm text-gray-500 text-center">{{ election()?.vote_count }} vote(s) exprimé(s)</div>

        <!-- Podium top 3 -->
        @if (results().length >= 1) {
          <div class="flex items-end justify-center gap-4 py-6">
            @if (results().length >= 2) {
              <div class="flex flex-col items-center gap-2">
                <div class="w-14 h-14 rounded-full bg-gray-100 border-2 border-gray-300 flex items-center justify-center text-lg font-bold text-gray-500">2</div>
                <div class="text-xs text-center font-medium text-gray-700">{{ results()[1].member_name }}</div>
                <div class="bg-gray-200 rounded-t-lg w-16 flex items-end justify-center pb-1 text-xs font-bold text-gray-600" [style.height.px]="60">{{ results()[1].vote_count }}</div>
              </div>
            }
            <div class="flex flex-col items-center gap-2">
              <i class="pi pi-trophy text-yellow-500 text-2xl"></i>
              <div class="w-16 h-16 rounded-full bg-yellow-50 border-2 border-yellow-400 flex items-center justify-center text-xl font-bold text-yellow-600">1</div>
              <div class="text-xs text-center font-semibold text-gray-800">{{ results()[0].member_name }}</div>
              <div class="bg-yellow-400 rounded-t-lg w-16 flex items-end justify-center pb-1 text-xs font-bold text-white" [style.height.px]="90">{{ results()[0].vote_count }}</div>
            </div>
            @if (results().length >= 3) {
              <div class="flex flex-col items-center gap-2">
                <div class="w-12 h-12 rounded-full bg-orange-50 border-2 border-orange-300 flex items-center justify-center text-base font-bold text-orange-500">3</div>
                <div class="text-xs text-center font-medium text-gray-700">{{ results()[2].member_name }}</div>
                <div class="bg-orange-200 rounded-t-lg w-16 flex items-end justify-center pb-1 text-xs font-bold text-orange-700" [style.height.px]="45">{{ results()[2].vote_count }}</div>
              </div>
            }
          </div>
        }

        <!-- Tableau complet -->
        <div class="flex flex-col gap-2">
          @for (r of results(); track r.candidate_id) {
            <div class="flex items-center gap-3 p-2 rounded-lg bg-gray-50">
              <div class="w-6 text-center text-xs font-bold text-gray-500">{{ r.rank }}</div>
              <div class="flex-1 min-w-0">
                <div class="text-sm font-medium text-gray-800 truncate">{{ r.member_name }}</div>
                <div class="w-full bg-gray-200 rounded-full h-1.5 mt-1">
                  <div class="bg-indigo-500 h-1.5 rounded-full" [style.width.%]="r.percentage"></div>
                </div>
              </div>
              <div class="text-right shrink-0">
                <div class="text-sm font-bold text-gray-800">{{ r.vote_count }}</div>
                <div class="text-xs text-gray-400">{{ r.percentage }}%</div>
              </div>
            </div>
          } @empty {
            <div class="text-center py-8 text-gray-400 text-sm">{{ 'common.no_data' | translate }}</div>
          }
        </div>
      </div>
    </p-drawer>
  `,
})
export class ElectionResultsDrawerComponent {
  protected visible = false;
  protected readonly election = signal<ElectionDto | null>(null);
  protected readonly results = signal<ElectionResultDto[]>([]);

  open(e: ElectionDto, results: ElectionResultDto[]): void {
    this.election.set(e);
    this.results.set(results);
    this.visible = true;
  }
}
