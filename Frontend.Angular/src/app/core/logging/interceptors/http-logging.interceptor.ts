// core/logging/interceptors/http-logging.interceptor.ts
/**
 * HTTP Logging Interceptor - MERGED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Merged http-logging.interceptor + http-error.interceptor
 * ✅ Single interceptor for all HTTP logging
 * ✅ Uses TraceService (merged service)
 * ✅ Logs both success and errors
 */

import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, tap, throwError } from 'rxjs';

import { LoggerService } from '../services/logger.service';
import { TraceService } from '../services/trace.service';

export const httpLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has('X-Skip-Logging')) {
    return next(req);
  }

  const logger = inject(LoggerService);
  const traceService = inject(TraceService);

  const startTime = Date.now();
  const span = traceService.createSpan(req.url);

  return next(req).pipe(
    tap((event) => {
      if (event instanceof HttpResponse) {
        const duration = Date.now() - startTime;
        traceService.endSpan(span.spanId);

        logger.info(`${event.status} ${req.method} ${req.url}`, {
          log: {
            source: 'HTTP',
            type: 'http'
          },
          http: {
            method: req.method,
            url: req.url,
            status_code: event.status,
            duration_ms: duration
          }
        });
      }
    }),

    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        const duration = Date.now() - startTime;
        traceService.endSpan(span.spanId, { error: error as Error });

        // Mark as logged to prevent GlobalErrorHandler duplication
        (error as any).__logged = true;

        logger.error(`${error.status} ${req.method} ${req.url}`, error, {
          log: {
            source: 'HTTP',
            type: 'http'
          },
          http: {
            method: req.method,
            url: req.url,
            status_code: error.status,
            duration_ms: duration,
            error_message: error.message
          }
        });
      }

      return throwError(() => error);
    })
  );
};