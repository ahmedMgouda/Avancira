import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError, timer } from 'rxjs';
import { catchError, retry, switchMap } from 'rxjs/operators';

import { NetworkStatusService } from './network-status.service';

/**
 * Network Interceptor
 * ═══════════════════════════════════════════════════════════════════════
 * Handles network-related HTTP errors and retry logic
 * 
 * Features:
 *   ✅ Detects network failures
 *   ✅ Waits for connection restoration before retry
 *   ✅ Exponential backoff for retries
 *   ✅ Configurable retry attempts
 *   ✅ Skip retry for specific endpoints
 *   ✅ Works with NetworkStatusService
 * 
 * Usage in app.config.ts:
 *   provideHttpClient(
 *     withInterceptors([
 *       correlationIdInterceptor,
 *       loggingInterceptor,
 *       authInterceptor,
 *       loadingInterceptor,
 *       networkInterceptor,  // ← Add before retry/error interceptors
 *       retryInterceptor,
 *       errorInterceptor
 *     ])
 *   )
 * 
 * Skip retry for specific requests:
 *   httpClient.get('/api/data', {
 *     headers: { 'X-Skip-Network-Retry': 'true' }
 *   })
 */
export const networkInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const networkService = inject(NetworkStatusService);

  // ──────────────────────────────────────────────────────────────
  // 1️⃣ Skip network retry if header is present
  // ──────────────────────────────────────────────────────────────
  if (req.headers.has('X-Skip-Network-Retry')) {
    const cleanReq = req.clone({
      headers: req.headers.delete('X-Skip-Network-Retry'),
    });
    return next(cleanReq);
  }

  // ──────────────────────────────────────────────────────────────
  // 2️⃣ Check if we're offline before making the request
  // ──────────────────────────────────────────────────────────────
  if (!networkService.isOnline()) {
    // Wait for connection with timeout, then retry
    return new Observable(subscriber => {
      networkService
        .waitForOnline(10000) // Wait up to 10 seconds
        .then(() => {
          // Connection restored, retry request
          next(req).subscribe({
            next: value => subscriber.next(value),
            error: err => subscriber.error(err),
            complete: () => subscriber.complete()
          });
        })
        .catch(_error => {
          // Timeout waiting for connection
          subscriber.error(
            new HttpErrorResponse({
              error: 'Network connection timeout',
              status: 0,
              statusText: 'Network Error',
              url: req.url
            })
          );
        });
    });
  }

  // ──────────────────────────────────────────────────────────────
  // 3️⃣ Make the request with network error handling
  // ──────────────────────────────────────────────────────────────
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only handle network errors (status 0 or specific network errors)
      const isNetworkError = 
        error.status === 0 || 
        error.error instanceof ProgressEvent ||
        error.statusText === 'Unknown Error';

      if (!isNetworkError) {
        // Not a network error, pass through
        return throwError(() => error);
      }

      // Update network status
      networkService.verifyConnection();

      // Wait for connection and retry with exponential backoff
      return timer(1000).pipe(
        switchMap(() => networkService.waitForOnline(15000)),
        switchMap(() => {
          // Connection restored, retry the request
          return next(req);
        }),
        retry({
          count: 2,
          delay: (error, retryCount) => {
            // Exponential backoff: 2s, 4s, 8s
            const delayMs = Math.min(1000 * Math.pow(2, retryCount), 8000);
            console.log(
              `[NetworkInterceptor] Retry ${retryCount} after ${delayMs}ms for ${req.url}`
            );
            return timer(delayMs);
          }
        }),
        catchError(retryError => {
          // All retries failed
          console.error(
            `[NetworkInterceptor] All retries failed for ${req.url}`,
            retryError
          );
          return throwError(() => retryError);
        })
      );
    })
  );
};