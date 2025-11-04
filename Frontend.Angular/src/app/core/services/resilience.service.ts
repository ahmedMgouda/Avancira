import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, RetryConfig } from 'rxjs/operators';

import { LoggerService } from './logger.service';

import { environment } from '../../environments/environment';

export interface RetryStrategy {
  maxRetries: number;
  scalingDuration: number;
  excludedStatusCodes: number[];
  maxDelay?: number;
}

@Injectable({ providedIn: 'root' })
export class ResilienceService {
  private readonly logger = inject(LoggerService);

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

  private getDelay(
    error: unknown,
    retryCount: number,
    strategy: RetryStrategy
  ): Observable<number> {
    if (!(error instanceof HttpErrorResponse) || !this.shouldRetry(error, strategy)) {
      return throwError(() => error);
    }

    const delay = this.calculateDelay(retryCount, strategy);
    const correlationId = this.logger.getCorrelationId();

    this.logger.info(`Retrying (${retryCount}/${strategy.maxRetries})`, {
      delay: `${delay}ms`,
      status: error.status,
      url: error.url ?? undefined,
      correlationId
    });

    return timer(delay);
  }

  private shouldRetry(error: HttpErrorResponse, strategy: RetryStrategy): boolean {
    if (strategy.excludedStatusCodes.includes(error.status)) return false;
    return this.retryableStatuses.includes(error.status);
  }

  /**
   * Calculate retry delay with exponential backoff and jitter
   * 
   * CHANGE: Fixed jitter to be ±25% (not just +25%)
   * This prevents "thundering herd" problem where all clients
   * retry at exactly the same time
   * 
   * Formula:
   *   base = scalingDuration * 2^(retryCount - 1)
   *   capped = min(base, maxDelay)
   *   jitter = random value between -25% and +25% of capped
   *   final = capped + jitter
   * 
   * Example with scalingDuration=1000ms:
   *   Retry 1: 1000ms ± 250ms = 750-1250ms
   *   Retry 2: 2000ms ± 500ms = 1500-2500ms
   *   Retry 3: 4000ms ± 1000ms = 3000-5000ms
   */
  private calculateDelay(retryCount: number, strategy: RetryStrategy): number {
    // Exponential backoff: 1x, 2x, 4x, 8x, ...
    const exponential = strategy.scalingDuration * Math.pow(2, retryCount - 1);

    // Cap at maxDelay
    const capped = Math.min(exponential, strategy.maxDelay ?? 30000);

    // Add jitter: ±25% of the capped value
    // BEFORE: jitter was 0-25% (always adding)
    // AFTER: jitter is -25% to +25% (randomized)
    const jitterRange = 0.25 * capped;
    const jitter = (Math.random() - 0.5) * 2 * jitterRange; // -0.5 to +0.5, scaled by range

    const finalDelay = Math.floor(capped + jitter);

    // Ensure delay is never negative (edge case protection)
    return Math.max(0, finalDelay);
  }
}