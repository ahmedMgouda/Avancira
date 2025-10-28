export interface Category {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  sortOrder: number;
  createdAt?: string | Date | null;
  updatedAt?: string | Date | null;
}

export interface CategoryCreateDto {
  name: string;
  description?: string | null;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  sortOrder: number;
}

export interface CategoryUpdateDto extends CategoryCreateDto {
  id: number;
}

export interface CategoryFilter {
  searchTerm?: string;
  isActive?: boolean;
  isVisible?: boolean;
  isFeatured?: boolean;
}

export interface PaginatedResult<T> {
  items: readonly T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}
