import { HttpErrorResponse,HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { inject,Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { ErrorHandlerService } from '../services/error-handler.service';

import { StandardError } from '../models/standard-error.model';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {
  private readonly errorHandler = inject(ErrorHandlerService);
  
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        const standardError: StandardError = this.errorHandler.handle(error);
        return throwError(() => standardError);
      })
    );
  }
}
