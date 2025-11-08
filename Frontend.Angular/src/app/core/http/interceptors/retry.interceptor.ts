/**
 * Retry Interceptor - Phase 3 Refactored
 * ✅ Uses ErrorClassifier for error detection
 * ✅ Cleaner logic and better logging
 */

import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap, throwError } from 'rxjs';

import { ResilienceService } from '../../http/services/resilience.service';
import { NetworkErrorTracker } from '../../network/services/network-error-tracker.service';
import { NetworkStatusService } from '../../network/services/network-status.service';
import { TraceContextService } from '../../services/trace-context.service';

import { environment } from '../../../environments/environment';
import { ErrorClassifier } from '../../utils/error-classifier';

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const resilience = inject(ResilienceService);
  const traceContext = inject(TraceContextService);
  const networkStatus = inject(NetworkStatusService);
  const errorTracker = inject(NetworkErrorTracker);

  // Skip retry if header set
  if (req.headers.has('X-Skip-Retry')) {
    const sanitizedRequest = req.clone({
      headers: req.headers.delete('X-Skip-Retry')
    });
    return next(sanitizedRequest);
  }

  const parentContext = traceContext.getCurrentContext();
  const maxRetries = resilience.maxRetries();

  return next(req).pipe(
    tap(event => {
      if (event instanceof HttpResponse) {
        errorTracker.markSuccess(); // Clear network error streak
      }
    }),

    retry({
      count: maxRetries,
      delay: (error: any, attempt: number) => {
        // Only handle HttpErrorResponse
        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }

        // ✅ Use ErrorClassifier instead of inline logic
        const classification = ErrorClassifier.classify(error);

        // Network checks
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

        // ✅ Use ErrorClassifier
        if (!classification.isRetryable) {
          if (!environment.production) {
            console.info('[Retry] Non-retryable error', {
              url: req.url,
              status: error.status,
              category: classification.category,
              reason: ErrorClassifier.getReason(error)
            });
          }
          return throwError(() => error);
        }

        // Create retry span + metadata
        const metadata = resilience.getRetryMetadata(
          attempt,
          maxRetries,
          parentContext
        );

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
              category: classification.category
            }
          );
        }

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