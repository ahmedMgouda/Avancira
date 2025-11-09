// app.config.ts - UPDATED
/**
 * Application Configuration - UPDATED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES:
 * ✅ Merged HTTP logging interceptors (removed http-error.interceptor)
 * ✅ Updated loading provider to use new config
 * ✅ Added network config provider
 */

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

// Auth
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { NETWORK_CONFIG } from './core/config/network.config';
import { getNetworkConfig } from './core/config/network.config';
// Error Handling
import { GlobalErrorHandler } from './core/handlers/global-error.handler';
// HTTP
import { retryInterceptor } from './core/http/interceptors/retry.interceptor';
import { traceContextInterceptor } from './core/http/interceptors/trace-context.interceptor';
// Loading
import { provideLoading } from './core/loading';
import { loadingInterceptor } from './core/loading/interceptors/loading.interceptor';
// Logging
import { httpLoggingInterceptor } from './core/logging/interceptors/http-logging.interceptor';
import { provideLogging } from './core/logging/providers/logging.providers';
// Network
import { networkInterceptor } from './core/network/interceptors/network.interceptor';
// Routes
import { routes } from './routes/app.routes';

// App Initializer
function initApp() {
  const initializer = inject(AppInitializerService);
  return initializer.initialize();
}

export const appConfig: ApplicationConfig = {
  providers: [
    // Change Detection
    provideExperimentalZonelessChangeDetection(),

    // Router & Animations
    provideRouter(routes),
    provideAnimationsAsync(),

    // HTTP Client with Interceptor Pipeline
    provideHttpClient(
      withInterceptors([
        traceContextInterceptor,  // 1️⃣ Trace Context
        authInterceptor,           // 2️⃣ Auth (moved before logging)
        httpLoggingInterceptor,    // 3️⃣ Logging (merged success + error)
        loadingInterceptor,        // 4️⃣ Loading
        networkInterceptor,        // 5️⃣ Network
        retryInterceptor           // 6️⃣ Retry (error handling last)
      ])
    ),

    // Network Configuration
    {
      provide: NETWORK_CONFIG,
      useFactory: getNetworkConfig
    },

    // Loading System (with new config)
    provideLoading(),

    // Logging System
    ...provideLogging(),

    // Error Handling
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    
    // App Initialization
    provideAppInitializer(initApp)
  ]
};