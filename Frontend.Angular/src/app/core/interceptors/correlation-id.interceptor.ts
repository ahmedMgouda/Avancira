import {
  HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { LoggerService } from '../services/logger.service';

import { environment } from '../../environments/environment';

export const CORRELATION_HEADER = 'X-Correlation-ID';

/**
 * Correlation ID Interceptor
 * ---------------------------------------------------------------------------
 * Adds X-Correlation-ID header to all requests (unless disabled via env flag)
 * Keeps correlation synced with LoggerService for consistent tracing
 */
export const correlationIdInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const logger = inject(LoggerService);

  // Skip entirely if disabled in environment
  if (environment.disableCorrelation) {
    return next(req);
  }

  const existingId = req.headers.get(CORRELATION_HEADER);
  const correlationId = existingId || generateCorrelationId();

  logger.setCorrelationId(correlationId);

  const requestWithId = req.clone({
    setHeaders: { [CORRELATION_HEADER]: correlationId }
  });

  return next(requestWithId).pipe(finalize(() => logger.clearCorrelationId()));
};

function generateCorrelationId(): string {
  try {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }
  } catch {}
  const random = Math.random().toString(36).substring(2, 10);
  return `${Date.now()}-${random}`;
}
