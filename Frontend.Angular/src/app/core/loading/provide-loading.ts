import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';

import { LOADING_CONFIG, LoadingConfig } from './loading.service';

import { provideRouteLoading } from './route-loading.initializer';

/**
 * Provide Loading System
 * ═══════════════════════════════════════════════════════════════════════
 * Configures loading tracking without providing HttpClient
 * (since you already have provideHttpClient with interceptors)
 * 
 * Features:
 *   ✅ Route loading tracking
 *   ✅ Configurable settings
 *   ✅ Works with your existing HTTP interceptor pipeline
 * 
 * Usage in app.config.ts:
 *   provideHttpClient(
 *     withInterceptors([
 *       correlationIdInterceptor,
 *       loggingInterceptor,
 *       authInterceptor,
 *       loadingInterceptor,  // ← Keep this in your interceptor chain
 *       retryInterceptor,
 *       errorInterceptor
 *     ])
 *   ),
 *   provideLoading(), // ← Add this
 * 
 * @param config Optional custom configuration
 * @returns Environment providers for the loading system
 */
export function provideLoading(
  config?: Partial<LoadingConfig>
): EnvironmentProviders {
  const defaultConfig: LoadingConfig = {
    debounceDelay: 200,
    requestTimeout: 30000,
    maxRequests: 100,
    maxOperations: 50,
    errorRetentionTime: 5000,
  };

  const finalConfig = { ...defaultConfig, ...config };

  return makeEnvironmentProviders([
    // Route loading tracking
    provideRouteLoading(),
    
    // Configuration
    {
      provide: LOADING_CONFIG,
      useValue: finalConfig
    }
  ]);
}

/**
 * Export loadingInterceptor for use in your interceptor chain
 * Re-export for convenience so you can import from one place
 */
export { loadingInterceptor } from './loading.interceptor';