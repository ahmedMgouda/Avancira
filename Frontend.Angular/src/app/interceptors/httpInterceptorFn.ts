import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, Observable, switchMap, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';


export const httpInterceptorFn: HttpInterceptorFn = (
  req: HttpRequest<any>,
  next: HttpHandlerFn
): Observable<HttpEvent<any>> => {
  // console.log('Intercepted request:', req);
  const router = inject(Router);
  const authService = inject(AuthService);
  
  // Add the Authorization header if the token exists
  const token = authService.getToken();
  if (token) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  // Intercept the response and map to `response.data` if applicable
  return next(req).pipe(
    map((event: HttpEvent<any>) => {
      if (event instanceof HttpResponse && event.body && event.body.data !== undefined) {
        // console.log('Intercepted response body:', event.body);
        // Map the response body to `response.data`
        return event.clone({ body: event.body.data });
      }
      return event;
    }),
    catchError((error: HttpErrorResponse) => {
      console.error('HTTP Error:', error);

      if (error.status === 401) {
        const refreshToken = authService.getRefreshToken();
        const token = authService.getToken();
        if (refreshToken && token) {
          return authService.refreshAuthToken(token, refreshToken).pipe(
            switchMap(res => {
              authService.saveToken(res.token);
              authService.saveRefreshToken(res.refreshToken);
              const retryReq = req.clone({
                setHeaders: { Authorization: `Bearer ${res.token}` },
              });
              return next(retryReq);
            }),
            catchError(refreshError => {
              authService.logout();
              router.navigate(['/signin']);
              return throwError(() => refreshError);
            })
          );
        } else {
          authService.logout();
          router.navigate(['/signin']);
        }
      }

      return throwError(() => error);
    })
  );
};
