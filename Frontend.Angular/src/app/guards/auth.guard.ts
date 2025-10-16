import { inject } from '@angular/core';
import {
  CanActivateChildFn,
  CanActivateFn,
  CanMatchFn,
} from '@angular/router';
import { from, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * ===========================================================
 * Auth Guards (BFF Edition)
 * -----------------------------------------------------------
 * - Ensure AuthService is initialized before activation.
 * - Redirects unauthenticated users via AuthService.
 * - Keeps no redirect logic inside the guard itself.
 * ===========================================================
 */

export const authGuard: CanActivateFn = (_route, state) =>
  ensureInitializedAndCheck(state.url);

export const authChildGuard: CanActivateChildFn = (_route, state) =>
  ensureInitializedAndCheck(state.url);

export const authMatchGuard: CanMatchFn = (_route, segments) => {
  const currentUrl = '/' + segments.map((s) => s.path).join('/');
  return ensureInitializedAndCheck(currentUrl);
};

function ensureInitializedAndCheck(returnUrl: string) {
  const authService = inject(AuthService);

  return from(authService.init()).pipe(
    map(() => {
      if (authService.isAuthenticated()) return true;
      authService.handleUnauthorized(returnUrl);
      return false;
    }),
    catchError((error) => {
      console.error('[Guard] Initialization error:', error);
      authService.handleUnauthorized(returnUrl);
      return of(false);
    })
  );
}
