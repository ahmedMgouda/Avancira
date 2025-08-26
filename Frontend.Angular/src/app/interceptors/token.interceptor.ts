import { HttpInterceptorFn } from '@angular/common/http';
import { INCLUDE_CREDENTIALS, SKIP_AUTH } from './auth.interceptor';
import { environment } from '../environments/environment';

/**
 * Ensures OAuth token requests send cookies and skip auth header injection.
 */
export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const isTokenCall = req.url.startsWith(`${environment.baseApiUrl}/connect/token`);
  if (!isTokenCall) {
    return next(req);
  }
  const context = req.context.set(SKIP_AUTH, true).set(INCLUDE_CREDENTIALS, true);
  return next(req.clone({ withCredentials: true, context }));
};
