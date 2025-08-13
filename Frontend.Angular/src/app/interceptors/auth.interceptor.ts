import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { catchError, filter, retry,switchMap, take } from 'rxjs/operators';

import { AuthService, TokenResponse } from '../services/auth.service';

let isRefreshing = false;
const refreshTokenSubject = new BehaviorSubject<string | null>(null);

export function authInterceptor(req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
  const authService = inject(AuthService);

  const token = authService.getAccessToken();
  const authReq = token ? addToken(req, token) : req;

  return next(authReq).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
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
  if (!isRefreshing) {
    isRefreshing = true; // set immediately to prevent race conditions
    refreshTokenSubject.next(null);

    return authService.refreshToken().pipe(
      retry(1), // retry once on network errors
      switchMap((response: TokenResponse) => {
        const newToken = response.token;
        isRefreshing = false;
        refreshTokenSubject.next(newToken);
        return next(addToken(request, newToken));
      }),
      catchError(err => {
        isRefreshing = false;
        refreshTokenSubject.next(null); // clear subject so waiting requests don't proceed
        authService.logout();
        return throwError(() => err);
      })
    );

  } else {
    // Wait until refresh completes and retry original request
    return refreshTokenSubject.pipe(
      filter((token): token is string => token !== null),
      take(1),
      switchMap(token => next(addToken(request, token)))
    );
  }
}
