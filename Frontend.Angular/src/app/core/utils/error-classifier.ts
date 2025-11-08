/**
 * Error Classifier Utility
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized error classification and detection logic
 * Replaces duplicate error detection in network.interceptor and ResilienceService
 * 
 * Features:
 *   ✅ Network error detection (0, timeout, DNS, connection refused)
 *   ✅ Transient error classification (should retry)
 *   ✅ Client vs Server error categorization
 *   ✅ Problem Details (RFC 7807) detection
 */

import { HttpErrorResponse } from '@angular/common/http';

export type ErrorCategory = 
  | 'network'      // Network connectivity issues
  | 'timeout'      // Request timeout
  | 'client'       // 4xx errors (bad request, validation, etc.)
  | 'server'       // 5xx errors
  | 'auth'         // 401, 403 authentication/authorization
  | 'unknown';     // Unclassified

export interface ErrorClassification {
  category: ErrorCategory;
  isTransient: boolean;      // Should retry?
  isRetryable: boolean;      // Same as isTransient (for compatibility)
  statusCode: number;
  message: string;
  isNetworkError: boolean;
  isProblemDetails: boolean;
}

export class ErrorClassifier {
  /**
   * Comprehensive network error detection
   * Catches various network failure scenarios across browsers
   */
  static isNetworkError(error: HttpErrorResponse): boolean {
    return (
      error.status === 0 ||                                    // No response from server
      error.error instanceof ProgressEvent ||                  // Network failure event
      error.statusText === 'Unknown Error' ||                  // Browser network error
      error.statusText === '' ||                               // Empty status text
      error.message?.includes('Http failure response') ||      // Angular HTTP error
      error.message?.includes('ERR_CONNECTION_REFUSED') ||     // Connection refused
      error.message?.includes('ERR_NAME_NOT_RESOLVED') ||      // DNS error
      error.message?.includes('ERR_INTERNET_DISCONNECTED') ||  // Internet disconnected
      error.message?.includes('ERR_NETWORK_CHANGED') ||        // Network changed
      error.message?.includes('ERR_CONNECTION_RESET') ||       // Connection reset
      error.message?.includes('ERR_CONNECTION_TIMED_OUT')      // Connection timeout
    );
  }

  /**
   * Check if error is a timeout
   */
  static isTimeout(error: HttpErrorResponse): boolean {
    return (
      error.status === 408 ||
      error.status === 504 ||
      error.message?.toLowerCase().includes('timeout')
    );
  }

  /**
   * Check if error is transient (should be retried)
   * Transient errors: network issues, timeouts, rate limits, temporary server errors
   */
  static isTransient(error: HttpErrorResponse): boolean {
    const status = error.status;

    // Network errors are transient
    if (this.isNetworkError(error)) {
      return true;
    }

    // Specific transient status codes
    const transientCodes = [
      0,    // Network error
      408,  // Request Timeout
      429,  // Too Many Requests
      500,  // Internal Server Error (may be temporary)
      502,  // Bad Gateway
      503,  // Service Unavailable
      504   // Gateway Timeout
    ];

    return transientCodes.includes(status);
  }

  /**
   * Check if error should be retried
   * More conservative than isTransient - excludes excluded status codes
   */
  static shouldRetry(
    error: HttpErrorResponse,
    excludedCodes: number[] = [400, 401, 403, 404, 409, 422]
  ): boolean {
    // Don't retry explicitly excluded codes
    if (excludedCodes.includes(error.status)) {
      return false;
    }

    return this.isTransient(error);
  }

  /**
   * Categorize error type
   */
  static categorize(error: HttpErrorResponse): ErrorCategory {
    const status = error.status;

    if (this.isNetworkError(error)) {
      return 'network';
    }

    if (this.isTimeout(error)) {
      return 'timeout';
    }

    if (status === 401 || status === 403) {
      return 'auth';
    }

    if (status >= 400 && status < 500) {
      return 'client';
    }

    if (status >= 500 && status < 600) {
      return 'server';
    }

    return 'unknown';
  }

  /**
   * Check if error response is RFC 7807 Problem Details
   */
  static isProblemDetails(errorBody: any): boolean {
    return (
      errorBody &&
      typeof errorBody === 'object' &&
      'type' in errorBody &&
      'title' in errorBody &&
      'status' in errorBody
    );
  }

  /**
   * Full error classification with all metadata
   */
  static classify(error: HttpErrorResponse): ErrorClassification {
    const category = this.categorize(error);
    const isTransient = this.isTransient(error);

    return {
      category,
      isTransient,
      isRetryable: isTransient, // Alias for compatibility
      statusCode: error.status,
      message: error.message || error.statusText || 'Unknown error',
      isNetworkError: this.isNetworkError(error),
      isProblemDetails: this.isProblemDetails(error.error)
    };
  }

  /**
   * Get human-readable error reason
   */
  static getReason(error: HttpErrorResponse): string {
    if (this.isNetworkError(error)) {
      return 'Network connectivity issue';
    }

    if (this.isTimeout(error)) {
      return 'Request timeout';
    }

    const status = error.status;

    const reasonMap: Record<number, string> = {
      400: 'Bad request - invalid data',
      401: 'Unauthorized - authentication required',
      403: 'Forbidden - insufficient permissions',
      404: 'Not found',
      409: 'Conflict - resource state mismatch',
      422: 'Validation error',
      429: 'Too many requests - rate limited',
      500: 'Internal server error',
      501: 'Not implemented',
      502: 'Bad gateway',
      503: 'Service unavailable',
      504: 'Gateway timeout'
    };

    return reasonMap[status] || `HTTP ${status} error`;
  }

  /**
   * Get severity level for logging/monitoring
   */
  static getSeverity(error: HttpErrorResponse): 'info' | 'warning' | 'error' | 'critical' {
    const status = error.status;

    if (status >= 500) return 'critical';
    if (status === 429) return 'warning'; // Rate limit
    if (status >= 400) return 'error';
    if (this.isTimeout(error)) return 'warning';
    if (this.isNetworkError(error)) return 'warning';

    return 'info';
  }

  /**
   * Extract error code from Problem Details type URL
   * Example: "https://example.com/errors/validation-error" → "VALIDATION_ERROR"
   */
  static extractCodeFromProblemType(type: string): string {
    const parts = type.split('/');
    const lastPart = parts[parts.length - 1];
    return lastPart.toUpperCase().replace(/-/g, '_');
  }

  /**
   * Map HTTP status to error code
   */
  static statusToCode(status: number): string {
    const codeMap: Record<number, string> = {
      400: 'BAD_REQUEST',
      401: 'UNAUTHORIZED',
      403: 'FORBIDDEN',
      404: 'NOT_FOUND',
      408: 'TIMEOUT',
      409: 'CONFLICT',
      422: 'VALIDATION_ERROR',
      429: 'TOO_MANY_REQUESTS',
      500: 'SERVER_ERROR',
      502: 'BAD_GATEWAY',
      503: 'SERVICE_UNAVAILABLE',
      504: 'GATEWAY_TIMEOUT'
    };

    return codeMap[status] || `HTTP_${status}`;
  }
}