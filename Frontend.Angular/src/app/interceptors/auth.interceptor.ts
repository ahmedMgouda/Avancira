import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

/**
 * Simple and production-safe auth interceptor.
 * - Always sends cookies to your BFF (dev or prod).
 * - Handles 401s cleanly via AuthService.
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  // Always include credentials for your BFF or API calls
  const isBffRequest =
    req.url.startsWith('/bff') ||
    req.url.startsWith(environment.bffBaseUrl);

  const request = isBffRequest
    ? req.clone({ withCredentials: true })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && isBffRequest) {
        console.log('[Auth Interceptor] 401 detected, redirecting to login');
        authService.handleUnauthorized();
      }
      return throwError(() => error);
    })
  );
};
