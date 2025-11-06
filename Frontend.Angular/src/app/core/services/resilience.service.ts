import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, RetryConfig } from 'rxjs/operators';

import { TraceContext, TraceContextService } from './trace-context.service';

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

/**
 * Resilience Service (Angular 19 - Signal-Based)
 * ═══════════════════════════════════════════════════════════════════════
 * Single source of truth for retry logic and exponential backoff
 * Used by both HTTP interceptors and network status monitoring
 * 
 * Features:
 *   ✅ Exponential backoff with jitter
 *   ✅ Configurable retry strategies (signals)
 *   ✅ W3C trace context support
 *   ✅ Retryable status detection
 *   ✅ Angular 19 signal-based API
 */
@Injectable({ providedIn: 'root' })
export class ResilienceService {
  private readonly traceContext = inject(TraceContextService);

  // ────────────────────────────────────────────────────────────────
  // SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  private readonly _strategy = signal<RetryStrategy>({
    maxRetries: environment.retryPolicy.maxRetries,
    scalingDuration: environment.retryPolicy.baseDelayMs,
    excludedStatusCodes: environment.retryPolicy.excluded,
    maxDelay: environment.retryPolicy.maxDelay
  });

  private readonly _retryableStatuses = signal([0, 408, 429, 500, 502, 503, 504]);

  // Public readonly signals
  readonly strategy = this._strategy.asReadonly();
  readonly retryableStatuses = this._retryableStatuses.asReadonly();
  
  // Computed signals
  readonly maxRetries = computed(() => this._strategy().maxRetries);
  readonly baseDelay = computed(() => this._strategy().scalingDuration);
  readonly maxDelay = computed(() => this._strategy().maxDelay ?? 30000);

  // ────────────────────────────────────────────────────────────────
  // PUBLIC API
  // ────────────────────────────────────────────────────────────────

  /**
   * Get RxJS retry configuration
   */
  getRetryConfig(custom?: Partial<RetryStrategy>): RetryConfig {
    const strategy = { ...this._strategy(), ...custom };
    return {
      count: strategy.maxRetries,
      delay: (e, i) => this.getDelay(e, i, strategy)
    };
  }

  /**
   * Get RxJS retry operator
   */
  withRetry(custom?: Partial<RetryStrategy>) {
    const strategy = { ...this._strategy(), ...custom };
    return retry<any>({
      count: strategy.maxRetries,
      delay: (e, i) => this.getDelay(e, i, strategy)
    });
  }

  /**
   * Get retry metadata for logging/headers
   */
  getRetryMetadata(
    retryCount: number,
    maxRetries: number,
    parentContext?: TraceContext
  ): RetryMetadata {
    const currentContext: TraceContext = parentContext || this.traceContext.getCurrentContext();
    const childContext = this.traceContext.createChildSpan(currentContext, retryCount);

    return {
      attempt: retryCount,
      maxAttempts: maxRetries,
      delay: this.calculateDelay(retryCount),
      traceId: childContext.traceId,
      spanId: childContext.spanId,
      parentSpanId: childContext.parentSpanId
    };
  }

  /**
   * Get retry delay observable (for interceptors)
   */
  getRetryDelay(
    error: unknown,
    retryCount: number,
    custom?: Partial<RetryStrategy>
  ): Observable<number> {
    const strategy = { ...this._strategy(), ...custom };
    return this.getDelay(error, retryCount, strategy);
  }

  /**
   * Calculate retry delay with exponential backoff and jitter
   * Uses current signal values
   * 
   * Formula: base * 2^(retry-1) ± 25% jitter, capped at maxDelay
   * 
   * @param retryCount Current retry attempt (1-based)
   * @param customStrategy Optional custom strategy (overrides signal values)
   * @returns Delay in milliseconds
   */
  calculateDelay(retryCount: number, customStrategy?: RetryStrategy): number {
    const strategy = customStrategy ?? this._strategy();
    const exponential = strategy.scalingDuration * Math.pow(2, retryCount - 1);
    const capped = Math.min(exponential, strategy.maxDelay ?? 30000);
    const jitterRange = 0.25 * capped;
    const jitter = (Math.random() - 0.5) * 2 * jitterRange;
    const finalDelay = Math.floor(capped + jitter);
    return Math.max(0, finalDelay);
  }

  /**
   * Check if error should be retried
   */
  shouldRetry(error: HttpErrorResponse, custom?: Partial<RetryStrategy>): boolean {
    const strategy = { ...this._strategy(), ...custom };
    if (strategy.excludedStatusCodes.includes(error.status)) return false;
    return this._retryableStatuses().includes(error.status);
  }

  /**
   * Update retry strategy (signal-based)
   */
  updateStrategy(partial: Partial<RetryStrategy>): void {
    this._strategy.update(current => ({ ...current, ...partial }));
  }

  /**
   * Reset to default strategy
   */
  resetStrategy(): void {
    this._strategy.set({
      maxRetries: environment.retryPolicy.maxRetries,
      scalingDuration: environment.retryPolicy.baseDelayMs,
      excludedStatusCodes: environment.retryPolicy.excluded,
      maxDelay: environment.retryPolicy.maxDelay
    });
  }

  /**
   * Add retryable status code
   */
  addRetryableStatus(status: number): void {
    this._retryableStatuses.update(statuses => 
      statuses.includes(status) ? statuses : [...statuses, status]
    );
  }

  /**
   * Remove retryable status code
   */
  removeRetryableStatus(status: number): void {
    this._retryableStatuses.update(statuses => 
      statuses.filter(s => s !== status)
    );
  }

  // ────────────────────────────────────────────────────────────────
  // INTERNAL
  // ────────────────────────────────────────────────────────────────

  private getDelay(
    error: unknown,
    retryCount: number,
    strategy: RetryStrategy
  ): Observable<number> {
    if (!(error instanceof HttpErrorResponse) || !this.shouldRetry(error, strategy)) {
      return throwError(() => error);
    }

    const delay = this.calculateDelay(retryCount, strategy);

    if (!environment.production) {
      console.info(
        `[Resilience] Retry ${retryCount}/${strategy.maxRetries} after ${delay}ms`,
        { status: error.status, url: error.url }
      );
    }

    return timer(delay);
  }
}