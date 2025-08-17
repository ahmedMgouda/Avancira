import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

export const AuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);

  return auth.ensureAccessToken().pipe(
    take(1),
    map(ok => ok ? true : auth.redirectToSignIn(state.url)),
    catchError(() => {
      auth.logout(false);
      return of(auth.redirectToSignIn(state.url));
    })
  );
};
