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

import { NetworkStatusService } from './network-status.service';

/**
 * Network Interceptor (Simplified - No Retry Logic)
 * ═══════════════════════════════════════════════════════════════════════
 * ONLY handles offline detection and marks network errors
 * Does NOT retry - that's the job of retryInterceptor
 * Does NOT show toasts - NetworkStatusService handles that
 * 
 * Responsibilities:
 *   ✅ Check if offline before making requests
 *   ✅ Wait for connection restoration
 *   ✅ Mark network errors IMMEDIATELY for retry interceptor
 *   ✅ Update network status on errors
 * 
 * CRITICAL: Must be placed BEFORE retryInterceptor in app.config.ts
 * 
 * Skip network check:
 *   httpClient.get('/api/data', {
 *     headers: { 'X-Skip-Network-Check': 'true' }
 *   })
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

  // ──────────────────────────────────────────────────────────────
  // Skip network check if header is present
  // ──────────────────────────────────────────────────────────────
  if (req.headers.has('X-Skip-Network-Check')) {
    const cleanReq = req.clone({
      headers: req.headers.delete('X-Skip-Network-Check'),
    });
    return next(cleanReq);
  }

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
              // Mark network errors even after waiting
              if (err instanceof HttpErrorResponse && isNetworkError(err)) {
                (err as any).__isNetworkError = true;
              }
              subscriber.error(err);
            },
            complete: () => subscriber.complete()
          });
          
          return subscription;
        })
        .catch(_error => {
          // Timeout waiting for connection - create marked error
          const networkError = new HttpErrorResponse({
            error: 'Network connection timeout',
            status: 0,
            statusText: 'Network Timeout',
            url: req.url
          });
          
          // Mark as network error
          (networkError as any).__isNetworkError = true;
          
          subscriber.error(networkError);
        });

      // Cleanup on unsubscribe
      return () => {
        waitPromise.catch(() => {/* ignore */});
      };
    });
  }

  // ──────────────────────────────────────────────────────────────
  // Make request and mark network errors IMMEDIATELY
  // ──────────────────────────────────────────────────────────────
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Detect and mark network errors BEFORE passing to retry interceptor
      if (isNetworkError(error)) {
        // ⚠️ CRITICAL: Mark the error IMMEDIATELY
        (error as any).__isNetworkError = true;
        
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
              isNetworkError: true
            }
          );
        }
      }

      // Pass error to next interceptor with flag set
      return throwError(() => error);
    })
  );
};