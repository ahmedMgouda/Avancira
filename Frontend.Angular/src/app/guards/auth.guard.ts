// src/app/guards/auth.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, take } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

export const AuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);

  // Fast path: already authenticated
  if (auth.isAuthenticated()) return true;

  // Do NOT start refresh here. Only wait if one is already running.
  return auth.waitForRefresh().pipe(
    take(1),
    map(() => (auth.isAuthenticated() ? true : auth.redirectToSignIn(state.url))),
    catchError(() => of(auth.redirectToSignIn(state.url)))
  );
};
