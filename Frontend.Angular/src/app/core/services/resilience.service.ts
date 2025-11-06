import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, RetryConfig } from 'rxjs/operators';

import { TraceContext,TraceContextService } from './trace-context.service';

import { environment } from '../../environments/environment';

export interface RetryStrategy {
  maxRetries: number;
  scalingDuration: number;
  excludedStatusCodes: number[];
  maxDelay?: number;
}

export interface RetryMetadata {
  attempt: number;
  maxAttempts: number;
  delay: number;
  traceId: string;
  spanId: string;
  parentSpanId?: string;
}

@Injectable({ providedIn: 'root' })
export class ResilienceService {
  private readonly traceContext = inject(TraceContextService);

  private readonly defaultStrategy: RetryStrategy = {
    maxRetries: environment.retryPolicy.maxRetries,
    scalingDuration: environment.retryPolicy.baseDelayMs,
    excludedStatusCodes: environment.retryPolicy.excluded,
    maxDelay: environment.retryPolicy.maxDelay
  };

  private readonly retryableStatuses = [0, 408, 429, 500, 502, 503, 504];

  getRetryConfig(custom?: Partial<RetryStrategy>): RetryConfig {
    const strategy = { ...this.defaultStrategy, ...custom };
    return {
      count: strategy.maxRetries,
      delay: (e, i) => this.getDelay(e, i, strategy)
    };
  }

  withRetry(custom?: Partial<RetryStrategy>) {
    const strategy = { ...this.defaultStrategy, ...custom };
    return retry<any>({
      count: strategy.maxRetries,
      delay: (e, i) => this.getDelay(e, i, strategy)
    });
  }

  /**
   * Get retry metadata for logging/headers (doesn't log itself)
   */
  getRetryMetadata(
    retryCount: number,
    maxRetries: number,
    parentContext?: TraceContext
  ): RetryMetadata {
    // Get current context or use provided parent context
    const currentContext: TraceContext = parentContext || this.traceContext.getCurrentContext();
    
    // Create child span for this retry attempt
    const childContext = this.traceContext.createChildSpan(currentContext, retryCount);

    return {
      attempt: retryCount,
      maxAttempts: maxRetries,
      delay: this.calculateDelay(retryCount, this.defaultStrategy),
      traceId: childContext.traceId,
      spanId: childContext.spanId,
      parentSpanId: childContext.parentSpanId
    };
  }

  /**
   * Public method to get retry delay observable
   * Used by retry interceptor
   */
  getRetryDelay(error: unknown, retryCount: number, custom?: Partial<RetryStrategy>): Observable<number> {
    const strategy = { ...this.defaultStrategy, ...custom };
    return this.getDelay(error, retryCount, strategy);
  }

  private getDelay(
    error: unknown,
    retryCount: number,
    strategy: RetryStrategy
  ): Observable<number> {
    if (!(error instanceof HttpErrorResponse) || !this.shouldRetry(error, strategy)) {
      return throwError(() => error);
    }

    const delay = this.calculateDelay(retryCount, strategy);

    // Don't log here - let interceptor handle logging with proper context
    if (!environment.production) {
      console.info(
        `[Resilience] Retry ${retryCount}/${strategy.maxRetries} after ${delay}ms`,
        { status: error.status, url: error.url }
      );
    }

    return timer(delay);
  }

  private shouldRetry(error: HttpErrorResponse, strategy: RetryStrategy): boolean {
    if (strategy.excludedStatusCodes.includes(error.status)) return false;
    return this.retryableStatuses.includes(error.status);
  }

  /**
   * Calculate retry delay with exponential backoff and jitter
   * Formula: base * 2^(retry-1) ± 25% jitter, capped at maxDelay
   */
  private calculateDelay(retryCount: number, strategy: RetryStrategy): number {
    const exponential = strategy.scalingDuration * Math.pow(2, retryCount - 1);
    const capped = Math.min(exponential, strategy.maxDelay ?? 30000);
    const jitterRange = 0.25 * capped;
    const jitter = (Math.random() - 0.5) * 2 * jitterRange;
    const finalDelay = Math.floor(capped + jitter);
    return Math.max(0, finalDelay);
  }
}