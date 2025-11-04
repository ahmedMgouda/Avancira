import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, Injectable, Injector, NgZone } from '@angular/core';
import { Router } from '@angular/router';

import { ErrorHandlerService } from '../services/error-handler.service';
import { LoggerService } from '../services/logger.service';
import { NotificationService } from '../services/notification.service';

import { AppError } from '../models/error.model';

import { environment } from '@/environments/environment';

/**
 * Global Error Handler
 * ─────────────────────────────────────────────────────────────────────────
 * Catches all unhandled errors in the Angular application:
 *   - Synchronous JavaScript errors (TypeError, ReferenceError, etc.)
 *   - Unhandled promise rejections
 *   - Angular-specific errors (change detection, template errors)
 *   - Third-party library errors
 * 
 * Integration with existing infrastructure:
 *   - Uses ErrorHandlerService for normalization to AppError
 *   - Uses LoggerService for structured logging
 *   - Uses NotificationService for user feedback (conditional)
 *   - Respects correlation ID flow
 * 
 * Does NOT handle:
 *   - HTTP errors (handled by ErrorInterceptor)
 *   - Errors already transformed to AppError by interceptors
 * 
 * Design Principles:
 *   - Fail-safe: Never throw from error handler
 *   - Non-blocking: Runs outside Angular zone
 *   - Defensive: Multiple fallback layers
 *   - Observable: All errors logged with correlation
 * 
 * @see https://angular.io/api/core/ErrorHandler
 * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/error_event
 */
