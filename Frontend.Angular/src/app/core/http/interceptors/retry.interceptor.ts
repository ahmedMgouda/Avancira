// core/http/interceptors/retry.interceptor.ts
/**
 * Retry Interceptor - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Uses NetworkService (merged service)
 * ✅ Uses ErrorClassifier for error detection
 * ✅ Uses TraceService (merged service)
 */

import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, tap, throwError } from 'rxjs';

import { NetworkService } from '../../network/services/network.service';
import { TraceContextService } from '../../services/trace-context.service';
import { ResilienceService } from '../services/resilience.service';

import { environment } from '../../../environments/environment';
import { ErrorClassifier } from '../../utils/error-classifier.utility';

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const resilience = inject(ResilienceService);
  const traceContext = inject(TraceContextService);
  const network = inject(NetworkService);

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
        network.markSuccess();
      }
    }),

    retry({
      count: maxRetries,
      delay: (error: any, attempt: number) => {
        if (!(error instanceof HttpErrorResponse)) {
          return throwError(() => error);
        }

        const classification = ErrorClassifier.classify(error);

        // Check network health
        if (!network.isHealthy()) {
          if (!environment.production) {
            console.info('[Retry] Network unhealthy - skipping retry', {
              url: req.url,
              status: error.status
            });
          }
          return throwError(() => error);
        }

        // Check if retryable
        if (!classification.isTransient) {
          if (!environment.production) {
            console.info('[Retry] Non-retryable error', {
              url: req.url,
              status: error.status,
              category: classification.category
            });
          }
          return throwError(() => error);
        }

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
              traceId: metadata.traceId
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
          status: error.status
        });
      }

      return throwError(() => error);
    })
  );
};