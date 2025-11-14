import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { CategoryCreateDto, CategoryUpdateDto } from '@models/category';

import { LoadingService } from '@/core/loading/services/loading.service';
import { StandardError } from '@core/logging/models/standard-error.model';
import { LoggerService } from '@core/logging/services/logger.service';
import { NetworkService } from '@core/network/services/network.service';
import { ToastManager } from '@core/toast/services/toast-manager.service';
import { CategoryService } from '@services/category.service';
import { CategoryValidatorService } from '@services/category-validator.service';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * CATEGORY FORM - ENHANCED WITH CROSS-CUTTING SERVICES
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * IMPROVEMENTS:
 * ✅ Custom validators (name format, async uniqueness)
 * ✅ Network awareness (disable when offline)
 * ✅ Better error handling with StandardError
 * ✅ Toast notifications
 * ✅ Loading states
 * ✅ Logging integration
 */
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
  private readonly validatorService = inject(CategoryValidatorService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastManager);
  private readonly logger = inject(LoggerService);
  private readonly network = inject(NetworkService);
  private readonly loadingService = inject(LoadingService);

  // ═══════════════════════════════════════════════════════════════════
  // STATE SIGNALS
  // ═══════════════════════════════════════════════════════════════════
  categoryForm!: FormGroup;
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly isEditMode = signal(false);
  readonly categoryId = signal<number | null>(null);
  readonly isOnline = this.network.isHealthy;

  // ═══════════════════════════════════════════════════════════════════
  // LIFECYCLE
  // ═══════════════════════════════════════════════════════════════════
  ngOnInit(): void {
    this.initForm();
    this.checkEditMode();
    this.logFormInitialization();
  }

  // ═══════════════════════════════════════════════════════════════════
  // FORM INITIALIZATION WITH ENHANCED VALIDATORS
  // ═══════════════════════════════════════════════════════════════════
  private initForm(): void {
    this.categoryForm = this.fb.group({
      name: [
        '',
        [
          Validators.required,
          Validators.maxLength(200),
          this.validatorService.validName() // Custom validator
        ],
        [this.validatorService.uniqueName(this.categoryId())] // Async validator
      ],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      isVisible: [true],
      isFeatured: [false],
      sortOrder: [
        0,
        [
          Validators.required,
          Validators.min(0),
          this.validatorService.positiveSortOrder() // Custom validator
        ]
      ],
    });
  }

  // ═══════════════════════════════════════════════════════════════════
  // EDIT MODE CHECK
  // ═══════════════════════════════════════════════════════════════════
  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id && !isNaN(+id)) {
      this.isEditMode.set(true);
      this.categoryId.set(+id);
      this.loadCategory(+id);
    }
  }

  // ═══════════════════════════════════════════════════════════════════
  // LOAD CATEGORY FOR EDITING - WITH ERROR HANDLING
  // ═══════════════════════════════════════════════════════════════════
  private loadCategory(id: number): void {
    this.loading.set(true);

    this.categoryService
      .getById(id)
      .pipe(
        catchError((error: StandardError) => {
          this.toast.error(error.userMessage, 'Failed to Load Category');
          this.logger.error('Failed to load category', { categoryId: id, error });
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
          
          this.logger.debug('Category loaded for editing', { categoryId: id, categoryName: category.name });
        }
        this.loading.set(false);
      });
  }

  // ═══════════════════════════════════════════════════════════════════
  // FORM SUBMISSION - WITH NETWORK CHECK
  // ═══════════════════════════════════════════════════════════════════
  onSubmit(): void {
    // Check network first
    if (!this.network.isHealthy()) {
      this.toast.warning('No internet connection. Please check your network and try again.', 'Offline');
      return;
    }

    // Validate form
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      this.toast.warning('Please fix the validation errors before submitting.', 'Validation Error');
      return;
    }

    this.submitting.set(true);
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
        catchError((error: StandardError) => {
          this.toast.error(error.userMessage, 'Failed to Create');
          this.logger.error('Failed to create category', { formValue, error });
          this.submitting.set(false);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(result => {
        if (result) {
          this.toast.success(`"${formValue.name}" has been created.`, 'Category Created');
          this.logger.info('Category created', { categoryId: result.id, categoryName: result.name });
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
        catchError((error: StandardError) => {
          this.toast.error(error.userMessage, 'Failed to Update');
          this.logger.error('Failed to update category', { dto, error });
          this.submitting.set(false);
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(result => {
        if (result) {
          this.toast.success(`"${formValue.name}" has been updated.`, 'Category Updated');
          this.logger.info('Category updated', { categoryId: dto.id, categoryName: formValue.name });
          this.navigateToList();
        }
      });
  }

  // ═══════════════════════════════════════════════════════════════════
  // NAVIGATION
  // ═══════════════════════════════════════════════════════════════════
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

  // ═══════════════════════════════════════════════════════════════════
  // VALIDATION HELPERS - ENHANCED ERROR MESSAGES
  // ═══════════════════════════════════════════════════════════════════
  getFieldError(fieldName: string): string {
    const control = this.categoryForm.get(fieldName);

    if (!control || !control.touched || !control.errors) {
      return '';
    }

    const errors = control.errors;

    // Standard validators
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

    // Custom validators
    if (errors['invalidName']) {
      return errors['invalidName'].message;
    }

    if (errors['negativeSortOrder']) {
      return errors['negativeSortOrder'].message;
    }

    if (errors['nameExists']) {
      return 'A category with this name already exists.';
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

  // ═══════════════════════════════════════════════════════════════════
  // UTILITY
  // ═══════════════════════════════════════════════════════════════════
  getFormTitle(): string {
    return this.isEditMode() ? 'Edit Category' : 'Create Category';
  }

  getSubmitButtonText(): string {
    if (this.submitting()) {
      return this.isEditMode() ? 'Updating...' : 'Creating...';
    }
    return this.isEditMode() ? 'Update Category' : 'Create Category';
  }

  canSubmit(): boolean {
    return this.categoryForm.valid && !this.submitting() && this.network.isHealthy();
  }

  private logFormInitialization(): void {
    this.logger.debug('CategoryFormComponent initialized', {
      mode: this.isEditMode() ? 'edit' : 'create',
      categoryId: this.categoryId(),
      isOnline: this.network.isHealthy()
    });
  }
}
