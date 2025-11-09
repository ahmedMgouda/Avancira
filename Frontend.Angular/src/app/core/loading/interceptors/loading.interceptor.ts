import {
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, Observable, throwError } from 'rxjs';

import { LoadingService } from '../services/loading.service';

import { IdGenerator } from '../../utils/id-generator.utility';


export const loadingInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const loader = inject(LoadingService);

  // ──────────────────────────────────────────────────────────────
  // 1️⃣ Skip loading for requests marked with X-Skip-Loading
  // ──────────────────────────────────────────────────────────────
  if (req.headers.has('X-Skip-Loading')) {
    return next(req);
  }

  // ──────────────────────────────────────────────────────────────
  // 2️⃣ Generate unique request ID for tracking
  //    Used by LoadingService to track individual requests
  // ──────────────────────────────────────────────────────────────
  const requestId = IdGenerator.generateRequestId();

  // ──────────────────────────────────────────────────────────────
  // 3️⃣ Notify LoadingService (start tracking)
  // ──────────────────────────────────────────────────────────────
  loader.startRequest(requestId, {
    method: req.method,
    url: req.urlWithParams,
  });

  // Track if we've already completed to prevent duplicate calls
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
      // Only complete if not already done in error handler
      if (!completed) {
        completed = true;
        loader.completeRequest(requestId);
      }
    })
  );
};