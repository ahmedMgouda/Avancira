import { inject, Injectable } from '@angular/core';
import { AbstractControl, AsyncValidatorFn, ValidationErrors, ValidatorFn } from '@angular/forms';
import { catchError, debounceTime, map, Observable, of, switchMap } from 'rxjs';

import { CategoryService } from './category.service';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * CATEGORY VALIDATOR SERVICE - SIMPLE & REUSABLE
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * Provides reusable validators for category forms:
 * - Async: Check name uniqueness
 * - Sync: Custom validation rules
 */
@Injectable({ providedIn: 'root' })
export class CategoryValidatorService {
  private readonly categoryService = inject(CategoryService);

  // ═══════════════════════════════════════════════════════════════════
  // ASYNC VALIDATORS
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Check if category name already exists
   * Usage: this.validatorService.uniqueName(this.categoryId())
   */
  uniqueName(excludeId?: number | null): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value || control.value.trim().length === 0) {
        return of(null);
      }

      return of(control.value).pipe(
        debounceTime(500), // Wait for user to stop typing
        switchMap(name => this.checkNameExists(name, excludeId)),
        map(exists => (exists ? { nameExists: true } : null)),
        catchError(() => of(null)) // On error, don't block form
      );
    };
  }

  /**
   * Check if name exists via API
   * Note: You'll need to add this endpoint to your backend
   */
  private checkNameExists(name: string, excludeId?: number | null): Observable<boolean> {
    // For now, return false (name is unique)
    // TODO: Implement backend endpoint: GET /api/subject-categories/check-name?name=X&excludeId=Y
    return of(false);
    
    // When backend is ready, uncomment:
    // const params = new HttpParams()
    //   .set('name', name)
    //   .set('excludeId', excludeId?.toString() ?? '');
    // return this.http.get<{exists: boolean}>(`${apiUrl}/check-name`, { params })
    //   .pipe(map(response => response.exists));
  }

  // ═══════════════════════════════════════════════════════════════════
  // SYNC VALIDATORS
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Validate that name contains only letters, numbers, spaces, and hyphens
   * Usage: this.validatorService.validName()
   */
  validName(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const value = control.value.trim();
      const pattern = /^[a-zA-Z0-9\s-]+$/;

      if (!pattern.test(value)) {
        return { invalidName: { message: 'Name can only contain letters, numbers, spaces, and hyphens' } };
      }

      return null;
    };
  }

  /**
   * Validate sort order is positive
   * Usage: this.validatorService.positiveSortOrder()
   */
  positiveSortOrder(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      
      if (value === null || value === undefined || value === '') {
        return null;
      }

      if (value < 0) {
        return { negativeSortOrder: { message: 'Sort order must be positive' } };
      }

      return null;
    };
  }
}
