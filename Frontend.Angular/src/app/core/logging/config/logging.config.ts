/**
 * Logging System Configuration
 * Central configuration for all logging behavior
 */

import { LogLevel } from '../models/log-entry.model';

export interface LoggingConfig {
  /** Enable/disable entire logging system */
  enabled: boolean;
  
  /** Minimum log level to capture */
  minLevel: LogLevel;
  
  /** Application metadata */
  application: {
    name: string;
    version: string;
  };
  
  /** Trace configuration */
  trace: {
    /** Enable trace creation on navigation */
    enabled: boolean;
    
    /** Auto-end trace on navigation (default: true) */
    autoEndOnNavigation: boolean;
  };
  
  /** Span configuration */
  span: {
    /** Enable span creation */
    enabled: boolean;
    
    /** Root span timeout in milliseconds (default: 30000) */
    rootSpanTimeout: number;
    
    /** Auto-end root span on new user action (default: true) */
    autoEndOnNewAction: boolean;
    
    /** Maximum number of active spans (prevent memory leak) */
    maxActiveSpans: number;
  };
  
  /** UI tracking configuration */
  ui: {
    /** Enable directive-based tracking */
    enableDirectiveTracking: boolean;
    
    /** Enable global event listeners (fallback) */
    enableGlobalTracking: boolean;
    
    /** Global tracking mode */
    globalTrackingMode: 'fallback' | 'always' | 'disabled';
    
    /** Auto-track click events */
    autoTrackClicks: boolean;
    
    /** Auto-track form submissions */
    autoTrackSubmits: boolean;
    
    /** Auto-track input changes (usually too noisy) */
    autoTrackInputs: boolean;
    
    /** Selectors to ignore for global tracking */
    ignoreSelectors: string[];
    
    /** Minimum time between duplicate events (ms) */
    deduplicationWindow: number;
  };
  
  /** HTTP tracking configuration */
  http: {
    /** Enable HTTP span creation */
    enabled: boolean;
    
    /** Log request bodies */
    logRequestBodies: boolean;
    
    /** Log response bodies */
    logResponseBodies: boolean;
    
    /** Log request headers */
    logRequestHeaders: boolean;
    
    /** Log response headers */
    logResponseHeaders: boolean;
    
    /** URLs to skip logging (e.g., health checks) */
    skipUrls: string[];
    
    /** Slow request threshold in milliseconds */
    slowRequestThreshold: number;
  };
  
  /** Console logging configuration */
  console: {
    /** Enable console output */
    enabled: boolean;
    
    /** Use colored output */
    useColors: boolean;
    
    /** Show timestamps */
    showTimestamps: boolean;
    
    /** Collapse log groups */
    collapseGroups: boolean;
  };
  
  /** Remote logging configuration */
  remote: {
    /** Enable sending logs to backend */
    enabled: boolean;
    
    /** Backend endpoint URL */
    endpoint: string;
    
    /** Batch size before flushing */
    batchSize: number;
    
    /** Flush interval in milliseconds */
    flushInterval: number;
    
    /** Flush on navigation */
    flushOnNavigation: boolean;
    
    /** Flush on page unload */
    flushOnUnload: boolean;
    
    /** Max buffer size (prevent memory issues) */
    maxBufferSize: number;
    
    /** Retry failed requests */
    retryFailedRequests: boolean;
    
    /** Max retry attempts */
    maxRetries: number;
  };
  
  /** Data sanitization configuration */
  sanitization: {
    /** Enable data sanitization */
    enabled: boolean;
    
    /** Fields to redact (case-insensitive) */
    sensitiveFields: string[];
    
    /** Headers to redact (case-insensitive) */
    sensitiveHeaders: string[];
    
    /** Replace sensitive data with this value */
    redactedValue: string;
  };
  
  /** Performance configuration */
  performance: {
    /** Maximum log entry size in bytes */
    maxEntrySize: number;
    
    /** Truncate large payloads */
    truncateLargePayloads: boolean;
    
    /** Payload size limit in bytes */
    payloadSizeLimit: number;
  };
}

/**
 * Development configuration
 * Verbose logging for debugging
 */
