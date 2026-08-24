import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';

export interface PageChangeEvent {
  page: number;
  limit: number;
}

@Component({
  selector: 'app-paginator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PaginatorModule],
  template: `
    <p-paginator
      [rows]="limit()"
      [totalRecords]="total()"
      [first]="(page() - 1) * limit()"
      [rowsPerPageOptions]="[10, 20, 50]"
      (onPageChange)="onPageChange($event)"
      styleClass="border-t border-gray-200 dark:border-gray-700" />
  `,
})
export class AppPaginatorComponent {
  readonly page  = input.required<number>();
  readonly limit = input.required<number>();
  readonly total = input.required<number>();

  readonly pageChange = output<PageChangeEvent>();

  onPageChange(event: PaginatorState): void {
    const first = event.first ?? 0;
    const rows  = event.rows  ?? this.limit();
    this.pageChange.emit({ page: Math.floor(first / rows) + 1, limit: rows });
  }
}
