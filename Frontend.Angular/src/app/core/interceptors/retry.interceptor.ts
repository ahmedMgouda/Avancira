import {
    HttpEvent,
    HttpHandlerFn,
    HttpInterceptorFn,
    HttpRequest
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';

import { LoggerService } from '../services/logger.service';
import { ResilienceService } from '../services/resilience.service';

import { environment } from '@/environments/environment';

/**
 * Retry Interceptor
 * ---------------------------------------------------------------------------
 * Automatically retries failed safe (idempotent) operations
 * Uses exponential backoff with jitter via ResilienceService
 * Can be customized via HTTP headers:
 *   - X-Skip-Retry: disables retry for this request
 *   - X-Allow-Retry: forces retry even for POST/PUT requests
 */
export const retryInterceptor: HttpInterceptorFn = (
    req: HttpRequest<unknown>,
    next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
    const resilience = inject(ResilienceService);
    const logger = inject(LoggerService);

    // ------------------------------------------------------------------------
    // Determine whether retry should apply
    // ------------------------------------------------------------------------
    const isSafeMethod = ['GET', 'HEAD', 'OPTIONS'].includes(req.method);
    const skipRetry = req.headers.has('X-Skip-Retry');
    const allowRetry = req.headers.has('X-Allow-Retry');

    if (skipRetry) {
        logger.debug(`Retry skipped for: ${req.method} ${req.url}`);
        return next(req);
    }

    // Only retry idempotent methods unless explicitly allowed
    if (!isSafeMethod && !allowRetry) {
        return next(req);
    }

    // ------------------------------------------------------------------------
    // Configure and apply retry operator
    // ------------------------------------------------------------------------
    const policy = environment.retryPolicy;

    const retryConfig = {
        maxRetries: policy.maxRetries,
        scalingDuration: policy.baseDelayMs,
        excludedStatusCodes: policy.excluded,
        maxDelay: policy.maxDelay
    };

    logger.debug(`Retry enabled for: ${req.method} ${req.url}`, {
        maxRetries: retryConfig.maxRetries
    });

    return next(req).pipe(resilience.withRetry(retryConfig));
};
