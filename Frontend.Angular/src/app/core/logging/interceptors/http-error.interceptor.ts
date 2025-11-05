import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ErrorHandlerService } from '../services/error-handler.service';

import { StandardError } from '../models/standard-error.model';

export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const errorHandler = inject(ErrorHandlerService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const standardError: StandardError = errorHandler.handle(error);
      return throwError(() => standardError);
    })
  );
};
