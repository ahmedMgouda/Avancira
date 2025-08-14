import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, retry, switchMap } from 'rxjs/operators';

import { AuthService, TokenResponse } from '../services/auth.service';

export function authInterceptor(req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
  const authService = inject(AuthService);

  const token = authService.getAccessToken();
  const authReq = token ? addToken(req, token) : req;

  return next(authReq).pipe(
    catchError(error => {
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

function addToken(request: HttpRequest<any>, token: string): HttpRequest<any> {
  return request.clone({
    setHeaders: { Authorization: `Bearer ${token}` }
  });
}

function handle401Error(
  request: HttpRequest<any>,
  next: HttpHandlerFn,
  authService: AuthService
): Observable<HttpEvent<any>> {
  if (!authService.refreshing) {
    authService.beginRefresh();

    return authService.refreshToken().pipe(
      retry(1), // retry once on network errors
      switchMap((response: TokenResponse) => {
        const newToken = response.token;
        authService.endRefresh(newToken);
        return next(addToken(request, newToken));
      }),
      catchError(err => {
        authService.refreshFailed();
        authService.logout();
        return throwError(() => err);
      })
    );

  } else {
    // Wait until refresh completes and retry original request
    return authService.waitForRefresh().pipe(
      switchMap(token => next(addToken(request, token)))
    );
  }
}