export const developmentConfig: LoggingConfig = {
  enabled: true,
  minLevel: LogLevel.DEBUG,
  
  application: {
    name: 'avancira-frontend',
    version: '1.0.0' // TODO: Get from package.json
  },
  
  trace: {
    enabled: true,
    autoEndOnNavigation: true
  },
  
  span: {
    enabled: true,
    rootSpanTimeout: 30000, // 30 seconds
    autoEndOnNewAction: true,
    maxActiveSpans: 100
  },
  
  ui: {
    enableDirectiveTracking: true,
    enableGlobalTracking: true,
    globalTrackingMode: 'fallback', // Only track if no directive
    autoTrackClicks: true,
    autoTrackSubmits: true,
    autoTrackInputs: false, // Too noisy
    ignoreSelectors: [
      '[data-no-track]',
      '.no-track',
      'input[type="password"]',
      'input[type="text"]', // Handled specially - don't create new span if active exists
      'input[type="email"]'
    ],
    deduplicationWindow: 500 // 500ms
  },
  
  http: {
    enabled: true,
    logRequestBodies: true,
    logResponseBodies: true,
    logRequestHeaders: true,
    logResponseHeaders: true,
    skipUrls: [
      '/health',
      '/ping',
      '/heartbeat',
      '/api/logs' // Don't log the logging endpoint!
    ],
    slowRequestThreshold: 2000 // 2 seconds
  },
  
  console: {
    enabled: true,
    useColors: true,
    showTimestamps: true,
    collapseGroups: false
  },
  
  remote: {
    enabled: true,
    endpoint: '/bff/api/logs',
    batchSize: 50,
    flushInterval: 5000, // 5 seconds
    flushOnNavigation: true,
    flushOnUnload: true,
    maxBufferSize: 200,
    retryFailedRequests: true,
    maxRetries: 3
  },
  
  sanitization: {
    enabled: true,
    sensitiveFields: [
      'password',
      'newPassword',
      'confirmPassword',
      'currentPassword',
      'token',
      'accessToken',
      'refreshToken',
      'apiKey',
      'secret',
      'creditCard',
      'cardNumber',
      'cvv',
      'ssn',
      'socialSecurity'
    ],
    sensitiveHeaders: [
      'authorization',
      'cookie',
      'set-cookie',
      'x-api-key',
      'x-auth-token'
    ],
    redactedValue: '[REDACTED]'
  },
  
  performance: {
    maxEntrySize: 10000, // 10KB
    truncateLargePayloads: true,
    payloadSizeLimit: 5000 // 5KB
  }
};

/**
 * Production configuration
 * Minimal logging, focused on errors and critical events
 */
export const productionConfig: LoggingConfig = {
  enabled: true,
  minLevel: LogLevel.WARN, // Only warnings and errors
  
  application: {
    name: 'avancira-frontend',
    version: '1.0.0'
  },
  
  trace: {
    enabled: true,
    autoEndOnNavigation: true
  },
  
  span: {
    enabled: true,
    rootSpanTimeout: 30000,
    autoEndOnNewAction: true,
    maxActiveSpans: 50 // Lower in production
  },
  
  ui: {
    enableDirectiveTracking: true,
    enableGlobalTracking: false, // Disable in production to reduce noise
    globalTrackingMode: 'disabled',
    autoTrackClicks: true,
    autoTrackSubmits: true,
    autoTrackInputs: false,
    ignoreSelectors: [
      '[data-no-track]',
      '.no-track',
      'input[type="password"]',
      'input[type="text"]',
      'input[type="email"]'
    ],
    deduplicationWindow: 1000 // 1 second (more aggressive)
  },
  
  http: {
    enabled: true,
    logRequestBodies: false, // Don't log bodies in production
    logResponseBodies: false,
    logRequestHeaders: false,
    logResponseHeaders: false,
    skipUrls: [
      '/health',
      '/ping',
      '/heartbeat',
      '/api/logs',
      '/analytics' // Skip analytics endpoints
    ],
    slowRequestThreshold: 3000 // 3 seconds (less sensitive)
  },
  
  console: {
    enabled: false, // No console logs in production
    useColors: false,
    showTimestamps: false,
    collapseGroups: true
  },
  
  remote: {
    enabled: true,
    endpoint: '/bff/api/logs',
    batchSize: 100, // Larger batches in production
    flushInterval: 10000, // 10 seconds (less frequent)
    flushOnNavigation: true,
    flushOnUnload: true,
    maxBufferSize: 300,
    retryFailedRequests: true,
    maxRetries: 3
  },
  
  sanitization: {
    enabled: true,
    sensitiveFields: [
      'password',
      'newPassword',
      'confirmPassword',
      'currentPassword',
      'token',
      'accessToken',
      'refreshToken',
      'apiKey',
      'secret',
      'creditCard',
      'cardNumber',
      'cvv',
      'ssn',
      'socialSecurity',
      'bankAccount'
    ],
    sensitiveHeaders: [
      'authorization',
      'cookie',
      'set-cookie',
      'x-api-key',
      'x-auth-token',
      'x-csrf-token'
    ],
    redactedValue: '[REDACTED]'
  },
  
  performance: {
    maxEntrySize: 5000, // 5KB (smaller in production)
    truncateLargePayloads: true,
    payloadSizeLimit: 2000 // 2KB
  }
};

/**
 * Get configuration based on environment
 */
export function getLoggingConfig(production: boolean): LoggingConfig {
  return production ? productionConfig : developmentConfig;
}
