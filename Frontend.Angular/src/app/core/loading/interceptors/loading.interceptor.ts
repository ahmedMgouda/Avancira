import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from '../services/loading.service';

import { IdGenerator } from '@/core/utils';


export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  // Skip loading if header is present
  if (req.headers.has('X-Skip-Loading')) {
    return next(req);
  }

  const loader = inject(LoadingService);
  const requestId = IdGenerator.generateRequestId();
  
  loader.startRequest(requestId);

  return next(req).pipe(
    finalize(() => loader.completeRequest(requestId))
  );
};