// core/config/environment.config.ts
/**
 * Environment Configuration Helper
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized environment detection and type-safe config access
 */

import { environment } from '../../environments/environment';

export type Environment = 'dev' | 'staging' | 'prod';

/**
 * Get current environment
 */
export function getCurrentEnvironment(): Environment {
  if (environment.production) {
    return 'prod';
  }
  
  // Check for staging flag (add to your environment.staging.ts)
  if ((environment as any).staging) {
    return 'staging';
  }
  
  return 'dev';
}

/**
 * Check if current environment matches
 */
export function isEnvironment(env: Environment): boolean {
  return getCurrentEnvironment() === env;
}

/**
 * Check if production
 */
export function isProduction(): boolean {
  return getCurrentEnvironment() === 'prod';
}

/**
 * Check if development
 */
export function isDevelopment(): boolean {
  return getCurrentEnvironment() === 'dev';
}