import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ErrorHandlerService } from '../services/error-handler.service';
import { NotificationService } from '../services/notification.service';

/**
 * Global Error Interceptor
 * ─────────────────────────────────────────────────────────────
 * Converts raw HttpErrorResponse → AppError
 * Delegates notification decisions to NotificationService
 * 
 * DESIGN CHANGE: This interceptor now focuses ONLY on:
 *   1. Error transformation (via ErrorHandlerService)
 *   2. Notification delegation (to NotificationService)
 * 
 * All notification policy (environment flags, severity, status codes)
 * is now handled by NotificationService.fromAppError()
 */
export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const errorHandler = inject(ErrorHandlerService);
  const notification = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Skip 401 - handled by AuthInterceptor
      if (error.status === 401) {
        return throwError(() => error);
      }

      // Transform HTTP error to normalized AppError
      const appError = errorHandler.handleHttpError(
        error,
        `${req.method} ${req.url}`
      );

      // Delegate notification decision to NotificationService
      // CHANGE: Removed all notification policy logic from here
      // NotificationService.fromAppError() now handles:
      //   - environment.disableNotifications check
      //   - Skip status codes (404, etc.)
      //   - Severity-based filtering (info/warning)
      notification.fromAppError(appError);

      // Rethrow as AppError for downstream handling
      return throwError(() => appError);
    })
  );
};