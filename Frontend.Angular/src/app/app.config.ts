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
// Import interceptors in correct order
import { traceContextInterceptor } from './core/interceptors/trace-context.interceptor';
// Import providers
import { provideLoading } from './core/loading';
import { loadingInterceptor } from './core/loading/loading.interceptor';
import { httpErrorInterceptor } from './core/logging/interceptors/http-error.interceptor';
import { httpLoggingInterceptor } from './core/logging/interceptors/http-logging.interceptor';
import { provideLogging } from './core/logging/providers/logging.providers';
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
        // 1️⃣ Trace Context - Add W3C trace headers to ALL requests
        traceContextInterceptor,
        
        // 2️⃣ HTTP Logging - Log requests (respects X-Skip-Logging)
        httpLoggingInterceptor,
        
        // 3️⃣ Auth - Add authentication token
        authInterceptor,
        
        // 4️⃣ Loading - Track loading state (respects X-Skip-Loading)
        loadingInterceptor,
        
        // 5️⃣ Retry - Handle failures with exponential backoff (respects X-Skip-Retry)
        retryInterceptor,
        
        // 6️⃣ Error Handling - Log errors, mark as __logged (respects X-Skip-Logging)
        httpErrorInterceptor
      ])
    ),
    
    // ═══════════════════════════════════════════════════════════
    // Core Systems
    // ═══════════════════════════════════════════════════════════
    provideLoading(),
    ...provideLogging(),
    
    // ═══════════════════════════════════════════════════════════
    // Error Handling & Initialization
    // ═══════════════════════════════════════════════════════════
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(initApp),
  ]
};