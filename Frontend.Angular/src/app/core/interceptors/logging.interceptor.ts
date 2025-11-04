import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
  HttpResponse
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { LoggerService } from '../services/logger.service';

import { environment } from '../../environments/environment';

/**
 * HTTP Logging Interceptor
 * ---------------------------------------------------------------------------
 * • Development: logs full requests/responses (method, URL, body, response, headers).
 * • Production: logs only slow requests, write operations, or errors.
 * • Integrated with LoggerService and correlation IDs.
 */
export const loggingInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  const logger = inject(LoggerService);
  const startTime = performance.now(); // 🟢 precise timing

  // Skip noise (health checks, telemetry, etc.)
  if (shouldSkipLogging(req.url)) return next(req);

  // 🟢 Log outgoing request (only in development)
  if (!environment.production && environment.logPolicy.enableRequestLogging) {
    logger.debug(`→ ${req.method} ${req.urlWithParams}`, {
      method: req.method,
      url: req.urlWithParams,
      body: safeJson(req.body),
      headers: extractHeaders(req.headers)
    });
  }

  return next(req).pipe(
    tap({
      next: (event) => {
        if (event instanceof HttpResponse) {
          const duration = performance.now() - startTime;
          handleSuccess(logger, req, event, duration);
        }
      },
      error: (error: HttpErrorResponse) => {
        const duration = performance.now() - startTime;
        handleError(logger, req, error, duration);
      }
    })
  );
};

// ────────────────────────────────────────────────────────────────
// Helpers
// ────────────────────────────────────────────────────────────────

function shouldSkipLogging(url: string): boolean {
  const skipPatterns = environment.skipLoggingPatterns ?? [];
  return skipPatterns.some(fragment => url.includes(fragment));
}

function handleSuccess(
  logger: LoggerService,
  req: HttpRequest<unknown>,
  res: HttpResponse<unknown>,
  duration: number
): void {
  const policy = environment.logPolicy;
  const slowThreshold = policy.slowThresholdMs ?? 2000;

  const context = {
    method: req.method,
    url: req.urlWithParams,
    status: res.status,
    duration,
    body: safeJson(req.body),
    response: safeJson(res.body),
    headers: extractHeaders(res.headers)
  };

  if (environment.production) {
    if (duration > slowThreshold) {
      logger.warn(`🐢 Slow request: ${req.method} ${req.urlWithParams}`, context);
    } else if (isWriteMethod(req.method)) {
      logger.info(`✅ ${req.method} ${req.urlWithParams}`, context);
    }
  } else {
    if (!policy.enableResponseLogging) return;

    const isSlow = duration > slowThreshold / 2;
    const level = isSlow ? 'warn' : 'debug';
    const emoji = isSlow ? '🐢' : '✅';
    (logger as any)[level](
      `${emoji} ← ${res.status} ${req.method} ${req.urlWithParams}`,
      context
    );
  }
}

function handleError(
  logger: LoggerService,
  req: HttpRequest<unknown>,
  error: HttpErrorResponse,
  duration: number
): void {
  const context = {
    method: req.method,
    url: req.urlWithParams,
    status: error.status,
    duration,
    body: safeJson(req.body),
    response: safeJson(error.error),
    headers: extractHeaders(error.headers)
  };

  if (error.status === 0) {
    logger.error(`🌐 Network error: ${req.method} ${req.urlWithParams}`, error, context);
  } else if (error.status >= 500) {
    logger.error(`💥 Server error (${error.status})`, error, context);
  } else if (error.status === 401 || error.status === 403) {
    logger.warn(`🔒 Authorization error (${error.status})`, context);
  } else if (error.status >= 400) {
    logger.info(`⚠️ Client error (${error.status})`, context);
  } else {
    logger.error(`❗ Unexpected error`, error, context);
  }
}

function isWriteMethod(method: string): boolean {
  return ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method);
}

// ────────────────────────────────────────────────────────────────
// Safe JSON + header extraction utilities
// ────────────────────────────────────────────────────────────────

function extractHeaders(headers: any): Record<string, string> {
  try {
    const result: Record<string, string> = {};
    for (const key of headers.keys()) {
      result[key] = headers.get(key) ?? '';
    }
    return result;
  } catch {
    return {};
  }
}

function safeJson(value: any): any {
  try {
    if (value == null) return null;
    const json = JSON.stringify(value);
    return json.length > 10000 ? '[TRUNCATED LARGE PAYLOAD]' : value;
  } catch {
    return '[Unserializable]';
  }
}
