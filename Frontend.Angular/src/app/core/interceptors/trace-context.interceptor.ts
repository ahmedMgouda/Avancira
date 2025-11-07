import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TraceContextService } from '../services/trace-context.service';

/**
 * Adds W3C trace context headers to all outgoing HTTP requests
 */
export const traceContextInterceptor: HttpInterceptorFn = (req, next) => {
  const traceContext = inject(TraceContextService);
  const context = traceContext.getCurrentContext();

  const retryAttempt = req.headers.get('X-Retry-Attempt');
  const attempt = retryAttempt ? parseInt(retryAttempt, 10) : undefined;

  const traceparent = traceContext.generateTraceparent(context);
  const tracestate = traceContext.generateTracestate(attempt);

  let headers = req.headers.set('traceparent', traceparent);
  if (tracestate) headers = headers.set('tracestate', tracestate);

  return next(req.clone({ headers }));
};
