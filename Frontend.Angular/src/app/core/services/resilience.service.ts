import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { retry, RetryConfig } from 'rxjs/operators';
import { TraceContext, TraceContextService } from './trace-context.service';
import { environment } from '../../environments/environment';

/**
 * Retry Strategy Modes
 */
export type RetryMode = 'exponential' | 'linear' | 'constant';

/**
 * Retry Strategy Configuration
 */
export interface RetryStrategy {
  mode: RetryMode;
  maxRetries: number;
  baseDelay: number;
  maxDelay: number;
  jitterFactor: number; // e.g., 0.25 = ±25%
  excludedStatusCodes: number[];
}

/**
 * Retry Metadata for logging & tracing
 */
export interface RetryMetadata {
  attempt: number;
  maxAttempts: number;
  delay: number;
  strategy: RetryMode;
  traceId: string;
  spanId: string;
  parentSpanId?: string | null;
}

/**
 * 🛡️ ResilienceService (Final Type-Safe Version)
 * --------------------------------------------------------------
 * ✅ Generic retry operator for any observable type
 * ✅ Exponential / Linear / Constant strategies
 * ✅ Signal-based configuration + environment defaults
 * ✅ W3C trace metadata support
 * ✅ Smart transient error classification
 */
@Injectable({ providedIn: 'root' })
export class ResilienceService {
  private readonly traceContext = inject(TraceContextService);

  // ────────────────────────────────────────────────────────────────
  // SIGNALS
  // ────────────────────────────────────────────────────────────────

  private readonly _strategy = signal<RetryStrategy>({
    mode: 'exponential',
    maxRetries: environment.retryPolicy?.maxRetries ?? 3,
    baseDelay: environment.retryPolicy?.baseDelayMs ?? 1000,
    maxDelay: environment.retryPolicy?.maxDelay ?? 10000,
    jitterFactor: 0.25,
    excludedStatusCodes: environment.retryPolicy?.excluded ?? [400, 401, 403, 404, 409],
  });

  private readonly _retryableStatuses = signal<number[]>([0, 408, 429, 500, 502, 503, 504]);

  // Readonly signals
  readonly strategy = this._strategy.asReadonly();
  readonly retryableStatuses = this._retryableStatuses.asReadonly();

  // Computed properties
  readonly maxRetries = computed(() => this._strategy().maxRetries);
  readonly baseDelay = computed(() => this._strategy().baseDelay);
  readonly maxDelay = computed(() => this._strategy().maxDelay);
  readonly mode = computed(() => this._strategy().mode);

  // ────────────────────────────────────────────────────────────────
  // PUBLIC METHODS
  // ────────────────────────────────────────────────────────────────

  shouldRetry(error: HttpErrorResponse): boolean {
    const s = this._strategy();
    const status = error.status;
    if (s.excludedStatusCodes.includes(status)) return false;
    return this._retryableStatuses().includes(status);
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
      default: // exponential
        delay = baseDelay * Math.pow(2, attempt - 1);
        break;
    }

    delay = Math.min(delay, maxDelay);
    const jitter = (Math.random() * 2 - 1) * (delay * jitterFactor);
    return Math.max(0, delay + jitter);
  }

  /**
   * ✅ Type-safe generic RetryConfig
   */
  getRetryConfig(custom?: Partial<RetryStrategy>): RetryConfig {
    const strategy = { ...this._strategy(), ...custom };
    return {
      count: strategy.maxRetries,
      delay: (error: unknown, attempt: number) =>
        this.getDelayObservable(error, attempt, strategy),
    };
  }

  /**
   * ✅ Generic operator – type propagates automatically
   */
  withRetry<T>(custom?: Partial<RetryStrategy>) {
    const cfg = this.getRetryConfig(custom);
    return retry<T>(cfg);
  }

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

  getRetryMetadata(
    attempt: number,
    maxAttempts: number,
    parentContext?: TraceContext
  ): RetryMetadata {
    const parent = parentContext ?? this.traceContext.getCurrentContext();
    const child = this.traceContext.createChildSpan(parent, attempt);

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

  addRetryableStatus(status: number): void {
    this._retryableStatuses.update((statuses) =>
      statuses.includes(status) ? statuses : [...statuses, status]
    );
  }

  removeRetryableStatus(status: number): void {
    this._retryableStatuses.update((statuses) =>
      statuses.filter((s) => s !== status)
    );
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
      },
      retryableStatuses: this._retryableStatuses(),
    };
  }
}
