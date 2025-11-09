// core/network/interceptors/network.interceptor.ts
/**
 * Network Interceptor - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Uses NetworkService (merged service)
 * ✅ Uses ErrorClassifier for error detection
 * ✅ Uses ToastManager (instead of ToastService)
 * ✅ Uses HTTP_ERROR_METADATA for consistent messages
 */

import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';
import { NetworkService } from '../services/network.service';

import { ErrorClassifier } from '../../utils/error-classifier.utility';

export const networkInterceptor: HttpInterceptorFn = (req, next) => {
  const network = inject(NetworkService);
  const toast = inject(ToastManager);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const classification = ErrorClassifier.classify(error);

        // Track error in network service
        network.trackError(error);

        // Show toast for network errors
        if (classification.category === 'network') {
          toast.error(
            classification.metadata.userMessage,
            classification.metadata.title
          );
        }
      }

      return throwError(() => error);
    })
  );
};