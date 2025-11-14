import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, shareReplay, tap } from 'rxjs/operators';

import { ErrorHandlerService } from '../../logging/services/error-handler.service';
import { StandardError, STANDARD_ERROR_BRAND } from '../../logging/models/standard-error.model';
import { BaseFilter, PaginatedResult } from '../../models/base.model';
import { CacheConfig, EntityCache } from '../../utils/entity-cache.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * BASE HTTP SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Correct StandardError property checking (code, not errorCode)
 * ✅ Correct timestamp type (Date, not string)
 * ✅ Symbol added correctly without type errors
 * ✅ Type-safe error detection
 */

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
  // ERROR HANDLING - FIXED
  // ═══════════════════════════════════════════════════════════════════

  private handleError(error: unknown, operation: string): Observable<never> {
    // Check if already a StandardError
    if (this.isStandardError(error)) {
      return throwError(() => error);
    }

    // Convert to StandardError
    const standardError = this.errorHandler.handle(error);
    
    // Add context and brand
    const errorWithContext = {
      ...standardError,
      userMessage: `Failed to ${operation}: ${standardError.userMessage}`,
    };

    // Add symbol for type checking (runtime only, doesn't affect type)
    Object.defineProperty(errorWithContext, STANDARD_ERROR_BRAND, {
      value: true,
      enumerable: false,
      writable: false
    });

    return throwError(() => errorWithContext as StandardError);
  }

  /**
   * FIX: Correct StandardError detection
   * - Checks for correct properties (code, not errorCode)
   * - Checks correct timestamp type (Date, not string)
   * - Uses symbol for most reliable check
   */
  private isStandardError(error: unknown): error is StandardError {
    if (!error || typeof error !== 'object') {
      return false;
    }

    // FIX 1: Check for symbol first (most reliable)
    if (STANDARD_ERROR_BRAND in error) {
      return true;
    }

    // FIX 2: Fallback to comprehensive field checking with CORRECT property names
    const hasAllFields = (
      'errorId' in error &&
      'userMessage' in error &&
      'userTitle' in error &&
      'severity' in error &&
      'timestamp' in error &&
      'code' in error  // FIX: was 'errorCode'
    );

    if (!hasAllFields) {
      return false;
    }

    // FIX 3: Validate types correctly
    const err = error as any;
    return (
      typeof err.errorId === 'string' &&
      typeof err.userMessage === 'string' &&
      typeof err.userTitle === 'string' &&
      typeof err.code === 'string' &&  // FIX: was errorCode
      err.timestamp instanceof Date &&  // FIX: was string check
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
