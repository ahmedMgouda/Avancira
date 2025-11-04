import { BaseFilter } from "../core/models/base.model";

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

/** DTO for creating a category */
export interface CategoryCreateDto {
  name: string;
  description?: string | null;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  sortOrder: number;
}

/** DTO for updating a category */
export interface CategoryUpdateDto extends CategoryCreateDto {
  id: number;
}

/** Optional filter for category listing */
export interface CategoryFilter extends BaseFilter {
  isActive?: boolean;
  isVisible?: boolean;
  isFeatured?: boolean;
}
