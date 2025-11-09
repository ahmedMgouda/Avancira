// core/config/deduplication.config.ts
/**
 * Unified Deduplication Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware deduplication - same for all users
 * 
 * UNIFIED APPROACH:
 * ✅ All users (authenticated + anonymous) deduplicated the same way
 * ✅ Prevents log/toast spam regardless of user type
 * ✅ Backend can apply additional filtering if needed
 */

import { type Environment,getCurrentEnvironment } from './environment.config';

export interface ServiceDedupConfig {
  /** Enable/disable deduplication */
  enabled: boolean;

  /** Time window to remember items (milliseconds) */
  windowMs: number;

  /** Maximum cache size (prevent memory leaks) */
  maxCacheSize: number;
}

export interface DeduplicationConfig {
  /** Logging deduplication */
  logging: ServiceDedupConfig;

  /** Toast deduplication */
  toasts: ServiceDedupConfig;

  /** Network error deduplication */
  networkErrors: ServiceDedupConfig;
}

const CONFIG_BY_ENV: Record<Environment, DeduplicationConfig> = {
  // ═══════════════════════════════════════════════════════════════════
  // Development Configuration
  // ═══════════════════════════════════════════════════════════════════
  dev: {
    logging: {
      enabled: false,      // See all duplicates in dev (easier debugging)
      windowMs: 5000,      // 5 seconds
      maxCacheSize: 100
    },
    
    toasts: {
      enabled: false,      // See all duplicate toasts in dev
      windowMs: 3000,      // 3 seconds
      maxCacheSize: 50
    },
    
    networkErrors: {
      enabled: false,      // Track all errors in dev
      windowMs: 10000,     // 10 seconds
      maxCacheSize: 30
    }
  },

  // ═══════════════════════════════════════════════════════════════════
  // Staging Configuration
  // ═══════════════════════════════════════════════════════════════════
  staging: {
    logging: {
      enabled: true,       // Production-like behavior
      windowMs: 5000,      // 5 seconds
      maxCacheSize: 100
    },
    
    toasts: {
      enabled: true,       // Prevent toast spam
      windowMs: 3000,      // 3 seconds
      maxCacheSize: 50
    },
    
    networkErrors: {
      enabled: true,       // Prevent duplicate error tracking
      windowMs: 10000,     // 10 seconds
      maxCacheSize: 30
    }
  },

  // ═══════════════════════════════════════════════════════════════════
  // Production Configuration (Optimized for 1000+ Users)
  // ═══════════════════════════════════════════════════════════════════
  prod: {
    logging: {
      enabled: true,       // CRITICAL - prevent log spam
      windowMs: 5000,      // 5 seconds
      maxCacheSize: 200    // Larger cache for 1000+ users
    },
    
    toasts: {
      enabled: true,       // CRITICAL - prevent toast spam
      windowMs: 3000,      // 3 seconds
      maxCacheSize: 50
    },
    
    networkErrors: {
      enabled: true,       // Prevent duplicate error tracking
      windowMs: 10000,     // 10 seconds
      maxCacheSize: 50     // Larger cache for scale
    }
  }
};

/**
 * Get deduplication configuration for current environment
 */
export function getDeduplicationConfig(env?: Environment): DeduplicationConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}

/**
 * Check if logging deduplication is enabled
 */
export function isLoggingDeduplicationEnabled(): boolean {
  return getDeduplicationConfig().logging.enabled;
}

/**
 * Check if toast deduplication is enabled
 */
export function isToastDeduplicationEnabled(): boolean {
  return getDeduplicationConfig().toasts.enabled;
}

/**
 * Check if network error deduplication is enabled
 */
export function isNetworkErrorDeduplicationEnabled(): boolean {
  return getDeduplicationConfig().networkErrors.enabled;
}