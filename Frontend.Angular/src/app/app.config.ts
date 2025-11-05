import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  //ErrorHandler,
  inject,
  provideAppInitializer,
  provideExperimentalZonelessChangeDetection // ← Changed for zoneless
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { AppInitializerService } from './core/services/app-initializer.service';

//import { GlobalErrorHandler } from './core/handlers/global-error.handler';
// Import interceptors
import { authInterceptor } from './core/interceptors/auth.interceptor';
// import { correlationIdInterceptor } from './core/interceptors/correlation-id.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';
// Import loading system (encapsulated)
import { loadingInterceptor, provideLoading } from './core/loading';
import { httpErrorInterceptor } from './core/logging/interceptors/http-error.interceptor';
import { httpLoggingInterceptor } from './core/logging/interceptors/http-logging.interceptor';
import { provideLogging } from './core/logging/providers/logging.providers';
//import { networkInterceptor } from './core/network/network.interceptor';
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
    // HTTP Client with Interceptor Pipeline
    // ═══════════════════════════════════════════════════════════
    provideHttpClient(
      withInterceptors([
        //correlationIdInterceptor, // 1. Add correlation ID first
        httpLoggingInterceptor,    // 2. Log requests
        authInterceptor,           // 3. Add auth token
        loadingInterceptor,        // 4. Track loading state
       // networkInterceptor,      // ← Optional: Network status interceptor
        retryInterceptor,          // 5. Retry failed requests
        httpErrorInterceptor       // 6. Handle errors last
      ])
    ),
    
    // ═══════════════════════════════════════════════════════════
    // Loading System (Route tracking + Config)
    // ═══════════════════════════════════════════════════════════
    provideLoading(), // ← One line! Handles route loading + config
    
    // Optional: Custom loading configuration
    // provideLoading({
    //   debounceDelay: 100,
    //   requestTimeout: 60000,
    //   maxRequests: 200
    // }),
    
    ...provideLogging(),

    // ═══════════════════════════════════════════════════════════
    // Error Handling & Initialization
    // ═══════════════════════════════════════════════════════════
    //{ provide: ErrorHandler, useClass: GlobalErrorHandler },
    provideAppInitializer(initApp),
  ]
};