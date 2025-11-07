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

import { GlobalErrorHandler } from './core/handlers/global-error.handler';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
import { traceContextInterceptor } from './core/interceptors/trace-context.interceptor';
import { provideLoading } from './core/loading';
import { loadingInterceptor } from './core/loading/loading.interceptor';
import { httpErrorInterceptor } from './core/logging/interceptors/http-error.interceptor';
import { httpLoggingInterceptor } from './core/logging/interceptors/http-logging.interceptor';
import { provideLogging } from './core/logging/providers/logging.providers';
import { NETWORK_STATUS_CONFIG } from './core/network';
import { networkInterceptor } from './core/network/network.interceptor';
import { environment } from './environments/environment';
import { routes } from './routes/app.routes';

function initApp() {
  const initializer = inject(AppInitializerService);
  return initializer.initialize();
}

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

    // ──────────────────────────────────────────────────────────────
    // NETWORK STATUS CONFIGURATION
    // ──────────────────────────────────────────────────────────────
    {
      provide: NETWORK_STATUS_CONFIG,
      useValue: {
        // BFF health endpoint
        healthEndpoint: `https://localhost:9200/health`,

        // Check interval (30s = industry standard)
        checkInterval: 30000,

        // Max retry attempts for health checks
        maxAttempts: 3
      }
    },

    // ═══════════════════════════════════════════════════════════
    // Core Systems
    // ═══════════════════════════════════════════════════════════
    provideLoading(),
    ...provideLogging(),

    // ═══════════════════════════════════════════════════════════
    // Error Handling & Initialization
    // ═══════════════════════════════════════════════════════════
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(initApp)
  ]
};
