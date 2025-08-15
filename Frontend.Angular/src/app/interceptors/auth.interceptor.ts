import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService, TokenResponse } from '../services/auth.service';

export function authInterceptor(
  req: HttpRequest<any>,
  next: HttpHandlerFn
): Observable<HttpEvent<any>> {
  const authService = inject(AuthService);

  const token = authService.getAccessToken();
  const deviceId = getDeviceId(authService);
  const authReq = addHeaders(req, token, deviceId);

  return next(authReq).pipe(
    catchError((error) => {
      // Only handle 401 for non-auth endpoints
      if (error instanceof HttpErrorResponse && error.status === 401) {
        if (req.url.includes('/auth/token') || req.url.includes('/auth/refresh')) {
          return throwError(() => error);
        }
        return handle401Error(authReq, next, authService);
      }
      return throwError(() => error);
    })
  );
}

function addHeaders(
  request: HttpRequest<any>,
  token: string | null,
  deviceId: string | null
): HttpRequest<any> {
  const headers: Record<string, string> = {};
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  if (deviceId) {
    headers['Device-Id'] = deviceId;
  }
  return Object.keys(headers).length ? request.clone({ setHeaders: headers }) : request;
}

function getDeviceId(authService: AuthService): string | null {
  const stored = localStorage.getItem('deviceId');
  if (stored) {
    return stored;
  }
  if (!authService.getAccessToken()) {
    const newId = crypto.randomUUID();
    localStorage.setItem('deviceId', newId);
    return newId;
  }
  return null;
}

function handle401Error(
  request: HttpRequest<any>,
  next: HttpHandlerFn,
  authService: AuthService
): Observable<HttpEvent<any>> {
  if (!authService.refreshing) {
    authService.beginRefresh();

    return authService.refreshToken().pipe(
      // If you want, you can add a retryWhen here for transient network errors only.
      switchMap((response: TokenResponse) => {
        const newToken = response.token;
        authService.endRefresh(newToken);
        return next(addHeaders(request, newToken, getDeviceId(authService)));
      }),
      catchError((err) => {
        authService.refreshFailed(err);
        authService.logout();
        return throwError(() => err);
      })
    );
  } else {
    // Another request is already refreshing → wait and retry
    return authService.waitForRefresh().pipe(
      switchMap((token) => next(addHeaders(request, token, getDeviceId(authService)))),
      catchError((err) => throwError(() => err))
    );
  }
}
