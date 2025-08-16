import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

export const AuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return auth.ensureAccessToken().pipe(
    take(1),
    map(ok => ok ? true : router.createUrlTree(['/signin'], { queryParams: { returnUrl: state.url } })),
    catchError(() => {
      auth.logout(false);
      return of(router.createUrlTree(['/signin'], { queryParams: { returnUrl: state.url } }));
    })
  );
};