@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  // ─────────────────────────────────────────────────────────────────────
  // Lazy Injection (prevents circular dependencies)
  // ─────────────────────────────────────────────────────────────────────
  
  private get errorHandler(): ErrorHandlerService {
    return this.injector.get(ErrorHandlerService);
  }
  
  private get logger(): LoggerService {
    return this.injector.get(LoggerService);
  }
  
  private get notification(): NotificationService {
    return this.injector.get(NotificationService);
  }
  
  private get router(): Router {
    return this.injector.get(Router);
  }

  // ─────────────────────────────────────────────────────────────────────
  // State Management
  // ─────────────────────────────────────────────────────────────────────
  
  private errorCount = 0;
  private readonly errorThreshold = 10; // Circuit breaker threshold
  private readonly errorWindowMs = 60000; // 1 minute window
  private errorWindowStart = Date.now();

  constructor(
    private readonly injector: Injector,
    private readonly zone: NgZone
  ) {
    this.initializeGlobalListeners();
    this.logHandlerInitialization();
  }

  // ═════════════════════════════════════════════════════════════════════
  // Main Error Handler Entry Point
  // ═════════════════════════════════════════════════════════════════════

  /**
   * Main error handling entry point called by Angular
   * Processes ALL unhandled errors in the application
   * 
   * @param error - Any error that wasn't caught by application code
   */
  handleError(error: unknown): void {
    // Run outside Angular zone to prevent change detection issues
    // and avoid infinite error loops
    this.zone.runOutsideAngular(() => {
      try {
        // Circuit breaker: prevent error storms
        if (this.shouldSuppressError()) {
          this.logSuppressedError(error);
          return;
        }

        this.processError(error);
      } catch (handlerError) {
        // Last resort fallback: if error handler itself fails
        this.handleHandlerFailure(handlerError, error);
      }
    });
  }

  // ═════════════════════════════════════════════════════════════════════
  // Error Processing Pipeline
  // ═════════════════════════════════════════════════════════════════════

  /**
   * Main error processing pipeline
   * 
   * Flow:
   *   1. Classify error type
   *   2. Transform to AppError (via ErrorHandlerService)
   *   3. Log with correlation
   *   4. Notify user (conditional)
   *   5. Handle critical scenarios
   */
  private processError(error: unknown): void {
    // Step 1: Classify the error
    const errorType = this.classifyError(error);
    
    // Skip if already handled by interceptor
    if (errorType === 'already-handled') {
      this.logger.debug('Error already handled by interceptor, skipping', {
        error: this.safeStringify(error)
      });
      return;
    }

    // Step 2: Extract or create correlation ID
    const correlationId = this.ensureCorrelationId();

    // Step 3: Transform to normalized AppError
    const appError = this.transformError(error, errorType, correlationId);

    // Step 4: Log with full context
    this.logError(appError, error, errorType);

    // Step 5: Notify user (conditional based on environment and error type)
    this.notifyUser(appError, errorType);

    // Step 6: Handle critical errors (optional recovery strategies)
    if (appError.severity === 'critical') {
      this.handleCriticalError(appError);
    }

    // Step 7: Track error count for circuit breaker
    this.trackError();
  }

  // ─────────────────────────────────────────────────────────────────────
  // Error Classification
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Classify error into specific type for appropriate handling
   * 
   * Classification hierarchy:
   *   1. Already-handled (AppError from interceptor)
   *   2. HTTP errors (shouldn't reach here, but defensive)
   *   3. Angular-specific errors
   *   4. JavaScript errors
   *   5. Promise rejection events
   *   6. Unknown
   */
  private classifyError(error: unknown): ErrorType {
    // Type 1: Already processed by HTTP interceptor
    if (this.errorHandler.isAppError(error)) {
      return 'already-handled';
    }

    // Type 2: HTTP errors (defensive - should be caught by interceptor)
    if (this.isHttpError(error)) {
      this.logger.warn('HTTP error reached global handler (unexpected)', {
        status: (error as any).status,
        url: (error as any).url
      });
      return 'http';
    }

    // Type 3: Promise rejection event (from window.unhandledrejection)
    if (this.isPromiseRejectionEvent(error)) {
      return 'promise-rejection';
    }

    // Type 4+5: JavaScript Error object
    if (error instanceof Error) {
      if (this.isAngularError(error)) {
        return 'angular';
      }
      if (this.isChunkLoadError(error)) {
        return 'chunk-load';
      }
      return 'javascript';
    }

    // Type 6: Unknown/unexpected
    return 'unknown';
  }

  /**
   * Transform classified error into normalized AppError
   */
  private transformError(
    error: unknown,
    type: ErrorType,
    correlationId: string
  ): AppError {
    const context = this.gatherErrorContext();

    try {
      switch (type) {
        case 'already-handled':
          // Error already normalized by interceptor
          return error as AppError;

        case 'http':
          // Defensive: HTTP errors should be caught by interceptor
          return this.errorHandler.handleHttpError(
            error as HttpErrorResponse,
            `global-handler:http`
          );

        case 'angular':
          return this.handleAngularError(error as Error, correlationId, context);

        case 'javascript':
          return this.handleJavaScriptError(error as Error, correlationId, context);

        case 'promise-rejection':
          const reason = (error as PromiseRejectionEvent).reason;
          return this.handlePromiseRejection(reason, correlationId, context);

        case 'chunk-load':
          return this.handleChunkLoadError(error as Error, correlationId, context);

        case 'unknown':
        default:
          return this.handleUnknownError(error, correlationId, context);
      }
    } catch (transformError) {
      // Fallback if transformation fails
      this.logger.error('Error transformation failed', transformError);
      return this.createFallbackAppError(error, correlationId);
    }
  }

  // ─────────────────────────────────────────────────────────────────────
  // Specific Error Type Handlers
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Handle Angular-specific errors
   * Examples: ExpressionChangedAfterItHasBeenCheckedError, NG0100, etc.
   */
  private handleAngularError(
    error: Error,
    correlationId: string,
    context: string
  ): AppError {
    const isChangeDetectionError = error.message.includes(
      'ExpressionChangedAfterItHasBeenCheckedError'
    );
    const isTemplateError = 
      error.message.includes('NG0') || 
      error.stack?.includes('.html');
    const isNullInjectorError = error.message.includes('NullInjectorError');

    return {
      message: environment.production
        ? this.getProductionAngularMessage(error)
        : error.message,
      code: this.getAngularErrorCode(error, isChangeDetectionError, !!isTemplateError, isNullInjectorError),
      severity: isChangeDetectionError ? 'warning' : 'error',
      timestamp: new Date(),
      correlationId,
      details: {
        name: error.name,
        stack: environment.production ? undefined : error.stack,
        context,
        url: this.safeWindowUrl(),
        errorType: 'angular',
        isChangeDetectionError,
        isTemplateError,
        isNullInjectorError
      }
    };
  }

  /**
   * Handle standard JavaScript errors
   * Examples: TypeError, ReferenceError, RangeError
   */
  private handleJavaScriptError(
    error: Error,
    correlationId: string,
    context: string
  ): AppError {
    return {
      message: environment.production
        ? 'An unexpected error occurred. Our team has been notified.'
        : error.message,
      code: this.getJavaScriptErrorCode(error),
      severity: this.getJavaScriptErrorSeverity(error),
      timestamp: new Date(),
      correlationId,
      details: {
        name: error.name,
        stack: environment.production ? undefined : error.stack,
        context,
        url: this.safeWindowUrl(),
        errorType: 'javascript',
        browser: navigator.userAgent
      }
    };
  }

  /**
   * Handle unhandled promise rejections
   */
  private handlePromiseRejection(
    reason: unknown,
    correlationId: string,
    context: string
  ): AppError {
    // If reason is already an Error object
    if (reason instanceof Error) {
      return this.handleJavaScriptError(reason, correlationId, `promise:${context}`);
    }

    // If reason is already an AppError (from HTTP)
    if (this.errorHandler.isAppError(reason)) {
      this.logger.debug('Promise rejection with AppError (already handled)');
      return reason;
    }

    // If reason is HTTP error
    if (this.isHttpError(reason)) {
      return this.errorHandler.handleHttpError(
        reason as HttpErrorResponse,
        'promise-rejection'
      );
    }

    // Unknown rejection reason
    return {
      message: environment.production
        ? 'An unexpected error occurred.'
        : `Promise rejected: ${String(reason)}`,
      code: 'PROMISE_REJECTION',
      severity: 'error',
      timestamp: new Date(),
      correlationId,
      details: {
        reason: this.safeStringify(reason),
        context,
        errorType: 'promise-rejection',
        url: this.safeWindowUrl()
      }
    };
  }

  /**
   * Handle lazy-loaded chunk loading failures
   * Common in large Angular apps with code splitting
   */
  private handleChunkLoadError(
    error: Error,
    correlationId: string,
    context: string
  ): AppError {
    return {
      message: environment.production
        ? 'Failed to load application resources. Please refresh the page.'
        : error.message,
      code: 'CHUNK_LOAD_ERROR',
      severity: 'error',
      timestamp: new Date(),
      correlationId,
      details: {
        name: error.name,
        stack: environment.production ? undefined : error.stack,
        context,
        url: this.safeWindowUrl(),
        errorType: 'chunk-load',
        message: error.message
      }
    };
  }

  /**
   * Fallback for completely unknown error types
   */
  private handleUnknownError(
    error: unknown,
    correlationId: string,
    context: string
  ): AppError {
    return {
      message: 'An unexpected error occurred.',
      code: 'UNKNOWN_ERROR',
      severity: 'critical',
      timestamp: new Date(),
      correlationId,
      details: {
        type: typeof error,
        value: this.safeStringify(error),
        context,
        url: this.safeWindowUrl(),
        errorType: 'unknown'
      }
    };
  }

  // ─────────────────────────────────────────────────────────────────────
  // Logging Integration
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Log error with appropriate level and context
   * Ensures all errors are logged in standard format
   */
  private logError(appError: AppError, originalError: unknown, errorType: ErrorType): void {
    const logContext = {
      correlationId: appError.correlationId,
      errorCode: appError.code,
      errorType,
      severity: appError.severity,
      url: this.safeWindowUrl(),
      route: this.safeRouterUrl(),
      timestamp: appError.timestamp.toISOString(),
      userAgent: navigator.userAgent,
      details: appError.details
    };

    // Log based on severity with appropriate emoji indicators
    switch (appError.severity) {
      case 'critical':
        this.logger.fatal(
          `🔥 Critical error: ${appError.message}`,
          originalError,
          logContext
        );
        break;

      case 'error':
        this.logger.error(
          `❌ Error: ${appError.message}`,
          originalError,
          logContext
        );
        break;

      case 'warning':
        this.logger.warn(
          `⚠️ Warning: ${appError.message}`,
          logContext
        );
        break;

      default:
        this.logger.info(
          `ℹ️ Info: ${appError.message}`,
          logContext
        );
    }
  }

  // ─────────────────────────────────────────────────────────────────────
  // User Notification Logic
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Decide whether to show toast notification to user
   * 
   * Policy:
   *   - Development: Show all errors
   *   - Production: Only show user-actionable errors
   *   - Never show for already-handled errors
   *   - Respect environment.disableNotifications flag
   */
  private notifyUser(appError: AppError, errorType: ErrorType): void {
    // Never notify if already handled by interceptor
    if (errorType === 'already-handled') return;

    // Never notify if globally disabled
    if (environment.disableNotifications) return;

    // Production: selective notification
    if (environment.production) {
      const shouldNotify = this.shouldNotifyInProduction(appError, errorType);
      if (shouldNotify) {
        this.zone.run(() => {
          this.notification.fromAppError(appError, { force: true });
        });
      }
      return;
    }

    // Development: notify for all errors (helps debugging)
    this.zone.run(() => {
      this.notification.fromAppError(appError, { force: true });
    });
  }

  /**
   * Determine if error should show toast in production
   */
  private shouldNotifyInProduction(appError: AppError, errorType: ErrorType): boolean {
    // User-actionable errors
    const actionableTypes: ErrorType[] = ['chunk-load'];
    if (actionableTypes.includes(errorType)) return true;

    // User-actionable error codes
    const actionableCodes = [
      'NETWORK_ERROR',
      'CHUNK_LOAD_ERROR',
      'VALIDATION_ERROR',
      'CONFLICT',
      'RATE_LIMIT'
    ];
    if (appError.code && actionableCodes.includes(appError.code)) return true;

    // Critical severity always notifies
    if (appError.severity === 'critical') return true;

    // All other errors: silent (logged but not shown)
    return false;
  }

  // ─────────────────────────────────────────────────────────────────────
  // Critical Error Handling
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Handle critical errors with recovery strategies
   * 
   * Recovery options:
   *   1. Navigate to error page
   *   2. Trigger app reload (last resort)
   *   3. Clear corrupted state
   */
  private handleCriticalError(appError: AppError): void {
    console.error('[GlobalErrorHandler] Critical error detected:', appError);

    // Strategy 1: Navigate to error page (if available)
    if (environment.errorPage?.enabled) {
      this.navigateToErrorPage(appError);
      return;
    }

    // Strategy 2: Trigger reload after delay (configurable)
    if (environment.errorPage?.autoReload) {
      this.scheduleReload(environment.errorPage.reloadDelayMs ?? 5000);
    }
  }

  private navigateToErrorPage(appError: AppError): void {
    try {
      this.zone.run(() => {
        this.router.navigate(['/error'], {
          queryParams: {
            correlationId: appError.correlationId,
            code: appError.code
          },
          skipLocationChange: true
        });
      });
    } catch (navError) {
      this.logger.error('Failed to navigate to error page', navError);
    }
  }

  private scheduleReload(delayMs: number): void {
    console.warn(`[GlobalErrorHandler] Scheduling app reload in ${delayMs}ms`);
    setTimeout(() => {
      window.location.reload();
    }, delayMs);
  }

  // ─────────────────────────────────────────────────────────────────────
  // Circuit Breaker (Error Storm Protection)
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Track error frequency to prevent error storms
   * If too many errors occur in short period, suppress additional errors
   */
  private trackError(): void {
    const now = Date.now();

    // Reset window if expired
    if (now - this.errorWindowStart > this.errorWindowMs) {
      this.errorCount = 0;
      this.errorWindowStart = now;
    }

    this.errorCount++;
  }

  /**
   * Check if error should be suppressed (circuit breaker open)
   */
  private shouldSuppressError(): boolean {
    if (this.errorCount >= this.errorThreshold) {
      if (this.errorCount === this.errorThreshold) {
        // Log once when threshold reached
        console.error(
          `[GlobalErrorHandler] Error threshold reached (${this.errorThreshold} errors in ${this.errorWindowMs}ms). Suppressing additional errors.`
        );
      }
      return true;
    }
    return false;
  }

  private logSuppressedError(error: unknown): void {
    // Log to console only (don't process through full pipeline)
    console.warn('[GlobalErrorHandler] Error suppressed (circuit breaker open):', error);
  }

  // ─────────────────────────────────────────────────────────────────────
  // Global Event Listeners
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Set up global window event listeners
   * Catches errors that don't bubble through Angular
   */
  private initializeGlobalListeners(): void {
    // Unhandled promise rejections
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      console.warn('[GlobalErrorHandler] Unhandled promise rejection', event.reason);
      this.handleError(event);
      event.preventDefault(); // Prevent default console error
    });

    // Resource loading errors (optional, only in development)
    if (!environment.production) {
      window.addEventListener('error', (event: ErrorEvent) => {
        // Distinguish resource errors from script errors
        if (event.target !== window && event.target !== null) {
          const target = event.target as HTMLElement;
          this.logger.warn('Resource load error', {
            src: (target as any).src || (target as any).href,
            tagName: target.tagName,
            type: event.type
          });
        }
      }, true); // Use capture phase
    }
  }

  // ─────────────────────────────────────────────────────────────────────
  // Type Guards
  // ─────────────────────────────────────────────────────────────────────

  private isHttpError(error: unknown): boolean {
    return error instanceof HttpErrorResponse ||
      (!!error && 
       typeof error === 'object' && 
       'status' in error && 
       'url' in error &&
       'statusText' in error);
  }

  private isAngularError(error: Error): boolean {
    const message = error.message.toLowerCase();
    const stack = (error.stack || '').toLowerCase();

    return (
      message.includes('ng0') ||
      message.includes('expressionchangedafterithasbeencheckederror') ||
      message.includes('nullinjectorerror') ||
      error.name.includes('Angular') ||
      stack.includes('angular') ||
      stack.includes('.html')
    );
  }

  private isChunkLoadError(error: Error): boolean {
    const message = error.message.toLowerCase();
    return (
      message.includes('loading chunk') ||
      message.includes('failed to fetch dynamically imported module') ||
      (message.includes('dynamically imported module') && message.includes('failed'))
    );
  }

  private isPromiseRejectionEvent(error: unknown): error is PromiseRejectionEvent {
    return !!error && 
           typeof error === 'object' && 
           'reason' in error && 
           'promise' in error;
  }

  // ─────────────────────────────────────────────────────────────────────
  // Utility Functions
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Ensure correlation ID exists (from logger or create new)
   */
  private ensureCorrelationId(): string {
    const existing = this.logger.getCorrelationId();
    if (existing) return existing;

    // Generate new ID for client-side errors
    const newId = this.generateCorrelationId();
    this.logger.setCorrelationId(newId);
    return newId;
  }

  private generateCorrelationId(): string {
    try {
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return crypto.randomUUID();
      }
    } catch {}
    return `client-${Date.now()}-${Math.random().toString(36).substring(2, 10)}`;
  }

  /**
   * Gather contextual information about where error occurred
   */
  private gatherErrorContext(): string {
    try {
      const route = this.router.url;
      return `${route} (route)`;
    } catch {
      return window.location.pathname;
    }
  }

  private safeWindowUrl(): string {
    try {
      return typeof window !== 'undefined' ? window.location.href : '';
    } catch {
      return '';
    }
  }

  private safeRouterUrl(): string {
    try {
      return this.router.url;
    } catch {
      return window.location.pathname;
    }
  }

  /**
   * Safe JSON stringification (prevents circular reference errors)
   */
  private safeStringify(value: unknown): string {
    try {
      return JSON.stringify(value, this.getCircularReplacer());
    } catch {
      return String(value);
    }
  }

  private getCircularReplacer() {
    const seen = new WeakSet();
    return (key: string, value: any) => {
      if (typeof value === 'object' && value !== null) {
        if (seen.has(value)) return '[Circular]';
        seen.add(value);
      }
      return value;
    };
  }

  // ─────────────────────────────────────────────────────────────────────
  // Error Code Mapping
  // ─────────────────────────────────────────────────────────────────────

  private getAngularErrorCode(
    error: Error,
    isChangeDetection: boolean,
    isTemplate: boolean,
    isNullInjector: boolean
  ): string {
    if (isChangeDetection) return 'CHANGE_DETECTION_ERROR';
    if (isTemplate) return 'TEMPLATE_ERROR';
    if (isNullInjector) return 'DEPENDENCY_INJECTION_ERROR';
    
    // Extract NG error code if present (e.g., NG0100)
    const match = error.message.match(/NG\d{4}/);
    if (match) return match[0];
    
    return 'ANGULAR_ERROR';
  }

  private getJavaScriptErrorCode(error: Error): string {
    const codeMap: Record<string, string> = {
      'TypeError': 'TYPE_ERROR',
      'ReferenceError': 'REFERENCE_ERROR',
      'RangeError': 'RANGE_ERROR',
      'SyntaxError': 'SYNTAX_ERROR',
      'URIError': 'URI_ERROR',
      'EvalError': 'EVAL_ERROR'
    };
    return codeMap[error.name] || 'CLIENT_ERROR';
  }

  private getJavaScriptErrorSeverity(error: Error): AppError['severity'] {
    // ReferenceErrors are often critical bugs
    if (error.name === 'ReferenceError') return 'error';
    // TypeErrors might be warnings (e.g., accessing null properties)
    if (error.name === 'TypeError') return 'error';
    // All others
    return 'error';
  }

  private getProductionAngularMessage(error: Error): string {
    if (error.message.includes('ExpressionChangedAfterItHasBeenCheckedError')) {
      return 'A display issue occurred. Please refresh the page.';
    }
    if (error.message.includes('NullInjectorError')) {
      return 'A configuration error occurred. Please contact support.';
    }
    return 'A rendering error occurred. Please refresh the page.';
  }

  // ─────────────────────────────────────────────────────────────────────
  // Fallback Error Creation
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Last resort: create minimal AppError when transformation fails
   */
  private createFallbackAppError(error: unknown, correlationId: string): AppError {
    return {
      message: 'An unexpected error occurred.',
      code: 'HANDLER_FAILURE',
      severity: 'critical',
      timestamp: new Date(),
      correlationId,
      details: {
        originalError: this.safeStringify(error),
        url: this.safeWindowUrl()
      }
    };
  }

  // ─────────────────────────────────────────────────────────────────────
  // Handler Failure (Last Resort)
  // ─────────────────────────────────────────────────────────────────────

  /**
   * Handle catastrophic failure: error handler itself threw an error
   * This should never happen, but defensive programming
   */
  private handleHandlerFailure(handlerError: unknown, originalError: unknown): void {
    console.error('[GlobalErrorHandler] CRITICAL: Error handler failed!');
    console.error('[GlobalErrorHandler] Handler error:', handlerError);
    console.error('[GlobalErrorHandler] Original error:', originalError);
    
    // Last resort: basic console output
    try {
      console.group('%c🔥 CRITICAL ERROR', 'color: red; font-weight: bold; font-size: 14px;');
      console.error('The error handler encountered an error while processing an error.');
      console.error('Handler Error:', handlerError);
      console.error('Original Error:', originalError);
      console.groupEnd();
    } catch {
      // If even console fails, we're in serious trouble
      // Nothing more we can do
    }
  }

  // ─────────────────────────────────────────────────────────────────────
  // Initialization Logging
  // ─────────────────────────────────────────────────────────────────────

  private logHandlerInitialization(): void {
    console.groupCollapsed(
      '%c🛡️ GlobalErrorHandler Initialized',
      'color: #10b981; font-weight: bold;'
    );
    console.log('%cEnvironment:', 'font-weight: bold;', environment.production ? 'Production' : 'Development');
    console.log('%cError Threshold:', 'font-weight: bold;', `${this.errorThreshold} errors / ${this.errorWindowMs}ms`);
    console.log('%cNotifications:', 'font-weight: bold;', environment.disableNotifications ? 'Disabled' : 'Enabled');
    console.log('%cGlobal Listeners:', 'font-weight: bold;', 'Active');
    console.groupEnd();
  }
}

// ═════════════════════════════════════════════════════════════════════
// Type Definitions
// ═════════════════════════════════════════════════════════════════════

/**
 * Error type classification
 */
type ErrorType =
  | 'already-handled'    // AppError from HTTP interceptor
  | 'http'               // HttpErrorResponse (defensive)
  | 'angular'            // Angular-specific errors
  | 'javascript'         // Standard JS errors
  | 'promise-rejection'  // Unhandled promise rejections
  | 'chunk-load'         // Lazy loading failures
  | 'unknown';           // Unclassified