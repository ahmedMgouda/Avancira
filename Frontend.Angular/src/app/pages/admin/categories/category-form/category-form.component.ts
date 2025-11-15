import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of } from 'rxjs';
import { StandardError } from '@core/logging/models/standard-error.model';
import { CategoryCreateDto, CategoryUpdateDto } from '@models/category';

import { LoadingService } from '@/core/loading/services/loading.service';
import { LoggerService } from '@core/logging/services/logger.service';
import { NetworkService } from '@core/network/services/network.service';
import { ToastManager } from '@core/toast/services/toast-manager.service';
import { DialogService } from '@core/dialogs/services/dialog.service';
import { CategoryService } from '@services/category.service';
import { CategoryValidatorService } from '@services/category-validator.service';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * CATEGORY FORM - AUTO-SORTORDER (NO MANUAL ENTRY)
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * - Removed sortOrder field (auto-assigned by backend)
 * - Added insertPosition selector (create only)
 * - Added customPosition input (when insertPosition is 'custom')
 * - Users reorder after creation using drag-drop/move buttons
 * - Uses Material Dialog for unsaved changes (not browser confirm)
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
  private readonly dialog = inject(DialogService); // NEW: Material dialog

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
  // FORM INITIALIZATION - NO SORTORDER FIELD
  // ═══════════════════════════════════════════════════════════════════
  private initForm(): void {
    this.categoryForm = this.fb.group({
      name: [
        '',
        [
          Validators.required,
          Validators.maxLength(200),
          this.validatorService.validName()
        ],
        [this.validatorService.uniqueName(this.categoryId())]
      ],
      description: ['', [Validators.maxLength(500)]],
      isActive: [true],
      isVisible: [true],
      isFeatured: [false],

      // NEW: Position controls (only for create mode)
      insertPosition: ['end'], // 'start' | 'end' | 'custom'
      customPosition: [null, [Validators.min(1)]]

      // REMOVED: sortOrder field - auto-assigned by backend!
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
  // LOAD CATEGORY FOR EDITING
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
            // Note: sortOrder is NOT loaded into form (read-only)
          });

          this.logger.debug('Category loaded for editing', {
            categoryId: id,
            categoryName: category.name,
            currentSortOrder: category.sortOrder // Log but don't edit
          });
        }
        this.loading.set(false);
      });
  }

  // ═══════════════════════════════════════════════════════════════════
  // FORM SUBMISSION
  // ═══════════════════════════════════════════════════════════════════
  onSubmit(): void {
    if (!this.network.isHealthy()) {
      this.toast.warning('No internet connection. Please check your network and try again.', 'Offline');
      return;
    }

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

  private createCategory(formValue: any): void {
    const dto: CategoryCreateDto = {
      name: formValue.name,
      description: formValue.description || undefined,
      isActive: formValue.isActive,
      isVisible: formValue.isVisible,
      isFeatured: formValue.isFeatured,

      // NEW: Include position preference
      insertPosition: formValue.insertPosition || 'end',
      customPosition: formValue.insertPosition === 'custom'
        ? formValue.customPosition
        : undefined

      // REMOVED: sortOrder - auto-assigned by backend!
    };

    this.categoryService
      .create(dto)
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
          this.toast.success(
            `"${formValue.name}" has been created at position ${result.sortOrder}.`,
            'Category Created'
          );
          this.logger.info('Category created', {
            categoryId: result.id,
            categoryName: result.name,
            assignedPosition: result.sortOrder
          });
          this.navigateToList();
        }
      });
  }

  private updateCategory(formValue: any): void {
    const dto: CategoryUpdateDto = {
      id: this.categoryId()!,
      name: formValue.name,
      description: formValue.description || undefined,
      isActive: formValue.isActive,
      isVisible: formValue.isVisible,
      isFeatured: formValue.isFeatured

      // REMOVED: sortOrder - changed via reorder/move endpoints only!
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
  // NAVIGATION - WITH MATERIAL DIALOG
  // ═══════════════════════════════════════════════════════════════════
  async onCancel(): Promise<void> {
    if (this.categoryForm.dirty) {
      // NEW: Use Material Dialog instead of browser confirm
      const confirmed = await this.dialog.confirmUnsaved();
      if (!confirmed) return;
    }
    this.navigateToList();
  }

  private navigateToList(): void {
    this.router.navigate(['/admin/categories']);
  }

  // ═══════════════════════════════════════════════════════════════════
  // VALIDATION HELPERS
  // ═══════════════════════════════════════════════════════════════════
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

    if (errors['invalidName']) {
      return errors['invalidName'].message;
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
      isActive: 'Active',
      isVisible: 'Visible',
      isFeatured: 'Featured',
      customPosition: 'Position'
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
