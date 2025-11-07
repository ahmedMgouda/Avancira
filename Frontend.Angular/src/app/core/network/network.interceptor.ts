import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { NetworkErrorTracker } from './network-error-tracker.service';
import { NetworkStatusService } from './network-status.service';

/**
 * Network Interceptor (Simplified - No Retry Logic)
 * ═══════════════════════════════════════════════════════════════════════
 * ONLY handles offline detection and tracks network errors using signals
 * Does NOT retry - that's the job of retryInterceptor
 * Does NOT show toasts - NetworkStatusService handles that
 * 
 * FIXES IMPLEMENTED:
 *   ✅ Uses NetworkErrorTracker service instead of error mutation
 *   ✅ Type-safe signal-based error tracking
 *   ✅ Clean separation of concerns
 *   ✅ No object mutations
 * 
 * Responsibilities:
 *   ✅ Check if offline before making requests
 *   ✅ Wait for connection restoration
 *   ✅ Track network errors via NetworkErrorTracker
 *   ✅ Update network status on errors
 * 
 * CRITICAL: Must be placed BEFORE retryInterceptor in app.config.ts
 * 
 * Example:
 *   providers: [
 *     provideHttpClient(
 *       withInterceptors([
 *         traceContextInterceptor,  // ← First (adds trace headers)
 *         networkInterceptor,       // ← Second (detects network issues)
 *         retryInterceptor          // ← Third (retries with backoff)
 *       ])
 *     )
 *   ]
 */

const CONNECTION_WAIT_TIMEOUT = 15000;

/**
 * Detect if error is network-related
 * Enhanced detection for various network error scenarios
 */
function isNetworkError(error: HttpErrorResponse): boolean {
  return (
    error.status === 0 ||                                    // No response from server
    error.error instanceof ProgressEvent ||                  // Network failure event
    error.statusText === 'Unknown Error' ||                  // Browser network error
    error.statusText === '' ||                               // Empty status text
    error.message?.includes('Http failure response') ||      // Angular HTTP error message
    error.message?.includes('ERR_CONNECTION_REFUSED') ||     // Connection refused
    error.message?.includes('ERR_NAME_NOT_RESOLVED') ||      // DNS error
    error.message?.includes('ERR_INTERNET_DISCONNECTED')     // Internet disconnected
  );
}

export const networkInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const networkService = inject(NetworkStatusService);
  const errorTracker = inject(NetworkErrorTracker);

  // ──────────────────────────────────────────────────────────────
  // Check if offline before making request
  // ──────────────────────────────────────────────────────────────
  if (!networkService.isOnline()) {
    return new Observable(subscriber => {
      const waitPromise = networkService
        .waitForOnline(CONNECTION_WAIT_TIMEOUT)
        .then(() => {
          // Connection restored, make the request
          const subscription = next(req).subscribe({
            next: value => subscriber.next(value),
            error: err => {
              // Track network errors even after waiting
              if (err instanceof HttpErrorResponse && isNetworkError(err)) {
                errorTracker.markNetworkError();
              }
              subscriber.error(err);
            },
            complete: () => subscriber.complete()
          });
          
          return subscription;
        })
        .catch(_error => {
          // Timeout waiting for connection - track error
          errorTracker.markNetworkError();
          
          const networkError = new HttpErrorResponse({
            error: 'Network connection timeout',
            status: 0,
            statusText: 'Network Timeout',
            url: req.url
          });
          
          subscriber.error(networkError);
        });

      // Cleanup on unsubscribe
      return () => {
        waitPromise.catch(() => {/* ignore */});
      };
    });
  }

  // ──────────────────────────────────────────────────────────────
  // Make request and track network errors via NetworkErrorTracker
  // FIX #3: Signal-based error tracking (no object mutation)
  // ──────────────────────────────────────────────────────────────
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Detect and track network errors
      if (isNetworkError(error)) {
        // FIX #3: Use signal-based error tracker
        errorTracker.markNetworkError();
        
        // Trigger network status verification (will show toasts if needed)
        networkService.verifyConnection();
        
        // Optional: Log in development
        if (!req.headers.has('X-Skip-Logging')) {
          console.info(
            '[NetworkInterceptor] Network error detected',
            {
              url: req.url,
              status: error.status,
              statusText: error.statusText,
              message: error.message,
              tracked: true
            }
          );
        }
      } else {
        // Non-network error - mark success to clear error state
        errorTracker.markSuccess();
      }

      // Pass error to next interceptor
      return throwError(() => error);
    })
  );
};