import { InjectionToken } from '@angular/core';

/**
 * Configuration for Network Status Service
 */
export interface NetworkStatusConfig {
  /** 
   * BFF Health endpoint URL
   * Examples:
   *   - Development:  '/health' or 'http://localhost:5000/health'
   *   - Staging:      'https://api-staging.example.com/health'
   *   - Production:   'https://api.example.com/health'
   */
  healthEndpoint?: string;
  
  /** 
   * Check interval in milliseconds
   * Default: 30000 (30 seconds)
   * Kubernetes: 10000, AWS ELB: 30000
   */
  checkInterval?: number;
  
  /** 
   * Maximum retry attempts before declaring offline
   * Default: 3
   */
  maxAttempts?: number;
}

/**
 * Configuration token for Network Status Service
 * Allows environment-specific configuration of health endpoint
 */
export const NETWORK_STATUS_CONFIG = new InjectionToken<NetworkStatusConfig>(
  'NETWORK_STATUS_CONFIG',
  {
    providedIn: 'root',
    factory: () => ({
      healthEndpoint: 'https://localhost:9200/health',
      checkInterval: 30000,
      maxAttempts: 1
    })
  }
);

