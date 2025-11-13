// core/config/toast.config.ts
/**
 * Toast Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware toast and deduplication configuration
 */

import { type Environment, getCurrentEnvironment } from './environment.config';

export interface ToastDeduplicationConfig {
  enabled: boolean;
  windowMs: number;
  maxSuppressedBeforeShow: number;
  showSuppressedCount: boolean;
  cleanupIntervalMs: number;
}

export interface ToastDisplayConfig {
  maxVisible: number;
  defaultDuration: number;
  position: 'top-left' | 'top-center' | 'top-right' | 'bottom-left' | 'bottom-center' | 'bottom-right';
}

export interface ToastConfig {
  display: ToastDisplayConfig;
  deduplication: ToastDeduplicationConfig;
}

const CONFIG_BY_ENV: Record<Environment, ToastConfig> = {
  // ═══════════════════════════════════════════════════════════════════
  // Development Configuration
  // ═══════════════════════════════════════════════════════════════════
  dev: {
    display: {
      maxVisible: 5,
      defaultDuration: 5000,
      position: 'top-right'
    },
    deduplication: {
      enabled: true,
      windowMs: 3000,              // 3 seconds - show more in dev
      maxSuppressedBeforeShow: 3,  // Lower threshold for dev
      showSuppressedCount: true,   // Show suppression counts
      cleanupIntervalMs: 60000     // 1 minute
    }
  },
  
  // ═══════════════════════════════════════════════════════════════════
  // Production Configuration
  // ═══════════════════════════════════════════════════════════════════
  prod: {
    display: {
      maxVisible: 5,
      defaultDuration: 5000,
      position: 'top-right'
    },
    deduplication: {
      enabled: true,
      windowMs: 10000,             // 10 seconds - suppress more
      maxSuppressedBeforeShow: 10, // Higher threshold
      showSuppressedCount: false,  // Don't show counts in prod
      cleanupIntervalMs: 120000    // 2 minutes
    }
  }
};

/**
 * Get toast configuration for current environment
 */
export function getToastConfig(env?: Environment): ToastConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}

/**
 * Get only deduplication config
 */
export function getToastDeduplicationConfig(env?: Environment): ToastDeduplicationConfig {
  return getToastConfig(env).deduplication;
}

/**
 * Get only display config
 */
export function getToastDisplayConfig(env?: Environment): ToastDisplayConfig {
  return getToastConfig(env).display;
}