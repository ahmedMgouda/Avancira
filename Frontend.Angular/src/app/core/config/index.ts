// core/config/index.ts
/**
 * Configuration Module - Centralized Access
 * ═══════════════════════════════════════════════════════════════════════
 * Single source of truth for all application configuration
 * 
 * BENEFITS:
 * ✅ No duplicate getCurrentEnvironment() calls
 * ✅ Single import path
 * ✅ Type-safe config access
 * ✅ Easy to test/mock
 */

// ─────────────────────────────────────────────────────────────────────
// Base Environment Detection (used by all configs)
// ─────────────────────────────────────────────────────────────────────
export { 
  type Environment,
  getCurrentEnvironment,
  isDevelopment,
  isEnvironment,
  isProduction} from './environment.config';

// ─────────────────────────────────────────────────────────────────────
// Feature Configs
// ─────────────────────────────────────────────────────────────────────
export {
  type DeduplicationConfig,
  getDeduplicationConfig,
  isLoggingDeduplicationEnabled,
  isNetworkErrorDeduplicationEnabled,
  isToastDeduplicationEnabled,
  type ServiceDedupConfig
} from './deduplication.config';
export {
  getLoadingConfig,
  type LoadingConfig
} from './loading.config';
export {
  getLoggingConfig,
  isConsoleLoggingEnabled,
  isRemoteLoggingEnabled,
  type LoggingConfig
} from './logging.config';
export {
  getNetworkConfig,
  NETWORK_CONFIG,
  type NetworkConfig
} from './network.config';
export {
  getSamplingConfig,
  getSamplingRate,
  isSamplingEnabled,
  type SamplingRateConfig
} from './sampling.config';

// ─────────────────────────────────────────────────────────────────────
// Usage Examples
// ─────────────────────────────────────────────────────────────────────
/**
 * In services:
 * ```typescript
 * import { getLoggingConfig, getCurrentEnvironment } from '@core/config';
 * 
 * const config = getLoggingConfig();
 * const env = getCurrentEnvironment();
 * ```
 * 
 * In app.config.ts:
 * ```typescript
 * import { NETWORK_CONFIG, getNetworkConfig } from '@core/config';
 * 
 * providers: [
 *   { provide: NETWORK_CONFIG, useFactory: getNetworkConfig }
 * ]
 * ```
 */