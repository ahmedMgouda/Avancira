// ════════════════════════════════════════════════════════════════════════════════
// CATEGORY MODELS - UPDATED WITH AUTO-SORTORDER
// ════════════════════════════════════════════════════════════════════════════════

export interface Category {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  sortOrder: number; // Read-only, managed by backend
  createdAt?: Date;
  updatedAt?: Date;
}

export interface CategoryCreateDto {
  name: string;
  description?: string;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  
  // NEW: Position control (optional)
  insertPosition?: 'start' | 'end' | 'custom';
  customPosition?: number;
  
  // REMOVED: sortOrder - auto-assigned by backend
}

export interface CategoryUpdateDto {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  isVisible: boolean;
  isFeatured: boolean;
  
  // REMOVED: sortOrder - changed via reorder/move only
}

export interface CategoryFilter {
  searchTerm?: string;
  isActive?: boolean;
  isVisible?: boolean;
  isFeatured?: boolean;
  pageIndex: number;
  pageSize: number;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
}
