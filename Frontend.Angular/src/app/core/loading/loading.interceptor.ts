import {
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, Observable, throwError } from 'rxjs';

import { LoadingService } from './loading.service';

import { BrowserCompat } from '../logging/utils/browser-compat.util';

export const loadingInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const loader = inject(LoadingService);

  // ──────────────────────────────────────────────────────────────
  // 1️⃣ Skip loading for requests marked with X-Skip-Loading
  // ──────────────────────────────────────────────────────────────
  if (req.headers.has('X-Skip-Loading')) {
    const cleanReq = req.clone({
      headers: req.headers.delete('X-Skip-Loading'),
    });
    return next(cleanReq);
  }

  // ──────────────────────────────────────────────────────────────
  // 2️⃣ Get correlation ID (from correlation interceptor)
  //    Fallback to random UUID only if missing
  // ──────────────────────────────────────────────────────────────
  const requestId = BrowserCompat.generateUUID();

  // ──────────────────────────────────────────────────────────────
  // 3️⃣ Notify LoadingService (start tracking)
  // ──────────────────────────────────────────────────────────────
  loader.startRequest(requestId, {
    method: req.method,
    url: req.urlWithParams,
  });

  // ✅ FIX: Track if we've already completed to prevent duplicate calls
  let completed = false;

  // ──────────────────────────────────────────────────────────────
  // 4️⃣ Handle request with proper error handling
  // ──────────────────────────────────────────────────────────────
  return next(req).pipe(
    catchError(error => {
      // Mark as completed and notify loader with error
      if (!completed) {
        completed = true;
        loader.completeRequest(requestId, error);
      }
      return throwError(() => error);
    }),
    finalize(() => {
      // ✅ FIX: Only complete if not already done in error handler
      if (!completed) {
        completed = true;
        loader.completeRequest(requestId);
      }
    })
  );
};