import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideToastr } from 'ngx-toastr';

import { routes } from './app.routes';
import { dateInterceptorFn } from './interceptors/dateInterceptorFn';
import { httpInterceptorFn } from './interceptors/httpInterceptorFn';

export const appConfig: ApplicationConfig = {
  providers: [
    // Zone change detection optimizations
    provideZoneChangeDetection({ eventCoalescing: true }),

    // Router setup
    provideRouter(routes),

    // HTTP client and interceptors
    provideHttpClient(
      withInterceptors([httpInterceptorFn, dateInterceptorFn])
    ),

    // Toastr configuration
    provideToastr({
      timeOut: 5000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
    }),

    provideAnimationsAsync(),
  ]
};
