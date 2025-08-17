import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { from,Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);

export function authInterceptor(req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
  const auth = inject(AuthService);
  const skip = req.context.get(SKIP_AUTH);

  const toApi = isApi(req, environment.apiUrl);
  if (skip || !toApi) return next(req.clone({ withCredentials: true }));

  // PRE-WAIT: avoid sending requests without a token on reload
  return from(auth.ensureAccessToken()).pipe(
    switchMap(() => {
      const token = auth.getAccessToken();
      const deviceId = auth.getDeviceIdForHeader();
      return next(withAuth(req, token, deviceId));
    }),
    // Fallback: single retry on 401 to handle rare races
    catchError(err => {
      const retried = req.context.get(ALREADY_RETRIED);
      if (!retried && err instanceof HttpErrorResponse && err.status === 401) {
        return auth.refreshAccessToken().pipe(
          switchMap(t => next(
            withAuth(req.clone({ context: req.context.set(ALREADY_RETRIED, true) }), t, auth.getDeviceIdForHeader())
          )),
          catchError(e => { auth.logout(); return throwError(() => e); })
        );
      }
      return throwError(() => err);
    })
  );
}

function isApi(req: HttpRequest<any>, apiBase: string): boolean {
  const requestUrl = new URL(req.url, window.location.origin);
  const baseUrl = new URL(apiBase, window.location.origin);
  return requestUrl.origin === baseUrl.origin && requestUrl.pathname.startsWith(baseUrl.pathname);
}

function withAuth(req: HttpRequest<any>, token: string | null, deviceId: string | null): HttpRequest<any> {
  const setHeaders: Record<string, string> = {};
  if (token) setHeaders['Authorization'] = `Bearer ${token}`;
  if (deviceId) setHeaders['Device-Id'] = deviceId;
  return Object.keys(setHeaders).length
    ? req.clone({ setHeaders, withCredentials: true })
    : req.clone({ withCredentials: true });
}
