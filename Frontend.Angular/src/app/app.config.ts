// app.config.ts - CORRECTED VERSION
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  ErrorHandler,
  inject,
  provideAppInitializer,
  provideExperimentalZonelessChangeDetection
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { AppInitializerService } from './core/services/app-initializer.service';

// ═══════════════════════════════════════════════════════════════════
// ✅ CORRECTED IMPORTS - Interceptors
// ═══════════════════════════════════════════════════════════════════
// HTTP Interceptors (core/http/interceptors/)
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { GlobalErrorHandler } from './core/handlers/global-error.handler';
import { retryInterceptor } from './core/http/interceptors/retry.interceptor';
import { traceContextInterceptor } from './core/http/interceptors/trace-context.interceptor';
// ═══════════════════════════════════════════════════════════════════
// ✅ CORRECTED IMPORTS - Providers
// ═══════════════════════════════════════════════════════════════════
// Loading System
import { provideLoading } from './core/loading';
// Loading Interceptor (core/loading/interceptors/)
import { loadingInterceptor } from './core/loading/interceptors/loading.interceptor';
// Logging Interceptors (core/logging/interceptors/)
import { httpErrorInterceptor } from './core/logging/interceptors/http-error.interceptor';
import { httpLoggingInterceptor } from './core/logging/interceptors/http-logging.interceptor';
// Logging System
import { provideLogging } from './core/logging/providers/logging.providers';
// Network Configuration
import { NETWORK_STATUS_CONFIG } from './core/network';
// Network Interceptor (core/network/interceptors/)
import { networkInterceptor } from './core/network/interceptors/network.interceptor';
// Routes
import { routes } from './routes/app.routes';

// ═══════════════════════════════════════════════════════════════════
// App Initializer
// ═══════════════════════════════════════════════════════════════════

function initApp() {
  const initializer = inject(AppInitializerService);
  return initializer.initialize();
}

// ═══════════════════════════════════════════════════════════════════
// Application Configuration
// ═══════════════════════════════════════════════════════════════════

export const appConfig: ApplicationConfig = {
  providers: [
    // ═══════════════════════════════════════════════════════════
    // Change Detection (Zoneless)
    // ═══════════════════════════════════════════════════════════
    provideExperimentalZonelessChangeDetection(),

    // ═══════════════════════════════════════════════════════════
    // Router & Animations
    // ═══════════════════════════════════════════════════════════
    provideRouter(routes),
    provideAnimationsAsync(),

    // ═══════════════════════════════════════════════════════════
    // HTTP Client with Interceptor Pipeline (ORDER MATTERS!)
    // ═══════════════════════════════════════════════════════════
    provideHttpClient(
      withInterceptors([
        traceContextInterceptor, // 1️⃣ Trace Context - W3C trace headers
        httpLoggingInterceptor,  // 2️⃣ Logging - respects X-Skip-Logging
        authInterceptor,         // 3️⃣ Auth - attach token
        loadingInterceptor,      // 4️⃣ Loading - respects X-Skip-Loading
        networkInterceptor,      // 5️⃣ Network - must come before retry
        retryInterceptor,        // 6️⃣ Retry - exponential backoff
        httpErrorInterceptor     // 7️⃣ Error Handling - log errors
      ])
    ),

    // ═══════════════════════════════════════════════════════════
    // Network Status Configuration
    // ═══════════════════════════════════════════════════════════
    {
      provide: NETWORK_STATUS_CONFIG,
      useValue: {
        healthEndpoint: 'https://localhost:9200/health',
        checkInterval: 30000,  // 30 seconds
        maxAttempts: 1
      }
    },

    // ═══════════════════════════════════════════════════════════
    // Core Systems
    // ═══════════════════════════════════════════════════════════
    
    // Loading System (includes route tracking + HTTP tracking)
    provideLoading({
      debounceDelay: 200,
      requestTimeout: 30000,
      maxRequests: 100,
      maxOperations: 50,
      errorRetentionTime: 5000,
      microtaskBatchThreshold: 5  // ✅ NEW from Phase 3
    }),
    
    // Logging System
    ...provideLogging(),

    // ═══════════════════════════════════════════════════════════
    // Error Handling & Initialization
    // ═══════════════════════════════════════════════════════════
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(initApp)
  ]
};