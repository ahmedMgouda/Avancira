
export interface BaseFilter {
  searchTerm?: string;
  pageIndex?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface PaginatedResult<T> {
  items: readonly T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}