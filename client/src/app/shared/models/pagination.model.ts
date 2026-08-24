export interface PaginationMeta {
  page: number;
  limit: number;
  total: number;
}

export interface PagedResult<T> {
  data: T[];
  pagination: PaginationMeta;
}

export const DEFAULT_PAGE_SIZE = 20;
