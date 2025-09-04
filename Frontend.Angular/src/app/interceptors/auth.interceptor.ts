// src/app/interceptors/auth.interceptor.ts
import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

/**
 * Bypass auth mechanics entirely for this request (no pre-wait, no Authorization, no 401 retry).
 * Use for /connect/token or other login endpoints to avoid recursion.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/** Does this request need the Authorization header? Default: true (protected API). */
export const REQUIRES_AUTH = new HttpContextToken<boolean>(() => true);

/**
 * Should this request send cookies (withCredentials)?
 * Default: false. For refresh, set to true on that request only.
 */
export const INCLUDE_CREDENTIALS = new HttpContextToken<boolean | undefined>(() => undefined);

/** Internal guard to prevent infinite 401 retry loops */
const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const auth = inject(AuthService);

  const toApi = isApi(req, environment.apiUrl);
  const skip = req.context.get(SKIP_AUTH);
  const needsAuth = req.context.get(REQUIRES_AUTH); // default true
  let withCreds = req.context.get(INCLUDE_CREDENTIALS) ?? false; // default false

  // 🔑 Force cookies for refresh token requests
  if (isRefreshRequest(req)) {
    withCreds = true;
  }

  // Apply cookie policy once
  const baseReq = setWithCreds(req, withCreds);

  // Non-API, or explicitly skipped, or anonymous endpoint → no Authorization
  if (!toApi || skip || !needsAuth) {
    return next(baseReq);
  }

  // Pre-wait: ensure we have a valid access token (de-duped inside AuthService)
  return auth.getValidAccessToken().pipe(
    switchMap(token => next(applyAuth(baseReq, token))),
    catchError(err => {
      const retried = req.context.get(ALREADY_RETRIED);
      const is401 = err instanceof HttpErrorResponse && err.status === 401;

      // Single safe retry on 401 (handles expiry race between pre-wait and server-side check)
      if (!retried && is401) {
        return auth.getValidAccessToken().pipe(
          switchMap(t =>
            next(
              applyAuth(
                baseReq.clone({ context: baseReq.context.set(ALREADY_RETRIED, true) }),
                t
              )
            )
          ),
          catchError(e => throwError(() => e))
        );
      }

      return throwError(() => err);
    })
  );
};

/* -------------------- helpers -------------------- */

function isApi(req: HttpRequest<unknown>, apiBase: string): boolean {
  const u = new URL(req.url, window.location.origin);
  const b = new URL(apiBase, window.location.origin);
  const basePath = b.pathname.endsWith('/') ? b.pathname : b.pathname + '/';
  return u.origin === b.origin && u.pathname.startsWith(basePath);
}

// 🔑 Detect refresh requests
function isRefreshRequest(req: HttpRequest<any>): boolean {
  return req.url.endsWith('/connect/token') && req.body?.grant_type === 'refresh_token';
}

function setWithCreds<T>(req: HttpRequest<T>, withCreds: boolean): HttpRequest<T> {
  return req.withCredentials === withCreds ? req : req.clone({ withCredentials: withCreds });
}

function applyAuth<T>(req: HttpRequest<T>, token: string | null): HttpRequest<T> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
