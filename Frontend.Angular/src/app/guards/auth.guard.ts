import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return auth.ensureAccessToken().pipe(
    map(ok => ok ? true : router.createUrlTree(['/signin'], { queryParams: { returnUrl: state.url } })),
    catchError(() => of(router.createUrlTree(['/signin'], { queryParams: { returnUrl: state.url } })))
  );
};
