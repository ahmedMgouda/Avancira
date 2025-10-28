import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  ApplicationConfig,
  importProvidersFrom,
  inject,
  provideAppInitializer,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { catchError, firstValueFrom, from, of } from 'rxjs';
import { OAuthModule } from 'angular-oauth2-oidc';
import { provideToastr } from 'ngx-toastr';

import { AuthService } from './services/auth.service';
import { ConfigService } from './services/config.service';

import { routes } from './routes/app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';

function initConfig() {
  return firstValueFrom(
    inject(ConfigService).loadConfig().pipe(catchError(() => of(null)))
  );
}

function initAuth() {
  const auth = inject(AuthService);
  return firstValueFrom(
    from(auth.init()).pipe(catchError(() => of(null)))
  );
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideToastr({
      timeOut: 5000,
      positionClass: 'toast-top-right',
      preventDuplicates: true,
    }),
    provideAnimationsAsync(),
    importProvidersFrom(OAuthModule.forRoot()),

    provideAppInitializer(initConfig),
    provideAppInitializer(initAuth),
  ]
};
