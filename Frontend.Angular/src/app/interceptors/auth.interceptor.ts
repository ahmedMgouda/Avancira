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

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const authService = inject(AuthService);

  // Send credentials only for BFF/API (same-origin)
  const shouldIncludeCreds = isSameOriginBffOrApi(req.url);
  const request = shouldIncludeCreds ? req.clone({ withCredentials: true }) : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && shouldIncludeCreds) {
        authService.handleUnauthorized();
      }
      return throwError(() => error);
    })
  );
};

/**
 * Checks whether a request targets the same-origin `/bff` or `/api` endpoints.
 */
function isSameOriginBffOrApi(url: string): boolean {
  try {
    const base = new URL(window.location.origin);
    const parsed = new URL(url, base);
    if (parsed.origin !== base.origin) return false;
    const path = parsed.pathname;
    return path.startsWith('/bff') || path.startsWith('/api');
  } catch {
    return url.startsWith('/bff') || url.startsWith('/api');
  }
}
