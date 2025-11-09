import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, of,Subject } from 'rxjs';
import { Category, CategoryFilter } from '@models/category';

// import { FileUploadComponent } from '@core/file-upload/components/file-upload.component';
// import { FileUploadService } from '@core/file-upload/services/file-upload.service';
import {ToastService} from '@core/toast/services/toast.service';
import { CategoryService } from '@services/category.service';

import { LoadingDirective } from '@/core/loading/directives/loading.directive';

import { FileMetadata, FileType } from '@/core/file-upload/models/file-upload.models';

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

  // private readonly fileUploadService = inject(FileUploadService);

  // ────────────────────────────────────────────────────────────────
  // STATE SIGNALS
  // ────────────────────────────────────────────────────────────────
  

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly deleting = signal<number | null>(null);

  readonly isRefresh = signal(false);


  // Use service's entities signal for consistent state
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
  readonly pageIndex = signal(0); // 0-based for backend
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
  readonly currentPage = computed(() => (this.paginatedData()?.pageIndex ?? 0) + 1); // 1-based for UI

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
  }



  fileType = FileType.Image;
  files: FileMetadata[] = [];
  isValid = true;

  onFilesChanged(files: FileMetadata[]): void {
    this.files = files;
  }

  onFilesValidated(valid: boolean): void {
    this.isValid = valid;
  }

  // async uploadImages(): Promise<void> {
  //   try {
  //     const urls = await this.fileUploadService.uploadFiles(
  //       this.files,
  //       '/api/products/upload-images'
  //     );
      
     
      
  //     console.log('Uploaded URLs:', urls);
  //     // Save URLs to your form or backend
  //   } catch{
  //   }
  // }



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

  private extractErrorMessage(err: unknown): string {
    if (err instanceof Error) return err.message;
    if (typeof err === 'string') return err;
    if (err && typeof err === 'object' && 'message' in err) {
      return String(err.message);
    }
    return 'An unexpected error occurred';
  }

  // ────────────────────────────────────────────────────────────────
  // LOAD DATA
  // ────────────────────────────────────────────────────────────────
  loadCategories(): void {
    this.loading.set(true);
    this.error.set(null);

    const filter = this.buildFilter();

    this.categoryService
      .getAll(filter)
      .pipe(
        catchError((err: unknown) => {
          this.error.set(this.extractErrorMessage(err));
          this.loading.set(false);
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
  }

  // ────────────────────────────────────────────────────────────────
  // PAGINATION HANDLERS
  // ────────────────────────────────────────────────────────────────
  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.resetToFirstPage();
    this.loadCategories();
  }

  onPageChange(page: number | string): void {
    if (typeof page !== 'number') return;
    if (page < 1 || page > this.totalPages()) return;
    
    this.pageIndex.set(page - 1); // Convert to 0-based
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
      // Toggle sort order
      this.sortOrder.set(this.sortOrder() === 'asc' ? 'desc' : 'asc');
    } else {
      // New field, default to ascending
      this.sortBy.set(field);
      this.sortOrder.set('asc');
    }
    this.loadCategories();
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

  onDelete(category: Category): void {
    const confirmMessage = `Are you sure you want to delete "${category.name}"?\n\nThis action cannot be undone.`;
    
    if (!confirm(confirmMessage)) return;

    this.deleting.set(category.id);
    this.error.set(null);

    this.categoryService
      .delete(category.id)
      .pipe(
        catchError((err: unknown) => {
          this.error.set(this.extractErrorMessage(err));
          this.deleting.set(null);
          return of(void 0);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.deleting.set(null);
        
        // If we deleted the last item on the page and not on first page, go back one page
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
  }

testLoading(): void {
  this.isRefresh.set(true);
  throw Error("test toast");
  // this.toast.success('Loading Test', 'This is a test of the loading spinner.');
  // console.log('Test spinner started');
  setTimeout(() => {
    this.isRefresh.set(false);
    // this.toast.error('Loading Test Ended', 'The loading spinner test has completed.');
    // console.log('Test spinner ended');
  }, 3000); // ✅ Will complete properly now
}

}