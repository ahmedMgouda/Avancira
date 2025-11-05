import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { ErrorHandler,Provider } from '@angular/core';

import { GlobalErrorHandler } from '../handlers/global-error.handler';
import { HttpErrorInterceptor } from '../interceptors/http-error.interceptor';
import { HttpLoggingInterceptor } from '../interceptors/http-logging.interceptor';

export function provideLogging(): Provider[] {
  return [
    {
      provide: ErrorHandler,
      useClass: GlobalErrorHandler
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpLoggingInterceptor,
      multi: true
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: HttpErrorInterceptor,
      multi: true
    }
  ];
}
