import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Subject } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { PaginationMeta } from '@shared/models/pagination.model';
import { DocumentDto, DocumentFilters, DocumentStatsDto } from '@models/document.model';
import { DocumentsApiService } from '../services/documents-api.service';

interface DocumentsState {
  documents: DocumentDto[];
  pagination: PaginationMeta;
  stats: DocumentStatsDto | null;
  filters: DocumentFilters;
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class DocumentsStore {
  private readonly api = inject(DocumentsApiService);
  private readonly _state = signal<DocumentsState>({
    documents: [],
    pagination: { page: 1, limit: 20, total: 0 },
    stats: null,
    filters: { page: 1, limit: 20 },
    loading: false,
    error: null,
  });

  readonly documents = computed(() => this._state().documents);
  readonly pagination = computed(() => this._state().pagination);
  readonly stats = computed(() => this._state().stats);
  readonly filters = computed(() => this._state().filters);
  readonly loading = computed(() => this._state().loading);
  readonly error = computed(() => this._state().error);

  private readonly loadTrigger$ = new Subject<DocumentFilters>();
  private readonly statsTrigger$ = new Subject<void>();

  constructor() {
    this.loadTrigger$.pipe(
      tap(() => this._state.update(s => ({ ...s, loading: true, error: null }))),
      switchMap(filters => this.api.list(filters).pipe(
        tap(res => this._state.update(s => ({
          ...s, documents: res.data, pagination: res.pagination, loading: false,
        }))),
        catchError(() => {
          this._state.update(s => ({ ...s, loading: false, error: 'Erreur lors du chargement des documents.' }));
          return EMPTY;
        }),
      )),
      takeUntilDestroyed(),
    ).subscribe();

    this.statsTrigger$.pipe(
      switchMap(() => this.api.stats().pipe(
        tap(stats => this._state.update(s => ({ ...s, stats }))),
        catchError(() => EMPTY),
      )),
      takeUntilDestroyed(),
    ).subscribe();
  }

  load(filters?: Partial<DocumentFilters>): void {
    const f = { ...this._state().filters, ...filters };
    this._state.update(s => ({ ...s, filters: f }));
    this.loadTrigger$.next(f);
  }

  loadStats(): void { this.statsTrigger$.next(); }

  upsert(doc: DocumentDto): void {
    this._state.update(s => {
      const idx = s.documents.findIndex(x => x.id === doc.id);
      const documents = idx >= 0
        ? s.documents.map(x => x.id === doc.id ? doc : x)
        : [doc, ...s.documents];
      return { ...s, documents };
    });
  }

  remove(id: string): void {
    this._state.update(s => ({ ...s, documents: s.documents.filter(x => x.id !== id) }));
  }
}
