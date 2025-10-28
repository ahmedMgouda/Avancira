import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';

import { Category } from '../../../../models/category';
import { CategoryService } from '../../../../services/category.service';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './category-list.component.html',
  styleUrls: [
    './category-list.component.scss',
    '../categories.styles.scss',
  ],
})
export class CategoryListComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);

  readonly categories = signal<Category[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly searchTerm = signal('');
  readonly filterActive = signal<boolean | undefined>(undefined);
  readonly filterVisible = signal<boolean | undefined>(undefined);
  readonly filterFeatured = signal<boolean | undefined>(undefined);

  readonly filteredCategories = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const active = this.filterActive();
    const visible = this.filterVisible();
    const featured = this.filterFeatured();

    return this.categories().filter((category) => {
      const matchesTerm =
        !term ||
        category.name.toLowerCase().includes(term) ||
        (category.description ?? '').toLowerCase().includes(term);
      const matchesActive =
        active === undefined || category.isActive === active;
      const matchesVisible =
        visible === undefined || category.isVisible === visible;
      const matchesFeatured =
        featured === undefined || category.isFeatured === featured;

      return matchesTerm && matchesActive && matchesVisible && matchesFeatured;
    });
  });

  ngOnInit(): void {
    this.categoryService.categories$
      .pipe(takeUntilDestroyed())
      .subscribe((categories) => this.categories.set(categories));

    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryService.getAll().subscribe({
      next: () => {
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load categories:', err);
        this.error.set('Failed to load categories. Please try again.');
        this.loading.set(false);
      },
    });
  }

  onSearchChange(term: string): void {
    this.searchTerm.set(term);
  }

  onFilterChange(
    filterType: 'active' | 'visible' | 'featured',
    value: string
  ): void {
    const boolValue = value === '' ? undefined : value === 'true';

    switch (filterType) {
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
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.filterActive.set(undefined);
    this.filterVisible.set(undefined);
    this.filterFeatured.set(undefined);
  }

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
    if (!confirm(`Are you sure you want to delete "${category.name}"?`)) {
      return;
    }

    this.categoryService.delete(category.id).subscribe({
      next: () => {
        this.loadCategories();
      },
      error: (err) => {
        console.error('Failed to delete category:', err);
        this.error.set('Failed to delete category. Please try again.');
      },
    });
  }

  trackByCategory(index: number, category: Category): number {
    return category.id;
  }
}
