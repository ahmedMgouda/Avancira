/**
 * Network Interceptor - Phase 3 Refactored
 * ✅ Uses ErrorClassifier for error detection
 * ✅ Cleaner separation of concerns
 */

import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { NetworkErrorTracker } from '../services/network-error-tracker.service';
import { NetworkStatusService } from '../services/network-status.service';

import { ErrorClassifier } from '../../utils/error-classifier';

const CONNECTION_WAIT_TIMEOUT = 15000;

export const networkInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const networkService = inject(NetworkStatusService);
  const errorTracker = inject(NetworkErrorTracker);

  // Check if offline before making request
  if (!networkService.isOnline()) {
    return new Observable(subscriber => {
      const waitPromise = networkService
        .waitForOnline(CONNECTION_WAIT_TIMEOUT)
        .then(() => {
          const subscription = next(req).subscribe({
            next: value => subscriber.next(value),
            error: err => {
              // ✅ Use ErrorClassifier
              if (err instanceof HttpErrorResponse && ErrorClassifier.isNetworkError(err)) {
                errorTracker.markNetworkError();
              }
              subscriber.error(err);
            },
            complete: () => subscriber.complete()
          });
          
          return subscription;
        })
        .catch(_error => {
          errorTracker.markNetworkError();
          
          const networkError = new HttpErrorResponse({
            error: 'Network connection timeout',
            status: 0,
            statusText: 'Network Timeout',
            url: req.url
          });
          
          subscriber.error(networkError);
        });

      return () => {
        waitPromise.catch(() => {/* ignore */});
      };
    });
  }

  // Make request and track network errors
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // ✅ Use ErrorClassifier
      if (ErrorClassifier.isNetworkError(error)) {
        errorTracker.markNetworkError();
        networkService.verifyConnection();
        
        if (!req.headers.has('X-Skip-Logging')) {
          console.info(
            '[NetworkInterceptor] Network error detected',
            {
              url: req.url,
              status: error.status,
              statusText: error.statusText,
              category: ErrorClassifier.categorize(error),
              tracked: true
            }
          );
        }
      } else {
        errorTracker.markSuccess();
      }

      return throwError(() => error);
    })
  );
};
