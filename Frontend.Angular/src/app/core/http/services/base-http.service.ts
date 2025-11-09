import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, shareReplay, tap } from 'rxjs/operators';

import { ResilienceService } from '../../http/services/resilience.service';
import { LoggerService } from '../../logging/services/logger.service';
import { TraceContextService } from '../../services/trace-context.service';

import { BaseFilter, PaginatedResult } from '../../models/base.model';
import { CacheConfig, EntityCache } from '../../utils/entity-cache.utility';

export abstract class BaseHttpService<
  T extends { id: number },
  TCreate = Omit<T, 'id' | 'createdAt' | 'updatedAt'>,
  TUpdate = TCreate & { id: number },
  TFilter extends BaseFilter = BaseFilter
> {
  protected readonly http = inject(HttpClient);
  protected readonly logger = inject(LoggerService);
  protected readonly resilience = inject(ResilienceService);
  protected readonly traceContext = inject(TraceContextService);

  protected abstract readonly apiUrl: string;
  protected abstract readonly entityName: string;

  protected readonly cache: EntityCache<T>;
  protected readonly entitiesSignal = signal<PaginatedResult<T> | null>(null);
  readonly entities = this.entitiesSignal.asReadonly();

  constructor(cacheConfig: CacheConfig = {}) {
    this.cache = new EntityCache<T>(cacheConfig);
  }

  // --------------------------------------------------------------------------
  // CRUD METHODS
  // --------------------------------------------------------------------------

  getAll(filter: TFilter = {} as TFilter): Observable<PaginatedResult<T>> {
    const params = this.buildHttpParams(filter);
    return this.http.get<PaginatedResult<T>>(this.apiUrl, { params }).pipe(
      tap(result => {
        this.entitiesSignal.set(result);
        this.cache.setList(result.items);
        this.logger.info(`Fetched ${this.entityName} list`, this.enrichLog({ total: result.totalCount }));
      }),
      catchError(error => {
        this.logger.error(`Failed to fetch ${this.entityName} list`, this.enrichLog(error));
        return throwError(() => error);
      })
    );
  }

  getById(id: number, forceRefresh = false): Observable<T> {
    if (!forceRefresh) {
      const cached = this.cache.get(id);
      if (cached) {
        this.logger.debug(`Cache hit for ${this.entityName} #${id}`, this.enrichLog());
        return of(cached);
      }
    }

    return this.http.get<T>(`${this.apiUrl}/${id}`).pipe(
      tap(entity => {
        this.cache.set(entity.id, entity);
        this.logger.info(`Fetched ${this.entityName} #${entity.id}`, this.enrichLog());
      }),
      shareReplay(1),
      catchError(error => {
        this.logger.error(`Failed to fetch ${this.entityName} #${id}`, this.enrichLog(error));
        return throwError(() => error);
      })
    );
  }

  create(dto: TCreate): Observable<T> {
    return this.http.post<T>(this.apiUrl, dto).pipe(
      tap(entity => {
        this.cache.set(entity.id, entity);
        this.cache.invalidateList();
        this.logger.info(`Created ${this.entityName} #${entity.id}`, this.enrichLog());
      }),
      catchError(error => {
        this.logger.error(`Failed to create ${this.entityName}`, this.enrichLog(error));
        return throwError(() => error);
      })
    );
  }

  update(dto: TUpdate): Observable<T> {
    const id = (dto as any).id;
    return this.http.put<T>(`${this.apiUrl}/${id}`, dto).pipe(
      tap(entity => {
        this.cache.set(entity.id, entity);
        this.cache.invalidateList();
        this.logger.info(`Updated ${this.entityName} #${entity.id}`, this.enrichLog());
      }),
      catchError(error => {
        this.logger.error(`Failed to update ${this.entityName} #${id}`, this.enrichLog(error));
        return throwError(() => error);
      })
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        this.cache.invalidate(id);
        this.cache.invalidateList();
        this.logger.warn(`Deleted ${this.entityName} #${id}`, this.enrichLog());
      }),
      catchError(error => {
        this.logger.error(`Failed to delete ${this.entityName} #${id}`, this.enrichLog(error));
        return throwError(() => error);
      })
    );
  }

  // --------------------------------------------------------------------------
  // Utilities
  // --------------------------------------------------------------------------

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
    this.logger.debug(`Cleared ${this.entityName} cache`, this.enrichLog());
  }

  getCacheStats() {
    return this.cache.getStats();
  }

  // Include trace context automatically
  private enrichLog(data?: any) {
    const ctx = this.traceContext.getCurrentContext();
    return { ...data, traceId: ctx.traceId, spanId: ctx.spanId };
  }
}
