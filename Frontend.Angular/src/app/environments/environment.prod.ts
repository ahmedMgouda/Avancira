import { LogLevel } from '@core/services/logger.service';

export const environment = {
  production: true,
  bffBaseUrl: 'https://avancira.com/bff',
  frontendUrl: 'https://avancira.com',

  logPolicy: {
    logLevel: LogLevel.Warn,        // Only warnings and errors
    slowThresholdMs: 2000,
    enableRequestLogging: false,    // Reduce log noise
    enableResponseLogging: false
  },

  retryPolicy: {
    maxRetries: 3,
    baseDelayMs: 1000,
    maxDelay: 30000,
    excluded: [400, 401, 403, 404, 422]
  },

  skipLoggingPatterns: ['/health', '/ping', '/heartbeat', '/notifications/poll'],
  skipErrorNotifications: [404],
  importantEndpoints: ['/api/auth/login', '/api/auth/logout', '/api/orders', '/api/payments'],

  disableCorrelation: false,
  disableNotifications: false,
  errorToastDuration: 7000,

  /** Observability / Telemetry */
  sentryDsn: '',
  appInsightsKey: '',

  useSignalR: true,


  errorPage: {
    enabled: true,
    path: '/error',
    autoReload: false, // Don't auto-reload in production (user should decide)
    reloadDelayMs: 10000,
  },

  clientErrorHandling: {
    showClientErrorToasts: false, // Silent in production (only actionable errors)
    logStackTraces: false, // Don't expose stack traces in production
    errorThreshold: 20, // Higher threshold for production
    errorWindowMs: 60000,
  },
};
