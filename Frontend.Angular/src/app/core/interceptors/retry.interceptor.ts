import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap,throwError } from 'rxjs';

import { NetworkErrorTracker } from '../network/network-error-tracker.service';
import { NetworkStatusService } from '../network/network-status.service';
import { ResilienceService } from '../services/resilience.service';
import { TraceContextService } from '../services/trace-context.service';

import { environment } from '../../environments/environment';

/**
 * Retry Interceptor (Resilient + Trace Aware)
 * ═══════════════════════════════════════════════════════════════════════
 * Handles automatic retries for transient HTTP failures with:
 *   ✅ Smart retry classification (transient only)
 *   ✅ Exponential/Linear/Constant strategy (from ResilienceService)
 *   ✅ Network-aware retry gating
 *   ✅ W3C trace context for observability
 *   ✅ Signal-based retry config integration
 * 
 * Must come AFTER networkInterceptor in app.config.ts
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const resilience = inject(ResilienceService);
  const traceContext = inject(TraceContextService);
  const networkStatus = inject(NetworkStatusService);
  const errorTracker = inject(NetworkErrorTracker);

  // Skip retry if header set
  if (req.headers.has('X-Skip-Retry')) {
    return next(req);
  }

  const parentContext = traceContext.getCurrentContext();
  const maxRetries = resilience.maxRetries(); // ← dynamic via signal

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        errorTracker.markSuccess(); // clear network error streak
      }
    }),

    retry({
      count: maxRetries,
      delay: (error: any, attempt: number) => {
        // ─────────────────────────────────────────────
        // Only handle HttpErrorResponse
        // ─────────────────────────────────────────────
        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }

        // ─────────────────────────────────────────────
        // Network checks
        // ─────────────────────────────────────────────
        if (!networkStatus.isOnline() || errorTracker.hasRecentNetworkError()) {
          if (!environment.production) {
            console.info('[Retry] Network issue - skipping retry', {
              url: req.url,
              isOnline: networkStatus.isOnline(),
              recentNetworkError: errorTracker.hasRecentNetworkError(),
              consecutive: errorTracker.consecutiveErrors()
            });
          }
          return throwError(() => error);
        }

        // ─────────────────────────────────────────────
        // Retryable classification
        // ─────────────────────────────────────────────
        if (!resilience.shouldRetry(error)) {
          if (!environment.production) {
            console.info('[Retry] Non-retryable error', {
              url: req.url,
              status: error.status,
              reason: getNotRetryableReason(error.status)
            });
          }
          return throwError(() => error);
        }

        // ─────────────────────────────────────────────
        // Create retry span + metadata
        // ─────────────────────────────────────────────
        const metadata = resilience.getRetryMetadata(
          attempt,
          maxRetries,
          parentContext
        );

        // Optional: attach retry span context to outgoing retry request
        // (if you propagate trace headers manually)
        // req = req.clone({ setHeaders: { 'traceparent': traceContext.formatTraceParent(metadata) } });

        if (!req.headers.has('X-Skip-Logging') && !environment.production) {
          console.info(
            `[Retry] Attempt ${metadata.attempt}/${metadata.maxAttempts}`,
            {
              url: req.url,
              status: error.status,
              delay: `${metadata.delay}ms`,
              strategy: metadata.strategy,
              traceId: metadata.traceId,
              spanId: metadata.spanId,
              parentSpanId: metadata.parentSpanId
            }
          );
        }

        // Return observable delay for retry()
        return resilience.getRetryDelay(error, attempt);
      }
    }),

    catchError((error: HttpErrorResponse) => {
      (error as any).__retryFailed = true;
      (error as any).__maxRetriesReached = true;

      if (!req.headers.has('X-Skip-Logging') && !environment.production) {
        console.warn('[Retry] Max attempts reached', {
          url: req.url,
          method: req.method,
          status: error.status,
          message: error.message
        });
      }

      return throwError(() => error);
    })
  );
};

/**
 * Helper: Reason for non-retryable errors
 */
function getNotRetryableReason(status: number): string {
  if (status >= 400 && status < 500) return 'Client error (4xx) – request issue';
  if (status === 500 || status === 501) return 'Permanent server error';
  return 'Unknown / not transient';
}
