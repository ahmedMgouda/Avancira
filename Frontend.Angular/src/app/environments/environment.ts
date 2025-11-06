export const environment = {
  production: false,
  bffBaseUrl: 'https://localhost:9200/bff',
  frontendUrl: 'https://localhost:4200',

  /** Logging policy */
  logPolicy: {
    slowThresholdMs: 2000,
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
  errorToastDuration: 7000,
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
    showClientErrorToasts: true,
    logStackTraces: true,
    errorThreshold: 10,
    errorWindowMs: 60000,
  },
};