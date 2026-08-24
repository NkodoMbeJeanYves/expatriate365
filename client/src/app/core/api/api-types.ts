export interface PaginationMeta {
  page: number;
  limit: number;
  total: number;
}

export interface PagedResult<T> {
  data: T[];
  pagination: PaginationMeta;
}

export interface ApiError {
  error: string;
}

export function extractApiError(err: unknown): string {
  if (err && typeof err === 'object' && 'error' in err) {
    const e = err as { error: unknown };
    if (e.error && typeof e.error === 'object' && 'error' in (e.error as object)) {
      return (e.error as ApiError).error;
    }
    if (typeof e.error === 'string') return e.error;
  }
  return 'Une erreur est survenue';
}
