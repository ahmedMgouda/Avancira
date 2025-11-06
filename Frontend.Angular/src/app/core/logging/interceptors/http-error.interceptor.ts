import { HttpErrorResponse,HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ErrorHandlerService } from '../services/error-handler.service';

/**
 * HTTP error interceptor with deduplication
 * Marks errors as __logged to prevent duplicate logging
 * Respects X-Skip-Logging header
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  // Check if logging should be skipped
  if (req.headers.has('X-Skip-Logging')) {
    return next(req);
  }

  const errorHandler = inject(ErrorHandlerService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // Mark as logged to prevent duplication in global handler
        (error as any).__logged = true;
        
        // Handle and log the error
        errorHandler.handle(error);
      }
      
      return throwError(() => error);
    })
  );
};