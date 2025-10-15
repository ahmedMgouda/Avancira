import {
  HttpContextToken,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../environments/environment';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);
export const INCLUDE_CREDENTIALS = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
): Observable<HttpEvent<unknown>> => {
  if (req.context.get(SKIP_AUTH)) {
    return next(req);
  }

  const shouldSendCredentials =
    req.context.get(INCLUDE_CREDENTIALS) || isBffRequest(req.url);

  const request =
    shouldSendCredentials && !req.withCredentials
      ? req.clone({ withCredentials: true })
      : req;

  return next(request);
};

function isBffRequest(url: string): boolean {
  const base = normaliseBaseUrl(environment.baseApiUrl);
  return url.startsWith(base);
}

function normaliseBaseUrl(url: string): string {
  return url.endsWith('/') ? url : `${url}/`;
}
