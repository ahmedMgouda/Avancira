import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Category } from '../../../../models/category';
import { CategoryService } from '../../../../services/category.service';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-detail.component.html',
  styleUrls: [
    './category-detail.component.scss',
    '../categories.styles.scss',
  ],
})
export class CategoryDetailComponent implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly category = signal<Category | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCategory(+id);
    }
  }

  onEdit(): void {
    const category = this.category();
    if (category) {
      this.router.navigate(['/admin/categories/edit', category.id]);
    }
  }

  onDelete(): void {
    const category = this.category();
    if (!category) {
      return;
    }

    if (!confirm(`Are you sure you want to delete "${category.name}"?`)) {
      return;
    }

    this.categoryService.delete(category.id).subscribe({
      next: () => {
        this.router.navigate(['/admin/categories']);
      },
      error: (err) => {
        console.error('Failed to delete category:', err);
        this.error.set('Failed to delete category. Please try again.');
      },
    });
  }

  onBack(): void {
    this.router.navigate(['/admin/categories']);
  }

  private loadCategory(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryService.getById(id).subscribe({
      next: (category) => {
        this.category.set(category);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load category:', err);
        this.error.set('Failed to load category. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
