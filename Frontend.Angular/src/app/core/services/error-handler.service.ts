import { HttpErrorResponse } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { LoggerService } from './logger.service';

import { AppError, ProblemDetails, ValidationErrors } from '../models/error.model';

/**
 * Centralized error transformation service
 * ─────────────────────────────────────────────────────────────
 * Converts HttpErrorResponse → AppError
 * Handles:
 *   - RFC 7807 Problem Details
 *   - Validation errors
 *   - Common HTTP status codes
 * 
 * DESIGN NOTE: This service does NOT log errors directly.
 * Logging is handled by:
 *   - LoggingInterceptor (for HTTP request/response cycle)
 *   - GlobalErrorHandler (for client-side errors)
 * This prevents duplicate logging.
 */
@Injectable({ providedIn: 'root' })
export class ErrorHandlerService {
  private readonly logger = inject(LoggerService);

  // ─────────────────────────────────────────────────────────
  // HTTP Error Transformation
  // ─────────────────────────────────────────────────────────

  /**
   * Transform HttpErrorResponse to AppError
   * 
   * @param error - The HTTP error response from Angular
   * @param operation - Description of the operation (e.g., "GET /api/users")
   * @returns Normalized AppError for consistent handling
   * 
   * CHANGE: Removed internal logging - LoggingInterceptor already logs this
   */
  handleHttpError(error: HttpErrorResponse, operation = 'operation'): AppError {
    const correlationId = this.getOrCreateCorrelationId();

    // Default AppError baseline
    const appError: AppError = {
      message: 'An unexpected error occurred.',
      status: error.status,
      timestamp: new Date(),
      correlationId,
    };

    // Handle RFC 7807 Problem Details first
    if (this.isProblemDetails(error.error)) {
      const problemError = this.transformProblemDetails(error.error);
      problemError.correlationId = correlationId;
      // REMOVED: this.logError() call - no longer needed
      return problemError;
    }

    // Handle network / connectivity issues
    if (error.status === 0) {
      appError.code = 'NETWORK_ERROR';
      appError.message = 'Unable to connect to the server. Please check your internet connection.';
      appError.severity = 'critical';
      // REMOVED: this.logError() call
      return appError;
    }

    // Map known HTTP codes
    const mapped = this.mapHttpStatus(error, operation);
    mapped.correlationId = correlationId;
    // REMOVED: this.logError() call
    return mapped;
  }

  // ─────────────────────────────────────────────────────────
  // RFC 7807 Transformation
  // ─────────────────────────────────────────────────────────

  private transformProblemDetails(problem: ProblemDetails): AppError {
    const appError: AppError = {
      message: problem.detail || problem.title || 'An error occurred.',
      status: problem.status,
      code: problem.type,
      timestamp: new Date(),
      details: {
        instance: problem.instance,
        extensions: problem.extensions,
      },
    };

    // Handle validation errors with array truncation
    if (problem.errors) {
      const messages = Array.isArray(problem.errors)
        ? problem.errors
        : Object.values(problem.errors as ValidationErrors).flat();

      const limited = messages.slice(0, 5);
      appError.message = limited.join('. ') + (messages.length > 5 ? '...' : '');
      appError.code = 'VALIDATION_ERROR';
      appError.severity = 'warning';
    }

    return appError;
  }

  private isProblemDetails(error: unknown): error is ProblemDetails {
    return !!error && typeof error === 'object' &&
      ('detail' in error || 'title' in error || 'type' in error);
  }

  // ─────────────────────────────────────────────────────────
  // HTTP Status Mapping
  // ─────────────────────────────────────────────────────────

