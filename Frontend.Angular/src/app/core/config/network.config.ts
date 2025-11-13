// core/config/network.config.ts
/**
 * Network Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Environment-aware network monitoring configuration
 */

import { InjectionToken } from '@angular/core';

import { type Environment, getCurrentEnvironment } from './environment.config';

export interface NetworkConfig {
  healthEndpoint: string;
  checkInterval: number;
  maxAttempts: number;
}

const CONFIG_BY_ENV: Record<Environment, NetworkConfig> = {
  dev: {
    healthEndpoint: 'https://localhost:9200/health',
    checkInterval: 30000,    // ✅ CHANGED: 30 seconds (aligned with prod)
    maxAttempts: 2           // ✅ CHANGED: 2 attempts for faster feedback
  },

  prod: {
    healthEndpoint: 'https://api.avancira.com/health',
    checkInterval: 30000,    // 30 seconds
    maxAttempts: 2           // ✅ CHANGED: 2 attempts for faster feedback
  }
};

export function getNetworkConfig(env?: Environment): NetworkConfig {
  const currentEnv = env || getCurrentEnvironment();
  return CONFIG_BY_ENV[currentEnv];
}

/**
 * Injection token for NetworkConfig
 */
export const NETWORK_CONFIG = new InjectionToken<NetworkConfig>(
  'NETWORK_CONFIG',
  {
    providedIn: 'root',
    factory: () => getNetworkConfig()
  }
);