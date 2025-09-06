import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpStatusCode
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

/**
 * Bypass auth entirely for this request (no token acquisition, no Authorization header).
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/**
 * Does this request need Authorization?
 * Default: true for API requests, false otherwise.
 */
export const REQUIRES_AUTH = new HttpContextToken<boolean | undefined>(() => undefined);

/**
 * Should this request send cookies (withCredentials)?
 * Default: false. For refresh-token requests set to true.
 */
export const INCLUDE_CREDENTIALS = new HttpContextToken<boolean>(() => false);

/** Prevent infinite retry loops */
const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  const isApiRequest = isApiCall(req, environment.apiUrl);
  const skipAuth = req.context.get(SKIP_AUTH);
  const requiresAuth = req.context.get(REQUIRES_AUTH) ?? isApiRequest;
  const withCreds = req.context.get(INCLUDE_CREDENTIALS) || isRefreshTokenRequest(req);

  const baseReq = applyCredentials(req, withCreds);

  // No auth needed → forward request
  if (!isApiRequest || skipAuth || !requiresAuth) {
    return next(baseReq);
  }

  // Acquire token from AuthService (handles refresh internally)
  return authService.getValidAccessToken().pipe(
    switchMap(token => next(addAuthHeader(baseReq, token))),
    catchError(error => handleAuthError(error, baseReq, next, authService))
  );
};

/**
 * Handle errors with one safe retry on 401.
 * Does not trigger refresh again (AuthService already did that).
 */
function handleAuthError(
  error: unknown,
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  authService: AuthService
): Observable<HttpEvent<unknown>> {
  const alreadyRetried = req.context.get(ALREADY_RETRIED);
  const is401 = error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized;
  const is403 = error instanceof HttpErrorResponse && error.status === HttpStatusCode.Forbidden;

  if (is401 && !alreadyRetried) {
    console.info('🔄 Retrying once after 401 with current token');

    return authService.getAccessToken() // just reuse latest token, no new refresh
      ? next(
          addAuthHeader(
            req.clone({ context: req.context.set(ALREADY_RETRIED, true) }),
            authService.getAccessToken()
          )
        )
      : throwError(() => error);
  }

  if (is401) {
    console.warn('⚠️ Unauthorized request - token invalid or expired', { url: req.url });
  } else if (is403) {
    console.warn('⚠️ Forbidden request - insufficient permissions', { url: req.url });
  }

  return throwError(() => error);
}

/** Detect API calls */
function isApiCall(req: HttpRequest<unknown>, apiBaseUrl: string): boolean {
  try {
    const requestUrl = new URL(req.url, window.location.origin);
    const apiUrl = new URL(apiBaseUrl, window.location.origin);
    const apiPath = apiUrl.pathname.endsWith('/') ? apiUrl.pathname : apiUrl.pathname + '/';
    return requestUrl.origin === apiUrl.origin && requestUrl.pathname.startsWith(apiPath);
  } catch {
    return false;
  }
}

/** Detect refresh token requests */
function isRefreshTokenRequest(req: HttpRequest<unknown>): boolean {
  return req.url.includes('/connect/token') &&
         typeof req.body === 'object' &&
         req.body !== null &&
         (req.body as any)['grant_type'] === 'refresh_token';
}

/** Clone with credentials */
function applyCredentials<T>(req: HttpRequest<T>, withCredentials: boolean): HttpRequest<T> {
  return req.withCredentials !== withCredentials ? req.clone({ withCredentials }) : req;
}

/** Add Authorization header */
function addAuthHeader<T>(req: HttpRequest<T>, token: string | null): HttpRequest<T> {
  if (!token?.trim()) return req;
  const newAuth = `Bearer ${token}`;
  return req.headers.get('Authorization') === newAuth
    ? req
    : req.clone({ setHeaders: { Authorization: newAuth } });
}
