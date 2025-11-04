import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { CategoryCreateDto, CategoryUpdateDto } from '@models/category';

import { CategoryService } from '@services/category.service';

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
  private readonly destroyRef = inject(DestroyRef);

  // ────────────────────────────────────────────────────────────────
  // STATE SIGNALS
  // ────────────────────────────────────────────────────────────────
  categoryForm!: FormGroup;
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly isEditMode = signal(false);
  readonly categoryId = signal<number | null>(null);

  // ────────────────────────────────────────────────────────────────
  // LIFECYCLE
  // ────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
  }

  // ────────────────────────────────────────────────────────────────
  // FORM INITIALIZATION
  // ────────────────────────────────────────────────────────────────
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

  // ────────────────────────────────────────────────────────────────
  // EDIT MODE CHECK
  // ────────────────────────────────────────────────────────────────
  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id && !isNaN(+id)) {
      this.isEditMode.set(true);
      this.categoryId.set(+id);
      this.loadCategory(+id);
    }
  }

  // ────────────────────────────────────────────────────────────────
  // LOAD CATEGORY FOR EDITING
  // ────────────────────────────────────────────────────────────────
  private loadCategory(id: number): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryService
      .getById(id)
      .pipe(
        catchError((err: unknown) => {
          this.error.set(this.extractErrorMessage(err));
          this.loading.set(false);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(category => {
        if (category) {
          this.categoryForm.patchValue({
            name: category.name,
            description: category.description ?? '',
            isActive: category.isActive,
            isVisible: category.isVisible,
            isFeatured: category.isFeatured,
            sortOrder: category.sortOrder,
          });
        }
        this.loading.set(false);
      });
  }

  // ────────────────────────────────────────────────────────────────
  // FORM SUBMISSION
  // ────────────────────────────────────────────────────────────────
  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      this.error.set('Please fix the validation errors before submitting.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const formValue = this.categoryForm.getRawValue();

    if (this.isEditMode()) {
      this.updateCategory(formValue);
    } else {
      this.createCategory(formValue);
    }
  }

  private createCategory(formValue: CategoryCreateDto): void {
    this.categoryService
      .create(formValue)
      .pipe(
        catchError((err: unknown) => {
          this.error.set(this.extractErrorMessage(err));
          this.submitting.set(false);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(result => {
        if (result) {
          this.navigateToList();
        }
      });
  }

  private updateCategory(formValue: CategoryCreateDto): void {
    const dto: CategoryUpdateDto = {
      id: this.categoryId()!,
      ...formValue,
    };

    this.categoryService
      .update(dto)
      .pipe(
        catchError((err: unknown) => {
          this.error.set(this.extractErrorMessage(err));
          this.submitting.set(false);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(result => {
        if (result) {
          this.navigateToList();
        }
      });
  }

  // ────────────────────────────────────────────────────────────────
  // NAVIGATION
  // ────────────────────────────────────────────────────────────────
  onCancel(): void {
    if (this.categoryForm.dirty) {
      const confirmMessage = 'You have unsaved changes. Are you sure you want to leave?';
      if (!confirm(confirmMessage)) return;
    }
    this.navigateToList();
  }

  private navigateToList(): void {
    this.router.navigate(['/admin/categories']);
  }

  // ────────────────────────────────────────────────────────────────
  // VALIDATION HELPERS
  // ────────────────────────────────────────────────────────────────
  getFieldError(fieldName: string): string {
    const control = this.categoryForm.get(fieldName);

    if (!control || !control.touched || !control.errors) {
      return '';
    }

    const errors = control.errors;

    if (errors['required']) {
      return `${this.getFieldLabel(fieldName)} is required.`;
    }

    if (errors['maxlength']) {
      const max = errors['maxlength'].requiredLength;
      return `${this.getFieldLabel(fieldName)} cannot exceed ${max} characters.`;
    }

    if (errors['min']) {
      return `${this.getFieldLabel(fieldName)} must be at least ${errors['min'].min}.`;
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
      isActive: 'Active',
      isVisible: 'Visible',
      isFeatured: 'Featured',
    };

    return labels[fieldName] || fieldName;
  }

  // ────────────────────────────────────────────────────────────────
  // UTILITY
  // ────────────────────────────────────────────────────────────────
  private extractErrorMessage(err: unknown): string {
    if (err instanceof Error) return err.message;
    if (typeof err === 'string') return err;
    if (err && typeof err === 'object' && 'message' in err) {
      return String(err.message);
    }
    return 'An unexpected error occurred';
  }

  getFormTitle(): string {
    return this.isEditMode() ? 'Edit Category' : 'Create Category';
  }

  getSubmitButtonText(): string {
    if (this.submitting()) {
      return this.isEditMode() ? 'Updating...' : 'Creating...';
    }
    return this.isEditMode() ? 'Update Category' : 'Create Category';
  }
}