import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of, Subject } from 'rxjs';
import { DialogService } from '@core/dialogs';
import { StandardError } from '@core/logging/models/standard-error.model';
import { Category, CategoryFilter } from '@models/category';

import { LoadingService } from '@/core/loading/services/loading.service';
import { LoggerService } from '@core/logging/services/logger.service';
import { ToastManager } from '@core/toast/services/toast-manager.service';
import { CategoryService } from '@services/category.service';

import { LoadingDirective } from '@/core/loading/directives/loading.directive';

/**
 * ═════════════════════════════════════════════════════════════════════════
 * CATEGORY LIST COMPONENT - WITH UNIQUE SORTORDER + SWAP LOGIC
 * ═════════════════════════════════════════════════════════════════════════
 * 
 * FEATURES:
 * ✅ Drag-drop reordering (within current page)
 * ✅ Move to position with swap (works across pages)
 * ✅ Ensures unique sortOrder (no duplicates)
 * ✅ Uses Material Dialog (not browser prompt)
 * ✅ Optimistic UI updates with rollback
 */
@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingDirective, DragDropModule],
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.scss', '../categories.styles.scss']
})
export class CategoryListComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastManager);
  private readonly loadingService = inject(LoadingService);
  private readonly dialogService = inject(DialogService);
  private readonly logger = inject(LoggerService);

  // ═════════════════════════════════════════════════════════════════════════
  // STATE SIGNALS
  // ═════════════════════════════════════════════════════════════════════════

  readonly loading = signal(false);
  readonly deleting = signal<number | null>(null);
  readonly reordering = signal(false);
  readonly paginatedData = this.categoryService.entities;

  // ═════════════════════════════════════════════════════════════════════════
  // FILTER STATE
  // ═════════════════════════════════════════════════════════════════════════
  readonly searchTerm = signal('');
  readonly filterActive = signal<boolean | undefined>(undefined);
  readonly filterVisible = signal<boolean | undefined>(undefined);
  readonly filterFeatured = signal<boolean | undefined>(undefined);

  // ═════════════════════════════════════════════════════════════════════════
  // PAGINATION STATE
  // ═════════════════════════════════════════════════════════════════════════
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly sortBy = signal<string>('sortOrder');
  readonly sortOrder = signal<'asc' | 'desc'>('asc');

  // ═════════════════════════════════════════════════════════════════════════
  // DEBOUNCE SEARCH
  // ═════════════════════════════════════════════════════════════════════════
  private readonly searchSubject = new Subject<string>();

  // ═════════════════════════════════════════════════════════════════════════
  // COMPUTED VALUES
  // ═════════════════════════════════════════════════════════════════════════
  readonly categories = computed(() => this.paginatedData()?.items ?? []);
  readonly totalCount = computed(() => this.paginatedData()?.totalCount ?? 0);
  readonly totalPages = computed(() => this.paginatedData()?.totalPages ?? 0);
  readonly currentPage = computed(() => (this.paginatedData()?.pageIndex ?? 0) + 1);

  readonly startIndex = computed(() => {
    const data = this.paginatedData();
    if (!data || data.items.length === 0) return 0;
    return data.pageIndex * data.pageSize + 1;
  });

  readonly endIndex = computed(() => {
    const data = this.paginatedData();
    if (!data) return 0;
    return Math.min((data.pageIndex + 1) * data.pageSize, data.totalCount);
  });

  readonly hasActiveFilters = computed(() =>
    !!(
      this.searchTerm() ||
      this.filterActive() !== undefined ||
      this.filterVisible() !== undefined ||
      this.filterFeatured() !== undefined
    )
  );

  readonly visiblePages = computed(() => {
    const current = this.currentPage();
    const total = this.totalPages();
    const pages: (number | string)[] = [];

    if (total === 0) return pages;

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push('...');
      const start = Math.max(2, current - 1);
      const end = Math.min(total - 1, current + 1);
      for (let i = start; i <= end; i++) pages.push(i);
      if (current < total - 2) pages.push('...');
      pages.push(total);
    }

    return pages;
  });

  readonly isEmpty = computed(() => this.categories().length === 0);
  readonly hasResults = computed(() => !this.loading() && !this.isEmpty());
  readonly showEmptyState = computed(() => !this.loading() && this.isEmpty());

  readonly canDrag = computed(() => 
    !this.loading() && 
    !this.reordering() && 
    this.sortBy() === 'sortOrder' &&
    this.sortOrder() === 'asc'
  );

  // ═════════════════════════════════════════════════════════════════════════
  // LIFECYCLE
  // ═════════════════════════════════════════════════════════════════════════
  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadCategories();
    
    this.logger.debug('CategoryListComponent initialized', {
      component: 'CategoryList',
      initialFilter: this.buildFilter(),
      timestamp: Date.now()
    });
  }

  // ═════════════════════════════════════════════════════════════════════════
  // PRIVATE HELPERS
  // ═════════════════════════════════════════════════════════════════════════
  private setupSearchDebounce(): void {
    this.searchSubject
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(term => {
        this.searchTerm.set(term);
        this.resetToFirstPage();
        this.loadCategories();
      });
  }

  private resetToFirstPage(): void {
    this.pageIndex.set(0);
  }

  private buildFilter(): CategoryFilter {
    return {
      searchTerm: this.searchTerm() || undefined,
      isActive: this.filterActive(),
      isVisible: this.filterVisible(),
      isFeatured: this.filterFeatured(),
      pageIndex: this.pageIndex(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortOrder: this.sortOrder()
    };
  }

  // ═════════════════════════════════════════════════════════════════════════
  // LOAD DATA
  // ═════════════════════════════════════════════════════════════════════════
  loadCategories(): void {
    this.loading.set(true);
    const filter = this.buildFilter();

    this.categoryService
      .getAll(filter)
      .pipe(
        catchError((error: StandardError) => {
          this.toast.error(error.userMessage, error.userTitle);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.loading.set(false);
      });
  }

  // ═════════════════════════════════════════════════════════════════════════
  // REORDERING FEATURES - WITH MATERIAL DIALOG
  // ═════════════════════════════════════════════════════════════════════════
  
  /**
   * Drag-drop reordering (within current page)
   * Backend ensures all sortOrder values remain unique
   */
  onDrop(event: CdkDragDrop<Category[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    const currentCategories = [...this.categories()];
    const originalOrder = currentCategories.map(c => c.id);
    moveItemInArray(currentCategories, event.previousIndex, event.currentIndex);
    const newOrder = currentCategories.map(c => c.id);

    this.logger.info('Reordering categories via drag-drop', {
      previousIndex: event.previousIndex,
      currentIndex: event.currentIndex,
      originalOrder,
      newOrder
    });

    this.reordering.set(true);

    this.categoryService.reorder(newOrder)
      .pipe(
        catchError((error: StandardError) => {
          this.toast.error('Failed to reorder. Changes reverted.', 'Reorder Failed');
          this.logger.error('Reorder failed', { error, originalOrder, newOrder });
          this.reordering.set(false);
          this.loadCategories();
          return of(void 0);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.reordering.set(false);
        this.toast.success('Order saved successfully.', 'Reordered');
        this.logger.info('Reorder successful', { newOrder });
        this.loadCategories();
      });
  }

  /**
   * Move category to specific position with swap logic
   * Uses Material Dialog instead of browser prompt
   * If target sortOrder is taken, backend swaps with existing category
   */
  async onMoveToPosition(category: Category): Promise<void> {
    // NEW: Use Material Dialog instead of browser prompt
    const newSortOrder = await this.dialogService.promptMoveToPosition({
      itemName: category.name,
      currentPosition: category.sortOrder,
      totalItems: this.totalCount(),
      min: 1
    });

    if (newSortOrder === null) return; // User cancelled

    if (newSortOrder === category.sortOrder) {
      return; // No change
    }

    this.logger.info('Moving category to position (with swap if needed)', {
      categoryId: category.id,
      categoryName: category.name,
      oldPosition: category.sortOrder,
      newPosition: newSortOrder
    });

    this.reordering.set(true);

    // Use dedicated moveToPosition endpoint (handles swap logic)
    this.categoryService
      .moveToPosition(category.id, newSortOrder)
      .pipe(
        catchError((error: StandardError) => {
          this.toast.error(
            error.userMessage || 'Failed to move category.',
            'Move Failed'
          );
          this.logger.error('Move to position failed', { error, categoryId: category.id, newSortOrder });
          this.reordering.set(false);
          return of(void 0);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.reordering.set(false);
        this.toast.success(
          `"${category.name}" moved to position ${newSortOrder}.`,
          'Position Updated'
        );
        this.logger.info('Move to position successful', {
          categoryId: category.id,
          newPosition: newSortOrder
        });
        this.loadCategories(); // Refresh to show new order
      });
  }

  // ═════════════════════════════════════════════════════════════════════════
  // SEARCH & FILTER HANDLERS
  // ═════════════════════════════════════════════════════════════════════════
  onSearchInput(term: string): void {
    this.searchSubject.next(term);
  }

  onFilterChange(type: 'active' | 'visible' | 'featured', value: string): void {
    const boolValue = value === '' ? undefined : value === 'true';
    
    switch (type) {
      case 'active':
        this.filterActive.set(boolValue);
        break;
      case 'visible':
        this.filterVisible.set(boolValue);
        break;
      case 'featured':
        this.filterFeatured.set(boolValue);
        break;
    }
    
    this.resetToFirstPage();
    this.loadCategories();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.filterActive.set(undefined);
    this.filterVisible.set(undefined);
    this.filterFeatured.set(undefined);
    this.resetToFirstPage();
    this.loadCategories();
  }

  // ═════════════════════════════════════════════════════════════════════════
  // PAGINATION HANDLERS
  // ═════════════════════════════════════════════════════════════════════════
  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.resetToFirstPage();
    this.loadCategories();
  }

  onPageChange(page: number | string): void {
    if (typeof page !== 'number') return;
    if (page < 1 || page > this.totalPages()) return;
    
    this.pageIndex.set(page - 1);
    this.loadCategories();
    this.scrollToTop();
  }

  onPreviousPage(): void {
    const current = this.currentPage();
    if (current > 1) {
      this.onPageChange(current - 1);
    }
  }

  onNextPage(): void {
    const current = this.currentPage();
    if (current < this.totalPages()) {
      this.onPageChange(current + 1);
    }
  }

  onFirstPage(): void {
    if (this.currentPage() !== 1) {
      this.onPageChange(1);
    }
  }

  onLastPage(): void {
    const last = this.totalPages();
    if (this.currentPage() !== last) {
      this.onPageChange(last);
    }
  }

  private scrollToTop(): void {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // ═════════════════════════════════════════════════════════════════════════
  // SORTING HANDLERS
  // ═════════════════════════════════════════════════════════════════════════
  onSort(field: string): void {
    if (this.sortBy() === field) {
      this.sortOrder.set(this.sortOrder() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      this.sortOrder.set('asc');
    }
    this.loadCategories();
  }

  getSortIcon(field: string): string {
    if (this.sortBy() !== field) return '⇅';
    return this.sortOrder() === 'asc' ? '↑' : '↓';
  }

  // ═════════════════════════════════════════════════════════════════════════
  // CRUD OPERATIONS
  // ═════════════════════════════════════════════════════════════════════════
  onCreate(): void {
    this.router.navigate(['/admin/categories/create']);
  }

  onEdit(id: number): void {
    this.router.navigate(['/admin/categories/edit', id]);
  }

  onView(id: number): void {
    this.router.navigate(['/admin/categories', id]);
  }

  async onDelete(category: Category): Promise<void> {
    const confirmed = await this.dialogService.confirmDelete(category.name);
    if (!confirmed) return;

    this.deleting.set(category.id);

    this.logger.info('Deleting category', {
      categoryId: category.id,
      categoryName: category.name,
      operation: 'delete',
      initiatedBy: 'user'
    });

    this.categoryService
      .delete(category.id)
      .pipe(
        catchError((error: StandardError) => {
          this.toast.error(error.userMessage, 'Delete Failed');
          this.deleting.set(null);
          return of(void 0);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.deleting.set(null);
        this.toast.success(`"${category.name}" has been deleted.`, 'Category Deleted');
        
        this.logger.info('Category deleted successfully', {
          categoryId: category.id,
          categoryName: category.name,
          operation: 'delete',
          status: 'success'
        });
        
        const currentData = this.paginatedData();
        if (currentData && currentData.items.length === 1 && this.pageIndex() > 0) {
          this.pageIndex.set(this.pageIndex() - 1);
        }
        
        this.loadCategories();
      });
  }

  isDeleting(categoryId: number): boolean {
    return this.deleting() === categoryId;
  }

  // ═════════════════════════════════════════════════════════════════════════
  // UTILITY
  // ═════════════════════════════════════════════════════════════════════════
  trackByCategory(_index: number, category: Category): number {
    return category.id;
  }

  refreshList(): void {
    this.categoryService.clearCache();
    this.loadCategories();
  }
}
