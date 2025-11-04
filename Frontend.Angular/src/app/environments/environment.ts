//import { LogLevel } from '@core/services/logger.service';

export const environment = {
  production: false,
  bffBaseUrl: 'https://localhost:9200/bff',
  frontendUrl: 'https://localhost:4200',

  /** Logging policy (used by LoggerService & loggingInterceptor) */
  logPolicy: {
    logLevel: 1,
    slowThresholdMs: 2000,
    enableRequestLogging: true,
    enableResponseLogging: true
  },

  /** Retry policy (used by ResilienceService / retryInterceptor) */
  retryPolicy: {
    maxRetries: 3,
    baseDelayMs: 1000,
    maxDelay: 30000,
    excluded: [400, 401, 403, 404, 422]
  },

  /** Skip / include patterns for noise reduction */
  skipLoggingPatterns: ['/health', '/ping', '/heartbeat', '/notifications/poll'],
  skipErrorNotifications: [404],
  importantEndpoints: ['/api/auth/login', '/api/auth/logout', '/api/orders', '/api/payments'],

  /** Feature flags */
  disableCorrelation: false,   // correlationIdInterceptor reads this
  disableNotifications: false,
  errorToastDuration: 7000,

  useSignalR: true,

    /**
   * Error page configuration
   * Controls critical error recovery strategies
   */
  errorPage: {
    /** Enable dedicated error page navigation */
    enabled: true,

    /** Path to error page component */
    path: '/error',

    /** Automatically reload app after critical error */
    autoReload: false,

    /** Delay before auto-reload (ms) */
    reloadDelayMs: 5000,
  },

  /**
   * Client-side error handling configuration
   */
  clientErrorHandling: {
    /** Show toast notifications for client-side errors in development */
    showClientErrorToasts: true,

    /** Include stack traces in error details (development only) */
    logStackTraces: true,

    /** Circuit breaker: max errors in time window */
    errorThreshold: 10,

    /** Circuit breaker: time window (ms) */
    errorWindowMs: 60000, // 1 minute
  },

};
