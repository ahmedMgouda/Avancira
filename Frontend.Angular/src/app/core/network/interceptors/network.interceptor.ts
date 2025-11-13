import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { NetworkService } from '../services/network.service';
import { ErrorClassifier } from '../../utils/error-classifier.utility';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * NETWORK INTERCEPTOR
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * Single Responsibility: Detect and report network errors
 * 
 * RESPONSIBILITIES:
 * ----------------
 * ✅ Classify HTTP errors
 * ✅ Report network-related errors to NetworkService
 * 
 * DOES NOT:
 * ---------
 * ❌ Show user notifications (NetworkNotificationService handles this)
 * ❌ Log errors (Logger handles this)
 * ❌ Handle state management (NetworkService handles this)
 * 
 * CLEAN SEPARATION:
 * -----------------
 * This interceptor ONLY detects and reports errors.
 * NetworkNotificationService subscribes to NetworkService state
 * and decides when/how to notify users.
 */

export const networkInterceptor: HttpInterceptorFn = (req, next) => {
  const network = inject(NetworkService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const classification = ErrorClassifier.classify(error);

        // Only track TRUE network and timeout errors
        // Exclude client errors (400-499 except 408)
        const isNetworkRelated =
          classification.category === 'network' ||
          classification.category === 'timeout';

        // Report to NetworkService (no notifications here!)
        if (isNetworkRelated) {
          network.trackError(true);
        }
      }

      return throwError(() => error);
    })
  );
};
