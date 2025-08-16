import {
  HttpContextToken,
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { environment } from '../environments/environment';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
const ALREADY_RETRIED = new HttpContextToken<boolean>(() => false);

export function authInterceptor(req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> {
  const auth = inject(AuthService);
  const skip = req.context.get(SKIP_AUTH);
  const retried = req.context.get(ALREADY_RETRIED);

  const toApi = isApi(req.url, environment.apiUrl);
  const token = (!skip && toApi) ? auth.getAccessToken() : null;
  const deviceId = toApi ? auth.getDeviceIdForHeader() : null;

  return next(withAuth(req, token, deviceId, toApi)).pipe(
    catchError(err => {
      if (!skip && toApi && !retried && err instanceof HttpErrorResponse && err.status === 401) {
        return auth.refreshAccessToken().pipe(
          switchMap(t => next(
            withAuth(req.clone({ context: req.context.set(ALREADY_RETRIED, true) }), t, auth.getDeviceIdForHeader(), toApi)
          )),
          catchError(e => { auth.logout(); return throwError(() => e); })
        );
      }
      return throwError(() => err);
    })
  );
}

function isApi(url: string, apiBase: string): boolean {
  // Adjust if your API path differs
  return url.startsWith(apiBase) || url.startsWith('/api/');
}

function withAuth(req: HttpRequest<any>, token: string | null, deviceId: string | null, toApi: boolean): HttpRequest<any> {
  if (!toApi) return req; // never attach tokens to 3rd-party origins
  const setHeaders: Record<string, string> = {};
  if (token) setHeaders['Authorization'] = `Bearer ${token}`;
  if (deviceId) setHeaders['Device-Id'] = deviceId; // persists across sessions
  return Object.keys(setHeaders).length
    ? req.clone({ setHeaders, withCredentials: true })
    : req.clone({ withCredentials: true });
}
