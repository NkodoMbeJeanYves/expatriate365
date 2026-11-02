import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { ShowcaseReport, ShowcasesApiService } from '../services/showcases-api.service';

@Component({
  selector: 'app-showcase-moderation-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, TranslatePipe],
  template: `
    <div class="mx-auto max-w-5xl space-y-6">
      <header>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ 'showcases.moderation_title' | translate }}</h1>
        <p class="mt-1 text-sm text-gray-500">{{ 'showcases.moderation_intro' | translate }}</p>
      </header>
      @if (error()) {
        <p class="rounded-lg bg-red-50 p-4 text-red-700">{{ 'showcases.moderation_error' | translate }}</p>
      }
      @if (loading()) {
        <p class="py-10 text-center text-gray-500">{{ 'common.loading' | translate }}</p>
      } @else if (reports().length === 0) {
        <p class="rounded-xl border border-gray-200 p-8 text-center text-gray-500">{{ 'showcases.no_reports' | translate }}</p>
      } @else {
        @for (report of reports(); track report.id) {
          <article class="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900">
            <div class="flex flex-wrap items-start justify-between gap-4">
              <div>
                <h2 class="font-semibold text-gray-900 dark:text-white">{{ report.showcase_title }}</h2>
                <p class="mt-1 whitespace-pre-line text-sm text-gray-600 dark:text-gray-300">{{ report.reason }}</p>
                <time class="mt-2 block text-xs text-gray-500">{{ report.created_at | date:'medium' }}</time>
              </div>
              <div class="flex gap-2">
                <button class="rounded-lg bg-red-700 px-3 py-2 text-sm text-white disabled:opacity-50" [disabled]="workingId() === report.id" (click)="resolve(report, 'hide')">{{ 'showcases.hide' | translate }}</button>
                <button class="rounded-lg border px-3 py-2 text-sm disabled:opacity-50" [disabled]="workingId() === report.id" (click)="resolve(report, 'dismiss')">{{ 'showcases.dismiss' | translate }}</button>
              </div>
            </div>
          </article>
        }
      }
    </div>
  `,
})
export class ShowcaseModerationPage implements OnInit {
  private readonly api = inject(ShowcasesApiService);
  readonly reports = signal<ShowcaseReport[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly workingId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  resolve(report: ShowcaseReport, action: 'hide' | 'dismiss'): void {
    this.workingId.set(report.id);
    this.error.set(false);
    this.api.resolveReport(report.id, action).subscribe({
      next: () => {
        this.reports.update((reports) => reports.filter((item) => item.id !== report.id));
        this.workingId.set(null);
      },
      error: () => {
        this.error.set(true);
        this.workingId.set(null);
      },
    });
  }

  private load(): void {
    this.api.getReports().subscribe({
      next: (result) => {
        this.reports.set(result.data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }
}
