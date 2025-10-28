import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { from, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * Admin Guard (Hybrid Context)
 * - Ensures user is authenticated
 * - Ensures user has 'admin' role OR hasAdminAccess === true
 * - Independent of activeProfile
 */
export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return from(authService.init()).pipe(
    switchMap(() => {
      // 1️⃣ Auth check
      if (!authService.isAuthenticated()) {
        authService.handleUnauthorized(state.url);
        return of(false);
      }

      const user = authService.currentUser();
      const roles = user?.roles || [];
      const hasAdminAccess = user?.hasAdminAccess ?? false;

      // 2️⃣ Must have admin role or flag
      if (!roles.includes('admin') && !hasAdminAccess) {
        console.warn('[AdminGuard] User lacks admin access');
        router.navigate(['/'], { queryParams: { error: 'access_denied' } });
        return of(false);
      }

      // ✅ All checks passed
      return of(true);
    }),
    catchError((err) => {
      console.error('[AdminGuard] Error:', err);
      authService.handleUnauthorized(state.url);
      return of(false);
    })
  );
};

export const adminChildGuard: CanActivateChildFn = (route, state) =>
  adminGuard(route, state);
