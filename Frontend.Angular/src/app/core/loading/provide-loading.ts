 /* Provide Loading System
 * ✅ KEEP THIS - Main setup function
 * 
 * What it does:
 * - Configures loading system (debounce, timeouts, limits)
 * - Includes route loading tracking
 * - Provides LOADING_CONFIG token
 * 
 * Usage in app.config.ts:
 *   provideLoading({
 *     debounceDelay: 200,
 *     requestTimeout: 30000,
 *     microtaskBatchThreshold: 5
 *   })
 */

import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';

import { LOADING_CONFIG, LoadingConfig } from './services/loading.service';

import { provideRouteLoading } from './route-loading.initializer';

export function provideLoading(
  config?: Partial<LoadingConfig>
): EnvironmentProviders {
  const defaultConfig: LoadingConfig = {
    debounceDelay: 200,
    requestTimeout: 30000,
    maxRequests: 100,
    maxOperations: 50,
    errorRetentionTime: 5000,
    microtaskBatchThreshold: 5 // ✅ NEW from Phase 3
  };

  const finalConfig = { ...defaultConfig, ...config };

  return makeEnvironmentProviders([
    // ✅ Route loading tracking (automatic progress bar)
    provideRouteLoading(),
    
    // ✅ Configuration
    {
      provide: LOADING_CONFIG,
      useValue: finalConfig
    }
  ]);
}

// ✅ Re-export interceptor for convenience
export { loadingInterceptor } from './interceptors/loading.interceptor';