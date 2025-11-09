// core/http/interceptors/trace-context.interceptor.ts
/**
 * Trace Context Interceptor - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Uses TraceService (merged service)
 */

import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TraceService } from '../../logging/services/trace.service';

export const traceContextInterceptor: HttpInterceptorFn = (req, next) => {
  const traceService = inject(TraceService);
  const context = traceService.getCurrentContext();

  const retryAttempt = req.headers.get('X-Retry-Attempt');
  const attempt = retryAttempt ? parseInt(retryAttempt, 10) : undefined;

  // Generate W3C traceparent header
  const version = '00';
  const traceId = context.traceId;
  const spanId = context.activeSpan?.spanId || traceService.createSpan('http-request').spanId;
  const flags = '01';
  const traceparent = `${version}-${traceId}-${spanId}-${flags}`;

  // Generate tracestate if retry
  const tracestate = attempt !== undefined ? `avancira=retry:${attempt}` : null;

  let headers = req.headers.set('traceparent', traceparent);
  if (tracestate) {
    headers = headers.set('tracestate', tracestate);
  }

  return next(req.clone({ headers }));
};