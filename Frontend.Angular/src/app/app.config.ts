import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideToastr } from 'ngx-toastr';

import { firstValueFrom, catchError, of } from 'rxjs';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { dateInterceptorFn } from './interceptors/dateInterceptorFn';
import { AuthService } from './services/auth.service';

function initAuth(auth: AuthService) {
  return () => firstValueFrom(
    auth.getValidAccessToken().pipe(catchError(() => of(null)))
  );
}

export const appConfig: ApplicationConfig = {
  providers: [
    // Zone change detection optimizations
    provideZoneChangeDetection({ eventCoalescing: true }),

    // Router setup
    provideRouter(routes),

    // HTTP client and interceptors
    provideHttpClient(
      withInterceptors([authInterceptor, dateInterceptorFn])
    ),

    // Toastr configuration
    provideToastr({
      timeOut: 5000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
    }),

    provideAnimationsAsync(),

    { provide: APP_INITIALIZER, useFactory: initAuth, deps: [AuthService], multi: true },
  ]
};
