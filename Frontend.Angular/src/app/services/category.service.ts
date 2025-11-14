import { HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { BaseHttpService } from '../core/http/services/base-http.service';

import { environment } from '../environments/environment';
import {
  Category,
  CategoryCreateDto,
  CategoryFilter,
  CategoryUpdateDto} from '../models/category';

@Injectable({ providedIn: 'root' })
export class CategoryService extends BaseHttpService<
  Category,
  CategoryCreateDto,
  CategoryUpdateDto,
  CategoryFilter
> {
  protected readonly apiUrl = `${environment.bffBaseUrl}/api/subject-categories`;
  protected readonly entityName = 'Category';

  constructor() {
    super({
      ttl: 5 * 60 * 1000, // 5 minutes cache TTL
      maxSize: 100
    });
  }

  /**
   * Override filter parameters
   * Adds custom category-specific filters
   */
  protected override buildHttpParams(filter: CategoryFilter): HttpParams {
    let params = super.buildHttpParams(filter);

    if (filter.isActive !== undefined) {
      params = params.set('isActive', filter.isActive.toString());
    }
    if (filter.isVisible !== undefined) {
      params = params.set('isVisible', filter.isVisible.toString());
    }
    if (filter.isFeatured !== undefined) {
      params = params.set('isFeatured', filter.isFeatured.toString());
    }

    return params;
  }

  /**
   * Reorder categories by sending new order to backend
   * Backend will recalculate sortOrder values (10, 20, 30...)
   * 
   * @param categoryIds - Array of category IDs in new order
   * @returns Observable<void>
   */
  reorder(categoryIds: number[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/reorder`, { categoryIds });
  }
}
