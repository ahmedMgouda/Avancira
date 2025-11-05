import { ErrorHandler, Provider } from '@angular/core';

import { GlobalErrorHandler } from '../handlers/global-error.handler';

export function provideLogging(): Provider[] {
  return [
    {
      provide: ErrorHandler,
      useClass: GlobalErrorHandler
    }
  ];
}
