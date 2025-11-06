import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, throwError } from 'rxjs';

import { ResilienceService } from '../services/resilience.service';
import { TraceContextService } from '../services/trace-context.service';

import { environment } from '../../environments/environment';

/**
 * Retry Interceptor (No Toast Notifications)
 * ═══════════════════════════════════════════════════════════════════════
 * Handles automatic retries with exponential backoff using ResilienceService
 * Silent retries - NO toast notifications (handled by NetworkStatusService)
 * 
 * Features:
 *   ✅ W3C Trace Context support
 *   ✅ Respects X-Skip-Retry header
 *   ✅ Uses ResilienceService for all retry logic
 *   ✅ Network error aware
 * 
 * Skip retry:
 *   httpClient.get('/api/data', {
 *     headers: { 'X-Skip-Retry': 'true' }
 *   })
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const resilience = inject(ResilienceService);
  const traceContext = inject(TraceContextService);

  // Check if retry should be skipped
  if (req.headers.has('X-Skip-Retry')) {
    return next(req);
  }

  // Get parent trace context at the start of the request
  const parentContext = traceContext.getCurrentContext();
  const maxRetries = 3;

  return next(req).pipe(
    retry({
      count: maxRetries,
      delay: (error: any, retryAttempt: number) => {
        // Only retry on HTTP errors
        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }

        // Get retry metadata (includes new span context)
        const metadata = resilience.getRetryMetadata(
          retryAttempt,
          maxRetries,
          parentContext
        );

        // Determine if this is a network error
        const isNetworkError = (error as any).__isNetworkError === true;

        // Log retry attempt (development only)
        if (!req.headers.has('X-Skip-Logging') && !environment.production) {
          console.info(
            `[Retry] Attempt ${metadata.attempt}/${metadata.maxAttempts}`,
            {
              url: req.url,
              method: req.method,
              status: error.status,
              delay: `${metadata.delay}ms`,
              isNetworkError,
              traceId: metadata.traceId,
              spanId: metadata.spanId,
              parentSpanId: metadata.parentSpanId
            }
          );
        }

        // Use ResilienceService for delay calculation
        return resilience.getRetryDelay(error, retryAttempt);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      // Mark error with retry information
      (error as any).__retryFailed = true;
      (error as any).__maxRetriesReached = true;

      return throwError(() => error);
    })
  );
};