  private mapHttpStatus(error: HttpErrorResponse, operation: string): AppError {
    const appError: AppError = {
      message: `Failed to ${operation}.`,
      status: error.status,
      timestamp: new Date(),
    };

    switch (error.status) {
      case 400:
      case 422:
        appError.code = 'VALIDATION_ERROR';
        appError.severity = 'warning';
        appError.message = this.extractValidationMessage(error)
          ?? 'The request contains invalid data. Please check your input.';
        break;

      case 401:
        appError.code = 'UNAUTHORIZED';
        appError.severity = 'warning';
        appError.message = 'Your session has expired. Please log in again.';
        break;

      case 403:
        appError.code = 'FORBIDDEN';
        appError.severity = 'error';
        appError.message = 'You do not have permission to perform this action.';
        break;

      case 404:
        appError.code = 'NOT_FOUND';
        appError.severity = 'info';
        appError.message = 'The requested resource was not found.';
        break;

      case 409:
        appError.code = 'CONFLICT';
        appError.severity = 'warning';
        appError.message = (error.error?.message as string)
          ?? 'A conflict occurred. The resource may have been modified by another user.';
        break;

      case 429:
        appError.code = 'RATE_LIMIT';
        appError.severity = 'warning';
        appError.message = 'Too many requests. Please try again later.';
        break;

      case 500:
      case 502:
      case 503:
      case 504:
        appError.code = 'SERVER_ERROR';
        appError.severity = 'error';
        appError.message = 'A server error occurred. Please try again later.';
        break;

      default:
        appError.code = 'UNKNOWN_ERROR';
        appError.severity = 'error';
        appError.message = (error.error?.message as string)
          ?? `Failed to ${operation}. Please try again.`;
    }

    appError.details = {
      url: error.url ?? '',
      statusText: error.statusText ?? '',
      originalError: error.error ?? null,
    };

    return appError;
  }

  private extractValidationMessage(error: HttpErrorResponse): string | null {
    const body = error.error;
    if (!body?.errors) return null;

    const messages = Array.isArray(body.errors)
      ? body.errors
      : Object.values(body.errors as ValidationErrors).flat();

    return messages.slice(0, 5).join('. ');
  }

  // ─────────────────────────────────────────────────────────
  // Helper for UI
  // ─────────────────────────────────────────────────────────

  /**
   * Produces a short, user-friendly message string for display components.
   * 
   * ENHANCEMENT: Added more specific cases for better UX
   */
  toUserMessage(error: AppError): string {
    // Validation errors
    if (error.severity === 'warning' && error.code === 'VALIDATION_ERROR') {
      return 'Please correct the highlighted fields.';
    }

    // Network errors
    if (error.code === 'NETWORK_ERROR') {
      return 'Connection lost. Check your internet connection.';
    }

    // Authentication errors
    if (error.code === 'UNAUTHORIZED') {
      return 'Your session expired. Please sign in again.';
    }

    // Authorization errors
    if (error.code === 'FORBIDDEN') {
      return 'You don\'t have permission to perform this action.';
    }

    // Conflict errors (NEW: more specific handling)
    if (error.code === 'CONFLICT' && error.status === 409) {
      return 'This item was modified by another user. Please refresh and try again.';
    }

    // Rate limiting (NEW)
    if (error.code === 'RATE_LIMIT') {
      return 'Too many requests. Please wait a moment and try again.';
    }

    // Server errors
    if (error.code === 'SERVER_ERROR') {
      return 'Something went wrong on our side. Please try again later.';
    }

    // Fallback to the error message
    return error.message;
  }

  // ─────────────────────────────────────────────────────────
  // Utilities for Global Error Handler
  // ─────────────────────────────────────────────────────────

  /**
   * NEW: Type guard for AppError detection
   * Used by GlobalErrorHandler to avoid double-processing
   */
  isAppError(error: unknown): error is AppError {
    return !!error &&
      typeof error === 'object' &&
      'message' in error &&
      'timestamp' in error &&
      error.timestamp instanceof Date;
  }

  /**
   * NEW: Get existing correlation ID or create a fallback
   * Ensures every error has tracking ID even if interceptor didn't set one
   */
  private getOrCreateCorrelationId(): string {
    const existing = this.logger.getCorrelationId();
    if (existing) return existing;

    // Fallback generation (shouldn't normally happen, but defensive)
    return this.generateCorrelationId();
  }

  /**
   * NEW: Correlation ID generation utility
   * Matches the logic in correlation interceptor
   */
  private generateCorrelationId(): string {
    try {
      if (typeof crypto !== 'undefined' && crypto.randomUUID) {
        return crypto.randomUUID();
      }
    } catch { }
    const random = Math.random().toString(36).substring(2, 10);
    return `fallback-${Date.now()}-${random}`;
  }
}