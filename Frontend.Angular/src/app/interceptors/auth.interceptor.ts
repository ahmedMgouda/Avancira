
// ============================================
// src/app/core/interceptors/auth.interceptor.ts
// ============================================
import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

/**
 * Skip auth entirely for this request
 * Used for /connect/authorize, /connect/token, /connect/revoke
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/**
 * Include credentials (withCredentials=true) for this request
 * Used for OAuth endpoints that need cookies
 */
export const INCLUDE_CREDENTIALS = new HttpContextToken<boolean>(() => false);

/** Prevent infinite retry loops on 401 */
const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);

/**
 * HTTP Interceptor for Authorization
 *
 * Responsibilities:
 * 1. Identify API requests (match against environment.apiUrl)
 * 2. Skip auth for requests marked with SKIP_AUTH token
 * 3. Acquire valid access token (triggers refresh if needed)
 * 4. Add Authorization: Bearer <token> header
 * 5. Handle 401 errors with single retry + redirect
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  const isApiRequest = isApiCall(req, environment.apiUrl);
  const skipAuth = req.context.get(SKIP_AUTH);

  // Conditionally enable credentials for this request
  let baseReq = req;
  if (req.context.get(INCLUDE_CREDENTIALS)) {
    baseReq = req.withCredentials !== true ? req.clone({ withCredentials: true }) : req;
  }

  // Skip auth if not an API request or explicitly marked to skip
  if (!isApiRequest || skipAuth) {
    return next(baseReq);
  }

  // Acquire valid token and add Authorization header
  return authService.getValidAccessToken().pipe(
    switchMap((token) => next(addAuthHeader(baseReq, token))),
    catchError((error) => handleAuthError(error, baseReq, next, authService))
  );
};

/**
 * Handle HTTP errors - particularly 401 Unauthorized
 *
 * Strategy:
 * 1. If 401 and not retried: get fresh token and retry
 * 2. If 401 after retry or no token: redirect to signin
 * 3. Other errors: pass through
 */
function handleAuthError(
  error: unknown,
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  const alreadyRetried = req.context.get(ALREADY_RETRIED);
  const is401 =
    error instanceof HttpErrorResponse &&
    error.status === HttpStatusCode.Unauthorized;

  // Retry on 401 with fresh token (not stale one)
  if (is401 && !alreadyRetried) {
    return authService.getValidAccessToken().pipe(
      switchMap((freshToken) =>
        next(
          addAuthHeader(
            req.clone({ context: req.context.set(ALREADY_RETRIED, true) }),
            freshToken
          )
        )
      )
    );
  }

  // Unauthorized after retry or no token available - redirect to signin
  if (is401) {
    authService.handleUnauthorized();
  }

  return throwError(() => error);
}

/** Check if URL is an API call (matches environment.apiUrl) */
function isApiCall(req: HttpRequest<unknown>, apiBaseUrl: string): boolean {
  // Simple and robust: prefix match for absolute API URLs
  const base = apiBaseUrl.endsWith('/') ? apiBaseUrl : apiBaseUrl + '/';
  return req.url.startsWith(base);
}

/** Add Authorization: Bearer <token> header */
function addAuthHeader<T>(req: HttpRequest<T>, token: string | null): HttpRequest<T> {
  if (!token?.trim()) return req;
  const newAuth = `Bearer ${token}`;
  return req.headers.get('Authorization') === newAuth
    ? req
    : req.clone({ setHeaders: { Authorization: newAuth } });
}

