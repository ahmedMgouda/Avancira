import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { CategoryCreateDto, CategoryUpdateDto } from '../../../../models/category';
import { CategoryService } from '../../../../services/category.service';

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './category-form.component.html',
  styleUrls: [
    './category-form.component.scss',
    '../categories.styles.scss',
  ],
})
export class CategoryFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  categoryForm!: FormGroup;
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly isEditMode = signal(false);
  readonly categoryId = signal<number | null>(null);

  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  private initForm(): void {
    this.categoryForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      isVisible: [true],
      isFeatured: [false],
      sortOrder: [0, [Validators.required, Validators.min(0)]],
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.isEditMode.set(true);
      this.categoryId.set(+id);
      this.loadCategory(+id);
    }
  }

  private loadCategory(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryService.getById(id).subscribe({
      next: (category) => {
        this.categoryForm.patchValue({
          name: category.name,
          description: category.description ?? '',
          isActive: category.isActive,
          isVisible: category.isVisible,
          isFeatured: category.isFeatured,
          sortOrder: category.sortOrder,
        });
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load category:', err);
        this.error.set('Failed to load category. Please try again.');
        this.loading.set(false);
      },
    });
  }

  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const formValue = this.categoryForm.getRawValue() as CategoryCreateDto;

    if (this.isEditMode()) {
      const dto: CategoryUpdateDto = {
        id: this.categoryId()!,
        ...formValue,
      };

      this.categoryService.update(dto).subscribe({
        next: () => {
          this.router.navigate(['/admin/categories']);
        },
        error: (err) => {
          console.error('Failed to update category:', err);
          this.error.set('Failed to update category. Please try again.');
          this.submitting.set(false);
        },
      });
    } else {
      const dto: CategoryCreateDto = formValue;

      this.categoryService.create(dto).subscribe({
        next: () => {
          this.router.navigate(['/admin/categories']);
        },
        error: (err) => {
          console.error('Failed to create category:', err);
          this.error.set('Failed to create category. Please try again.');
          this.submitting.set(false);
        },
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/admin/categories']);
  }

  getFieldError(fieldName: string): string {
    const control = this.categoryForm.get(fieldName);

    if (!control || !control.touched || !control.errors) {
      return '';
    }

    if (control.errors['required']) {
      return `${this.getFieldLabel(fieldName)} is required.`;
    }

    if (control.errors['maxlength']) {
      return `${this.getFieldLabel(fieldName)} is too long.`;
    }

    if (control.errors['min']) {
      return `${this.getFieldLabel(fieldName)} must be at least ${control.errors['min'].min}.`;
    }

    return 'Invalid value.';
  }

  isFieldInvalid(fieldName: string): boolean {
    const control = this.categoryForm.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }

  private getFieldLabel(fieldName: string): string {
    const labels: Record<string, string> = {
      name: 'Name',
      description: 'Description',
      sortOrder: 'Sort Order',
    };

    return labels[fieldName] || fieldName;
  }
}
