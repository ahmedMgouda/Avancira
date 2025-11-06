export const environment = {
  production: true,
  bffBaseUrl: 'https://api.avancira.com/bff',
  frontendUrl: 'https://www.avancira.com',

  /** Logging policy */
  logPolicy: {
    slowThresholdMs: 3000,
  },

  /** Retry policy */
  retryPolicy: {
    maxRetries: 3,
    baseDelayMs: 1000,
    maxDelay: 30000,
    excluded: [400, 401, 403, 404, 422]
  },

  /** Skip patterns */
  skipLoggingPatterns: ['/health', '/ping', '/heartbeat'],
  skipErrorNotifications: [404],

  /** Feature flags */
  disableNotifications: false,
  errorToastDuration: 5000,
  useSignalR: true,

  /** Error page configuration */
  errorPage: {
    enabled: true,
    path: '/error',
    autoReload: false,
    reloadDelayMs: 5000,
  },

  /** Client-side error handling */
  clientErrorHandling: {
    showClientErrorToasts: false, // Don't show in production
    logStackTraces: false, // Security - don't expose stack traces
    errorThreshold: 10,
    errorWindowMs: 60000,
  },
};