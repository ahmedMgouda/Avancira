import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TraceContextService } from '../services/trace-context.service';

/**
 * Adds W3C Trace Context headers to all HTTP requests
 * Supports retry attempt tracking via tracestate
 */
export const traceContextInterceptor: HttpInterceptorFn = (req, next) => {
  const traceContext = inject(TraceContextService);

  // Get current trace context
  const context = traceContext.getCurrentContext();
  
  // Check if this is a retry (from custom header)
  const retryAttempt = req.headers.get('X-Retry-Attempt');
  const attempt = retryAttempt ? parseInt(retryAttempt, 10) : undefined;

  // Generate headers
  const traceparent = traceContext.generateTraceparent(context);
  const tracestate = traceContext.generateTracestate(attempt);

  // Add headers to request
  let headers = req.headers.set('traceparent', traceparent);
  
  if (tracestate) {
    headers = headers.set('tracestate', tracestate);
  }

  return next(req.clone({ headers }));
};