// core/config/sampling.config.ts
/**
 * Unified Type-Based Sampling Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware sampling - same for all users
 * 
 * UNIFIED APPROACH:
 * ✅ All users (authenticated + anonymous) sampled the same way
 * ✅ Sample by log type only (not by user type)
 * ✅ Backend can filter by user if needed
 */

import { type Environment,getCurrentEnvironment } from './environment.config';

export interface SamplingRateConfig {
  /** Enable/disable sampling globally */
  enabled: boolean;

  /** Default rate for unknown types (0.0 - 1.0) */
  defaultRate: number;

  /** Per-type sampling rates */
  rates: Record<string, number>;

  /** Types that should never be sampled out */
  alwaysInclude: string[];
}

const CONFIG_BY_ENV: Record<Environment, SamplingRateConfig> = {
  // ═══════════════════════════════════════════════════════════════════
  // Development Configuration
  // ═══════════════════════════════════════════════════════════════════
  dev: {
    enabled: false,       // Log everything in dev (no sampling)
    defaultRate: 1.0,     // 100% - keep everything
    rates: {},
    alwaysInclude: []
  },

  // ═══════════════════════════════════════════════════════════════════
  // Staging Configuration
  // ═══════════════════════════════════════════════════════════════════
  staging: {
    enabled: true,        // Moderate sampling for testing
    defaultRate: 0.5,     // 50% default

    rates: {
      // ─────────────────────────────────────────────────────────────
      // Critical (always log)
      // ─────────────────────────────────────────────────────────────
      'error': 1.0,        // 100% of errors
      'fatal': 1.0,        // 100% of fatal errors
      'security': 1.0,     // 100% of security events

      // ─────────────────────────────────────────────────────────────
      // High priority
      // ─────────────────────────────────────────────────────────────
      'http': 0.8,         // 80% of HTTP logs
      'navigation': 0.5,   // 50% of navigation
      'auth': 0.8,         // 80% of authentication

      // ─────────────────────────────────────────────────────────────
      // Medium priority
      // ─────────────────────────────────────────────────────────────
      'application': 0.5,  // 50% of application logs
      'performance': 0.5,  // 50% of performance logs

      // ─────────────────────────────────────────────────────────────
      // Low priority
      // ─────────────────────────────────────────────────────────────
      'debug': 0.3,        // 30% of debug logs
      'trace': 0.1         // 10% of trace logs
    },

    alwaysInclude: ['error', 'fatal', 'security']
  },

  // ═══════════════════════════════════════════════════════════════════
  // Production Configuration (Optimized for 1000+ Users)
  // ═══════════════════════════════════════════════════════════════════
  prod: {
    enabled: true,        // Aggressive sampling for cost optimization
    defaultRate: 0.1,     // 10% default (reduce log volume by 90%)

    rates: {
      // ─────────────────────────────────────────────────────────────
      // Critical (NEVER sample out - 100%)
      // ─────────────────────────────────────────────────────────────
      'error': 1.0,        // 100% of errors (ALWAYS log)
      'fatal': 1.0,        // 100% of fatal errors (ALWAYS log)
      'security': 1.0,     // 100% of security events (ALWAYS log)

      // ─────────────────────────────────────────────────────────────
      // High priority (sufficient for monitoring)
      // ─────────────────────────────────────────────────────────────
      'http': 0.2,         // 20% of HTTP logs (200 req/1000 logged)
      'navigation': 0.1,   // 10% of navigation
      'auth': 0.5,         // 50% of authentication (important for security)

      // ─────────────────────────────────────────────────────────────
      // Medium priority
      // ─────────────────────────────────────────────────────────────
      'application': 0.1,  // 10% of application logs
      'performance': 0.05, // 5% of performance logs

      // ─────────────────────────────────────────────────────────────
      // Low priority (minimal sampling)
      // ─────────────────────────────────────────────────────────────
      'debug': 0.01,       // 1% of debug (almost none in prod)
      'trace': 0.001       // 0.1% of trace (essentially disabled)
    },

    alwaysInclude: ['error', 'fatal', 'security']
  }
};

/**
 * Get sampling configuration for current environment
 */
export function getSamplingConfig(env?: Environment): SamplingRateConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}

/**
 * Check if sampling is enabled
 */
export function isSamplingEnabled(): boolean {
  return getSamplingConfig().enabled;
}

/**
 * Get sampling rate for a specific log type
 */
export function getSamplingRate(type: string): number {
  const config = getSamplingConfig();
  
  // Check if type should always be included
  if (config.alwaysInclude.includes(type)) {
    return 1.0;
  }
  
  // Return configured rate or default
  return config.rates[type] ?? config.defaultRate;
}