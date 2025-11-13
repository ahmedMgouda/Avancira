// core/config/loading.config.ts
/**
 * Loading Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware loading system configuration
 */

import { type Environment,getCurrentEnvironment } from './environment.config';

export interface LoadingConfig {
  debounceDelay: number;
  requestTimeout: number;
  maxRequests: number;
  errorRetentionTime: number;
  buffer: {
    maxSize: number;
  };
}

const CONFIG_BY_ENV: Record<Environment, LoadingConfig> = {
  dev: {
    debounceDelay: 100,      // Faster feedback in dev
    requestTimeout: 60000,   // Longer timeout for debugging
    maxRequests: 50,         // Smaller limit for dev
    errorRetentionTime: 10000, // Keep errors longer
    buffer: {
      maxSize: 50
    }
  },

  prod: {
    debounceDelay: 200,
    requestTimeout: 30000,
    maxRequests: 200,        // Larger for 1000+ users
    errorRetentionTime: 5000,
    buffer: {
      maxSize: 200           // Larger buffer for scale
    }
  }
};

export function getLoadingConfig(env?: Environment): LoadingConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}