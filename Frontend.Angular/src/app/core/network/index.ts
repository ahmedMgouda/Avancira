/**
 * Network Module Public API
 * ═══════════════════════════════════════════════════════════════════════
 * Comprehensive network resilience module for monolithic backends
 * 
 * Usage:
 *   import { 
 *     NetworkStatusService, 
 *     NetworkErrorTracker,
 *     networkInterceptor,
 *     retryInterceptor
 *   } from '@core/network';
 * 
 * Configuration (app.config.ts):
 *   providers: [
 *     provideHttpClient(
 *       withInterceptors([
 *         traceContextInterceptor,  // Add trace headers
 *         networkInterceptor,       // Detect network issues
 *         retryInterceptor          // Retry with backoff
 *       ])
 *     ),
 *     {
 *       provide: NETWORK_STATUS_CONFIG,
 *       useValue: {
 *         healthEndpoint: environment.bffUrl + '/health',
 *         checkInterval: 30000,
 *         maxAttempts: 3
 *       }
 *     }
 *   ]
 * 
 * Features:
 *   ✅ Network status monitoring with /health checks
 *   ✅ Network error tracking with signals
 *   ✅ Automatic retries with exponential backoff
 *   ✅ Smart error classification (only retry transient failures)
 *   ✅ Toast notifications for offline/online
 *   ✅ W3C trace context support
 * 
 * REMOVED:
 *   ❌ Circuit breaker (not needed for monolithic backends)
 *   ❌ Per-domain isolation (single backend = single fate)
 */

// Models
export * from './health-check.model';

// Configuration
export * from './network-status.config';

// Services
export * from './network-error-tracker.service';
export * from './network-status.service';

// Interceptors
export * from './network.interceptor';

// Note: retryInterceptor is exported from @core/interceptors
// to avoid circular dependencies with ResilienceService