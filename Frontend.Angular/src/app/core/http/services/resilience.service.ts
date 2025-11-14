import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, RetryConfig } from 'rxjs/operators';

import { TraceContext, TraceContextService } from '../../services/trace-context.service';

import { environment } from '../../../environments/environment';
import { ErrorClassifier } from '../../utils/error-classifier.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * RESILIENCE SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Replaced Math.random() with crypto.getRandomValues()
 * ✅ Uses TraceContext (not TraceSnapshot) for consistency
 * ✅ Cryptographically secure jitter generation
 * ✅ Fallback to Math.random() in non-crypto environments
 */

export type RetryMode = 'exponential' | 'linear' | 'constant';

export interface RetryStrategy {
  mode: RetryMode;
  maxRetries: number;
  baseDelay: number;
  maxDelay: number;
  jitterFactor: number;
  excludedStatusCodes: number[];
}

export interface RetryMetadata {
  attempt: number;
  maxAttempts: number;
  delay: number;
  strategy: RetryMode;
  traceId: string;
  spanId: string;
  parentSpanId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ResilienceService {
  private readonly traceContext = inject(TraceContextService);

  private readonly _strategy = signal<RetryStrategy>({
    mode: 'exponential',
    maxRetries: environment.retryPolicy?.maxRetries ?? 3,
    baseDelay: environment.retryPolicy?.baseDelayMs ?? 1000,
    maxDelay: environment.retryPolicy?.maxDelay ?? 10000,
    jitterFactor: 0.25,
    excludedStatusCodes: environment.retryPolicy?.excluded ?? [400, 401, 403, 404, 409],
  });

  readonly strategy = this._strategy.asReadonly();
  readonly maxRetries = computed(() => this._strategy().maxRetries);
  readonly baseDelay = computed(() => this._strategy().baseDelay);
  readonly maxDelay = computed(() => this._strategy().maxDelay);
  readonly mode = computed(() => this._strategy().mode);

  // ═══════════════════════════════════════════════════════════════════
  // Core Methods
  // ═══════════════════════════════════════════════════════════════════

  shouldRetry(error: HttpErrorResponse): boolean {
    const s = this._strategy();
    if (s.excludedStatusCodes.includes(error.status)) return false;
    return ErrorClassifier.isTransient(error);
  }

  calculateDelay(attempt: number, custom?: Partial<RetryStrategy>): number {
    const s = { ...this._strategy(), ...custom };
    const { baseDelay, maxDelay, jitterFactor, mode } = s;

    let delay: number;
    switch (mode) {
      case 'linear':
        delay = baseDelay * attempt;
        break;
      case 'constant':
        delay = baseDelay;
        break;
      default:
        delay = baseDelay * Math.pow(2, attempt - 1);
        break;
    }

    delay = Math.min(delay, maxDelay);
    
    const jitter = this.generateSecureJitter() * (delay * jitterFactor);
    return Math.max(0, delay + jitter);
  }

  getRetryConfig(custom?: Partial<RetryStrategy>): RetryConfig {
    const strategy = { ...this._strategy(), ...custom };
    return {
      count: strategy.maxRetries,
      delay: (error: unknown, attempt: number) =>
        this.getDelayObservable(error, attempt, strategy),
    };
  }

  withRetry<T>(custom?: Partial<RetryStrategy>) {
    const cfg = this.getRetryConfig(custom);
    return retry<T>(cfg);
  }

  /**
   * FIX: Accepts TraceContext (not TraceSnapshot)
   */
  getRetryMetadata(
    attempt: number,
    maxAttempts: number,
    parentContext?: TraceContext
  ): RetryMetadata {
    const parent = parentContext ?? this.traceContext.getCurrentContext();
    const child = this.traceContext.createChildSpan(parent);

    return {
      attempt,
      maxAttempts,
      delay: this.calculateDelay(attempt),
      strategy: this._strategy().mode,
      traceId: child.traceId,
      spanId: child.spanId,
      parentSpanId: child.parentSpanId ?? null,
    };
  }

  updateStrategy(partial: Partial<RetryStrategy>): void {
    this._strategy.update((s) => ({ ...s, ...partial }));
  }

  resetStrategy(): void {
    this._strategy.set({
      mode: 'exponential',
      maxRetries: environment.retryPolicy?.maxRetries ?? 3,
      baseDelay: environment.retryPolicy?.baseDelayMs ?? 1000,
      maxDelay: environment.retryPolicy?.maxDelay ?? 10000,
      jitterFactor: 0.25,
      excludedStatusCodes: environment.retryPolicy?.excluded ?? [400, 401, 403, 404, 409],
    });
  }

  getDiagnostics() {
    const s = this._strategy();
    return {
      mode: s.mode,
      baseDelay: s.baseDelay,
      maxDelay: s.maxDelay,
      jitterFactor: s.jitterFactor,
      exampleDelays: {
        attempt1: this.calculateDelay(1),
        attempt2: this.calculateDelay(2),
        attempt3: this.calculateDelay(3),
      }
    };
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════

  private getDelayObservable(
    error: unknown,
    attempt: number,
    strategy: RetryStrategy
  ): Observable<number> {
    if (!(error instanceof HttpErrorResponse) || !this.shouldRetry(error)) {
      return throwError(() => error);
    }

    const delay = this.calculateDelay(attempt, strategy);

    if (!environment.production) {
      console.info(`[Resilience] Retry ${attempt}/${strategy.maxRetries} after ${delay}ms`, {
        status: error.status,
        url: error.url,
      });
    }

    return timer(delay);
  }

  getRetryDelay(error: HttpErrorResponse, attempt: number): Observable<number> {
    const delay = this.calculateDelay(attempt);
    return timer(delay);
  }

  /**
   * Generate cryptographically secure random jitter
   * Returns a value in range [-1, 1]
   */
  private generateSecureJitter(): number {
    try {
      if (typeof crypto !== 'undefined' && crypto.getRandomValues) {
        const buffer = new Uint32Array(1);
        crypto.getRandomValues(buffer);
        return (buffer[0] / 0xffffffff) * 2 - 1;
      }
    } catch {
      // Crypto API not available or failed
    }

    // Fallback to Math.random() in non-crypto environments
    return Math.random() * 2 - 1;
  }
}
