// core/constants/http-error-metadata.constant.ts
/**
 * HTTP Error Metadata - Single Source of Truth
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized error information for all HTTP status codes
 */

export interface HttpErrorMetadata {
  code: string;
  title: string;
  userMessage: string;
  reason: string;
  severity: 'info' | 'warning' | 'error' | 'critical';
  retryable: boolean;
}

export const HTTP_ERROR_METADATA: Record<number, HttpErrorMetadata> = {
  400: {
    code: 'BAD_REQUEST',
    title: 'Bad Request',
    userMessage: 'The request was invalid. Please check your input.',
    reason: 'Bad request - invalid data',
    severity: 'error',
    retryable: false
  },
  401: {
    code: 'UNAUTHORIZED',
    title: 'Unauthorized',
    userMessage: 'You need to log in to access this resource.',
    reason: 'Unauthorized - authentication required',
    severity: 'error',
    retryable: false
  },
  403: {
    code: 'FORBIDDEN',
    title: 'Forbidden',
    userMessage: 'You do not have permission to access this resource.',
    reason: 'Forbidden - insufficient permissions',
    severity: 'error',
    retryable: false
  },
  404: {
    code: 'NOT_FOUND',
    title: 'Not Found',
    userMessage: 'The requested resource was not found.',
    reason: 'Not found',
    severity: 'error',
    retryable: false
  },
  408: {
    code: 'TIMEOUT',
    title: 'Request Timeout',
    userMessage: 'The request took too long. Please try again.',
    reason: 'Request timeout',
    severity: 'warning',
    retryable: true
  },
  409: {
    code: 'CONFLICT',
    title: 'Conflict',
    userMessage: 'There was a conflict with the current state.',
    reason: 'Conflict - resource state mismatch',
    severity: 'error',
    retryable: false
  },
  422: {
    code: 'VALIDATION_ERROR',
    title: 'Validation Error',
    userMessage: 'The data provided was invalid.',
    reason: 'Validation error',
    severity: 'error',
    retryable: false
  },
  429: {
    code: 'TOO_MANY_REQUESTS',
    title: 'Too Many Requests',
    userMessage: 'Too many requests. Please try again in a moment.',
    reason: 'Too many requests - rate limited',
    severity: 'warning',
    retryable: true
  },
  500: {
    code: 'SERVER_ERROR',
    title: 'Server Error',
    userMessage: 'A server error occurred. Please try again.',
    reason: 'Internal server error',
    severity: 'critical',
    retryable: true
  },
  501: {
    code: 'NOT_IMPLEMENTED',
    title: 'Not Implemented',
    userMessage: 'This feature is not yet implemented.',
    reason: 'Not implemented',
    severity: 'error',
    retryable: false
  },
  502: {
    code: 'BAD_GATEWAY',
    title: 'Bad Gateway',
    userMessage: 'The server is temporarily unavailable. Please try again.',
    reason: 'Bad gateway',
    severity: 'warning',
    retryable: true
  },
  503: {
    code: 'SERVICE_UNAVAILABLE',
    title: 'Service Unavailable',
    userMessage: 'The service is temporarily unavailable. Please try again.',
    reason: 'Service unavailable',
    severity: 'warning',
    retryable: true
  },
  504: {
    code: 'GATEWAY_TIMEOUT',
    title: 'Gateway Timeout',
    userMessage: 'The request timed out. Please try again.',
    reason: 'Gateway timeout',
    severity: 'warning',
    retryable: true
  }
};

export const NETWORK_ERROR_METADATA: HttpErrorMetadata = {
  code: 'NETWORK_ERROR',
  title: 'Network Error',
  userMessage: 'Network connection failed. Please check your internet connection.',
  reason: 'Network connectivity issue',
  severity: 'warning',
  retryable: true
};

export function getHttpErrorMetadata(status: number): HttpErrorMetadata {
  if (status === 0) return NETWORK_ERROR_METADATA;
  if (HTTP_ERROR_METADATA[status]) return HTTP_ERROR_METADATA[status];

  const isServerError = status >= 500 && status < 600;
  const isClientError = status >= 400 && status < 500;

  return {
    code: `HTTP_${status}`,
    title: isServerError ? 'Server Error' : isClientError ? 'Request Error' : 'Error',
    userMessage: isServerError 
      ? 'A server error occurred. Please try again.'
      : isClientError
        ? 'The request was invalid. Please try again.'
        : 'An error occurred. Please try again.',
    reason: `HTTP ${status} error`,
    severity: isServerError ? 'critical' : 'error',
    retryable: isServerError
  };
}

export function isRetryableStatus(status: number): boolean {
  return getHttpErrorMetadata(status).retryable;
}

export function getErrorSeverity(status: number): HttpErrorMetadata['severity'] {
  return getHttpErrorMetadata(status).severity;
}