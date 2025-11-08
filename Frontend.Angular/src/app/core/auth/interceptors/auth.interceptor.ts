import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';

import { AuthService } from '../../auth/services/auth.service';

import { environment } from '../../../environments/environment';

/**
 * Authentication Interceptor
 * ─────────────────────────────────────────────────────────────
 * Automatically sends cookies to BFF endpoints
 * Handles 401 responses from the BFF by triggering AuthService logic
 * Leaves non-BFF requests untouched
 * 
 * ENHANCEMENT: Added guard against 401 redirect storms
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  // ────────────────────────────────────────────────────────
  // Determine whether this request targets the BFF
  // ────────────────────────────────────────────────────────
  const isBffRequest =
    req.url.startsWith('/bff') ||
    (environment.bffBaseUrl && req.url.startsWith(environment.bffBaseUrl));

  // Clone request only if it's a BFF request
  const request = isBffRequest
    ? req.clone({ withCredentials: true }) // send cookies
    : req;

  // ────────────────────────────────────────────────────────
  // Pass to next interceptor and handle 401 responses
  // ────────────────────────────────────────────────────────
  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && isBffRequest) {
        // Trigger centralized re-authentication or logout
        // NEW: AuthService now guards against multiple simultaneous redirects
        authService.handleUnauthorized();
      }
      return throwError(() => error);
    })
  );
};