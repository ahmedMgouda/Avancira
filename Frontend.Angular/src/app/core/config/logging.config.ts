// core/config/logging.config.ts
/**
 * Unified Logging Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware logging configuration
 * 
 * UNIFIED APPROACH:
 * ✅ All users (authenticated + anonymous) logged the same way
 * ✅ No user-type specific configs
 * ✅ Backend handles user filtering if needed
 * ✅ Simpler configuration
 */

import { type Environment,getCurrentEnvironment } from './environment.config';

export interface LoggingConfig {
  application: {
    name: string;
    version: string;
  };

  console: {
    enabled: boolean;
    useColors: boolean;
  };

  remote: {
    enabled: boolean;
    endpoint: string;
    batchSize: number;
    flushInterval: number;
    retry: {
      enabled: boolean;
      maxRetries: number;
      baseDelayMs: number;
    };
  };

  sanitization: {
    enabled: boolean;
    sensitiveFields: string[];
    redactedValue: string;
  };

  buffer: {
    maxSize: number;
  };
}

const CONFIG_BY_ENV: Record<Environment, LoggingConfig> = {
  // ═══════════════════════════════════════════════════════════════════
  // Development Configuration
  // ═══════════════════════════════════════════════════════════════════
  dev: {
    application: {
      name: 'avancira-frontend',
      version: '1.0.0'
    },

    console: {
      enabled: true,        // Full console logging in dev
      useColors: true
    },

    remote: {
      enabled: false,       // No remote logging in dev (optional)
      endpoint: '/api/logs',
      batchSize: 10,
      flushInterval: 5000,  // 5 seconds
      retry: {
        enabled: false,     // No retry in dev
        maxRetries: 0,
        baseDelayMs: 1000
      }
    },

    sanitization: {
      enabled: true,        // Always sanitize sensitive data
      sensitiveFields: [
        'password', 'token', 'authorization', 'api_key', 'secret',
        'credit_card', 'ssn', 'accessToken', 'refreshToken', 'apiKey',
        'privateKey', 'Bearer', 'sessionToken'
      ],
      redactedValue: '[REDACTED]'
    },

    buffer: {
      maxSize: 100          // Reasonable buffer for dev
    }
  },

  // ═══════════════════════════════════════════════════════════════════
  // Staging Configuration
  // ═══════════════════════════════════════════════════════════════════
  staging: {
    application: {
      name: 'avancira-frontend',
      version: '1.0.0'
    },

    console: {
      enabled: true,        // Console + remote in staging
      useColors: true
    },

    remote: {
      enabled: true,        // Enable remote logging
      endpoint: '/api/logs',
      batchSize: 10,
      flushInterval: 5000,  // 5 seconds
      retry: {
        enabled: true,      // Retry on failures
        maxRetries: 3,
        baseDelayMs: 1000
      }
    },

    sanitization: {
      enabled: true,
      sensitiveFields: [
        'password', 'token', 'authorization', 'api_key', 'secret',
        'credit_card', 'ssn', 'accessToken', 'refreshToken', 'apiKey',
        'privateKey', 'Bearer', 'sessionToken'
      ],
      redactedValue: '[REDACTED]'
    },

    buffer: {
      maxSize: 100
    }
  },

  // ═══════════════════════════════════════════════════════════════════
  // Production Configuration
  // ═══════════════════════════════════════════════════════════════════
  prod: {
    application: {
      name: 'avancira-frontend',
      version: '1.0.0'
    },

    console: {
      enabled: false,       // No console logging in prod
      useColors: false
    },

    remote: {
      enabled: true,        // Only remote logging in prod
      endpoint: '/api/logs',
      batchSize: 20,        // Larger batches for efficiency
      flushInterval: 10000, // 10 seconds - less frequent
      retry: {
        enabled: true,      // Critical - retry on failures
        maxRetries: 3,
        baseDelayMs: 1000
      }
    },

    sanitization: {
      enabled: true,        // CRITICAL - always sanitize in prod
      sensitiveFields: [
        'password', 'token', 'authorization', 'api_key', 'secret',
        'credit_card', 'ssn', 'accessToken', 'refreshToken', 'apiKey',
        'privateKey', 'Bearer', 'sessionToken', 'cvv', 'pin'
      ],
      redactedValue: '[REDACTED]'
    },

    buffer: {
      maxSize: 200          // Larger buffer for 1000+ users
    }
  }
};

/**
 * Get logging configuration for current environment
 */
export function getLoggingConfig(env?: Environment): LoggingConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}

/**
 * Check if remote logging is enabled
 */
export function isRemoteLoggingEnabled(): boolean {
  return getLoggingConfig().remote.enabled;
}

/**
 * Check if console logging is enabled
 */
export function isConsoleLoggingEnabled(): boolean {
  return getLoggingConfig().console.enabled;
}