import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, shareReplay, tap } from 'rxjs/operators';

import { ErrorHandlerService } from '../../logging/services/error-handler.service';
import { StandardError } from '../../logging/models/standard-error.model';
import { BaseFilter, PaginatedResult } from '../../models/base.model';
import { CacheConfig, EntityCache } from '../../utils/entity-cache.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * BASE HTTP SERVICE - IMPROVED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * IMPROVEMENTS:
 * ✅ Better StandardError type checking with symbol
 * ✅ Prevents false positives from duck-typing
 * ✅ Type-safe error detection
 */

// Type discriminator symbol for StandardError
const STANDARD_ERROR_BRAND = Symbol('StandardError');

export abstract class BaseHttpService<
  T extends { id: number },
  TCreate = Omit<T, 'id' | 'createdAt' | 'updatedAt'>,
  TUpdate = TCreate & { id: number },
  TFilter extends BaseFilter = BaseFilter
> {
  protected readonly http = inject(HttpClient);
  protected readonly errorHandler = inject(ErrorHandlerService);

  protected abstract readonly apiUrl: string;
  protected abstract readonly entityName: string;

  protected readonly cache: EntityCache<T>;
  protected readonly entitiesSignal = signal<PaginatedResult<T> | null>(null);
  readonly entities = this.entitiesSignal.asReadonly();

  constructor(cacheConfig: CacheConfig = {}) {
    this.cache = new EntityCache<T>(cacheConfig);
  }

  // ═══════════════════════════════════════════════════════════════════
  // CRUD METHODS
  // ═══════════════════════════════════════════════════════════════════

  getAll(filter: TFilter = {} as TFilter): Observable<PaginatedResult<T>> {
    const params = this.buildHttpParams(filter);
    return this.http.get<PaginatedResult<T>>(this.apiUrl, { params }).pipe(
      tap(result => {
        this.entitiesSignal.set(result);
        this.cache.setList(result.items);
      }),
      catchError(error => this.handleError(error, `fetch ${this.entityName} list`))
    );
  }

  getById(id: number, forceRefresh = false): Observable<T> {
    if (!forceRefresh) {
      const cached = this.cache.get(id);
      if (cached) {
        return new Observable(observer => {
          observer.next(cached);
          observer.complete();
        });
      }
    }

    return this.http.get<T>(`${this.apiUrl}/${id}`).pipe(
      tap(entity => this.cache.set(entity.id, entity)),
      shareReplay(1),
      catchError(error => this.handleError(error, `fetch ${this.entityName} #${id}`))
    );
  }

  create(dto: TCreate): Observable<T> {
    return this.http.post<T>(this.apiUrl, dto).pipe(
      tap(entity => {
        this.cache.set(entity.id, entity);
        this.cache.invalidateList();
      }),
      catchError(error => this.handleError(error, `create ${this.entityName}`))
    );
  }

  update(dto: TUpdate): Observable<T> {
    const id = (dto as any).id;
    return this.http.put<T>(`${this.apiUrl}/${id}`, dto).pipe(
      tap(entity => {
        this.cache.set(entity.id, entity);
        this.cache.invalidateList();
      }),
      catchError(error => this.handleError(error, `update ${this.entityName} #${id}`))
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this.cache.invalidate(id);
        this.cache.invalidateList();
      }),
      catchError(error => this.handleError(error, `delete ${this.entityName} #${id}`))
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // ERROR HANDLING - IMPROVED
  // ═══════════════════════════════════════════════════════════════════

  private handleError(error: unknown, operation: string): Observable<never> {
    // Check with improved type detection
    if (this.isStandardError(error)) {
      return throwError(() => error);
    }

    const standardError = this.errorHandler.handle(error);
    
    const errorWithContext: StandardError = {
      ...standardError,
      userMessage: `Failed to ${operation}: ${standardError.userMessage}`,
      // Add brand for type checking
      [STANDARD_ERROR_BRAND]: true as const
    };

    return throwError(() => errorWithContext);
  }

  /**
   * Improved StandardError detection
   * Uses multiple checks to avoid false positives:
   * 1. Symbol brand (most reliable)
   * 2. Comprehensive field checking (fallback)
   */
  private isStandardError(error: unknown): error is StandardError {
    if (!error || typeof error !== 'object') {
      return false;
    }

    // Check for brand symbol (most reliable)
    if (STANDARD_ERROR_BRAND in error) {
      return true;
    }

    // Fallback: Comprehensive duck-typing
    // Check ALL required fields to minimize false positives
    const hasAllFields = (
      'errorId' in error &&
      'userMessage' in error &&
      'userTitle' in error &&
      'severity' in error &&
      'timestamp' in error &&
      'errorCode' in error
    );

    if (!hasAllFields) {
      return false;
    }

    // Validate field types
    const err = error as any;
    return (
      typeof err.errorId === 'string' &&
      typeof err.userMessage === 'string' &&
      typeof err.userTitle === 'string' &&
      typeof err.timestamp === 'string' &&
      ['info', 'warning', 'error', 'critical'].includes(err.severity)
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // UTILITIES
  // ═══════════════════════════════════════════════════════════════════

  protected buildHttpParams(filter: TFilter): HttpParams {
    let params = new HttpParams()
      .set('pageIndex', (filter.pageIndex ?? 0).toString())
      .set('pageSize', (filter.pageSize ?? 25).toString());

    if (filter.searchTerm?.trim()) params = params.set('searchTerm', filter.searchTerm.trim());
    if (filter.sortBy) params = params.set('sortBy', filter.sortBy);
    if (filter.sortOrder) params = params.set('sortOrder', filter.sortOrder);
    return params;
  }

  clearCache(): void {
    this.cache.clear();
    this.entitiesSignal.set(null);
  }

  getCacheStats() {
    return this.cache.getStats();
  }
}
