import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of, Subject } from 'rxjs';

import { Category, CategoryFilter } from '@models/category';
import { LoadingService } from '@/core/loading/services/loading.service';
import { ToastService } from '@core/toast/services/toast.service';
import { DialogService } from '@core/dialogs';
import { ErrorHandlerService } from '@core/logging/services/error-handler.service';
import { LoggerService } from '@core/logging/services/logger.service';
import { CategoryService } from '@services/category.service';
import { LoadingDirective } from '@/core/loading/directives/loading.directive';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingDirective],
  templateUrl: './category-list.component.html',
  styleUrls: ['./category-list.component.scss', '../categories.styles.scss']
})
export class CategoryListComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastService);
  private readonly loadingService = inject(LoadingService);
  private readonly dialogService = inject(DialogService);
  private readonly errorHandler = inject(ErrorHandlerService);
  private readonly logger = inject(LoggerService);

  // ────────────────────────────────────────────────────────────────
  // STATE SIGNALS
  // ────────────────────────────────────────────────────────────────

  readonly loading = signal(false);
  readonly deleting = signal<number | null>(null);
  readonly paginatedData = this.categoryService.entities;

  // ────────────────────────────────────────────────────────────────
  // FILTER STATE
  // ────────────────────────────────────────────────────────────────
  readonly searchTerm = signal('');
  readonly filterActive = signal<boolean | undefined>(undefined);
  readonly filterVisible = signal<boolean | undefined>(undefined);
  readonly filterFeatured = signal<boolean | undefined>(undefined);

  // ────────────────────────────────────────────────────────────────
  // PAGINATION STATE
  // ────────────────────────────────────────────────────────────────
  readonly pageIndex = signal(0);
  readonly pageSize = signal(25);
  readonly sortBy = signal<string>('sortOrder');
  readonly sortOrder = signal<'asc' | 'desc'>('asc');

  // ────────────────────────────────────────────────────────────────
  // DEBOUNCE SEARCH
  // ────────────────────────────────────────────────────────────────
  private readonly searchSubject = new Subject<string>();

  // ────────────────────────────────────────────────────────────────
  // COMPUTED VALUES
  // ────────────────────────────────────────────────────────────────
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

  // ────────────────────────────────────────────────────────────────
  // LIFECYCLE
  // ────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadCategories();
    this.logger.debug('CategoryListComponent initialized');
  }

  // ────────────────────────────────────────────────────────────────
  // PRIVATE HELPERS
  // ────────────────────────────────────────────────────────────────
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

  // ────────────────────────────────────────────────────────────────
  // LOAD DATA
  // ────────────────────────────────────────────────────────────────
  loadCategories(): void {
    this.loading.set(true);
    const filter = this.buildFilter();

    this.categoryService
      .getAll(filter)
      .pipe(
        catchError((error: unknown) => {
          // Use ErrorHandlerService for proper error handling
          const standardError = this.errorHandler.handle(error);
          this.toast.error(standardError.userMessage, standardError.userTitle);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.loading.set(false);
      });
  }

  // ────────────────────────────────────────────────────────────────
  // SEARCH & FILTER HANDLERS
  // ────────────────────────────────────────────────────────────────
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
    this.logger.debug('Filters cleared');
  }

  // ────────────────────────────────────────────────────────────────
  // PAGINATION HANDLERS
  // ────────────────────────────────────────────────────────────────
  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.resetToFirstPage();
    this.loadCategories();
    this.logger.debug('Page size changed', { size });
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

  // ────────────────────────────────────────────────────────────────
  // SORTING HANDLERS
  // ────────────────────────────────────────────────────────────────
  onSort(field: string): void {
    if (this.sortBy() === field) {
      this.sortOrder.set(this.sortOrder() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(field);
      this.sortOrder.set('asc');
    }
    this.loadCategories();
    this.logger.debug('Sort changed', { field, order: this.sortOrder() });
  }

  getSortIcon(field: string): string {
    if (this.sortBy() !== field) return '⇅';
    return this.sortOrder() === 'asc' ? '↑' : '↓';
  }

  // ────────────────────────────────────────────────────────────────
  // CRUD OPERATIONS
  // ────────────────────────────────────────────────────────────────
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
    // Use DialogService for confirmation
    const confirmed = await this.dialogService.confirmDelete(category.name);
    
    if (!confirmed) {
      this.logger.debug('Delete cancelled by user', { categoryId: category.id });
      return;
    }

    this.deleting.set(category.id);
    this.logger.info('Deleting category', { categoryId: category.id, name: category.name });

    this.categoryService
      .delete(category.id)
      .pipe(
        catchError((error: unknown) => {
          // Use ErrorHandlerService for proper error handling
          const standardError = this.errorHandler.handle(error);
          this.toast.error(standardError.userMessage, 'Delete Failed');
          this.deleting.set(null);
          return of(void 0);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.deleting.set(null);
        this.toast.success(`"${category.name}" has been deleted.`, 'Category Deleted');
        this.logger.info('Category deleted successfully', { categoryId: category.id });
        
        // Navigate to previous page if we deleted the last item
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

  // ────────────────────────────────────────────────────────────────
  // UTILITY
  // ────────────────────────────────────────────────────────────────
  trackByCategory(_index: number, category: Category): number {
    return category.id;
  }

  refreshList(): void {
    this.categoryService.clearCache();
    this.loadCategories();
    this.toast.info('Category list refreshed');
    this.logger.debug('Cache cleared and list refreshed');
  }

  // ────────────────────────────────────────────────────────────────
  // TESTING / DEBUG
  // ────────────────────────────────────────────────────────────────
  testLoading(): void {
    this.loadingService.showGlobal();
    this.loadingService.updateGlobalMessage('Testing loading overlay...');
    this.logger.debug('Test loading started');
    
    setTimeout(() => {
      this.loadingService.hideGlobal();
      this.toast.success('Loading test completed');
      this.logger.debug('Test loading completed');
    }, 3000);
  }
}
