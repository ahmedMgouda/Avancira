import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs/operators';

import { LoggerService } from '../services/logger.service';
import { SpanManagerService } from '../services/span-manager.service';

/**
 * HTTP logging interceptor with skip logic
 * Respects X-Skip-Logging header
 */
export const httpLoggingInterceptor: HttpInterceptorFn = (req, next) => {
  // Check if logging should be skipped
  if (req.headers.has('X-Skip-Logging')) {
    return next(req);
  }

  const logger = inject(LoggerService);
  const spanManager = inject(SpanManagerService);

  const startTime = Date.now();
  const span = spanManager.createSpan(req.url);

  return next(req).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          const duration = Date.now() - startTime;
          spanManager.endSpan(span.spanId);

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
      },
      error: (error) => {
        spanManager.endSpan(span.spanId, { error });
      }
    })
  );
};