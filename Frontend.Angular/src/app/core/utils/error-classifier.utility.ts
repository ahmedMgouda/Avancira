import { HttpErrorResponse } from '@angular/common/http';

import { 
  getHttpErrorMetadata, 
  isRetryableStatus, 
  NETWORK_ERROR_METADATA 
} from '../constants/http-error-metadata.constant';

export type ErrorCategory = 
  | 'network'
  | 'timeout'
  | 'client'
  | 'server'
  | 'auth'
  | 'unknown';

export interface ErrorClassification {
  category: ErrorCategory;
  isTransient: boolean;
  statusCode: number;
  message: string;
  metadata: ReturnType<typeof getHttpErrorMetadata>;
  isProblemDetails: boolean;
}

export class ErrorClassifier {
  /**
   * Comprehensive network error detection
   */
  static isNetworkError(error: HttpErrorResponse): boolean {
    return (
      error.status === 0 ||
      error.error instanceof ProgressEvent ||
      error.statusText === 'Unknown Error' ||
      error.statusText === '' ||
      error.message?.includes('Http failure response') ||
      error.message?.includes('ERR_CONNECTION_REFUSED') ||
      error.message?.includes('ERR_NAME_NOT_RESOLVED') ||
      error.message?.includes('ERR_INTERNET_DISCONNECTED') ||
      error.message?.includes('ERR_NETWORK_CHANGED') ||
      error.message?.includes('ERR_CONNECTION_RESET') ||
      error.message?.includes('ERR_CONNECTION_TIMED_OUT')
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
   * ✅ Uses HTTP_ERROR_METADATA for retryable determination
   */
  static isTransient(error: HttpErrorResponse): boolean {
    if (this.isNetworkError(error)) return true;
    return isRetryableStatus(error.status);
  }

  /**
   * Categorize error type
   */
  static categorize(error: HttpErrorResponse): ErrorCategory {
    const status = error.status;

    if (this.isNetworkError(error)) return 'network';
    if (this.isTimeout(error)) return 'timeout';
    if (status === 401 || status === 403) return 'auth';
    if (status >= 400 && status < 500) return 'client';
    if (status >= 500 && status < 600) return 'server';

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
   * ✅ Returns metadata from constant
   */
  static classify(error: HttpErrorResponse): ErrorClassification {
    const category = this.categorize(error);
    const isTransient = this.isTransient(error);
    const metadata = this.isNetworkError(error) 
      ? NETWORK_ERROR_METADATA 
      : getHttpErrorMetadata(error.status);

    return {
      category,
      isTransient,
      statusCode: error.status,
      message: metadata.userMessage,
      metadata,
      isProblemDetails: this.isProblemDetails(error.error)
    };
  }

  /**
   * Extract error code from Problem Details type URL
   */
  static extractCodeFromProblemType(type: string): string {
    const parts = type.split('/');
    const lastPart = parts[parts.length - 1];
    return lastPart.toUpperCase().replace(/-/g, '_');
  }
}