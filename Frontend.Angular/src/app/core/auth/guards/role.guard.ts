import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { from, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

import { PortalRole } from '../../../layouts/portal/portal.types';

/**
 * ===========================================================
 * RoleGuard (Unified Hybrid Guard)
 * -----------------------------------------------------------
 * • Ensures user is authenticated
 * • Ensures user has the required role
 * • Optionally switches activeProfile (for tutor/student)
 * • Supports admin hasAdminAccess bypass
 * -----------------------------------------------------------
 * Usage example in route:
 *   {
 *     path: 'dashboard',
 *     component: StudentDashboardComponent,
 *     canActivate: [roleGuard],
 *     data: { role: 'student' }
 *   }
 * ===========================================================
 */
export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRole = (route.data['role'] ?? null) as PortalRole | null;

  return from(authService.init()).pipe(
    switchMap(() => {
      if (!authService.isAuthenticated()) {
        authService.handleUnauthorized(state.url);
        return of(false);
      }

      const user = authService.currentUser();
      const roles = user?.roles || [];
      const activeProfile = user?.activeProfile;
      const hasAdminAccess = user?.hasAdminAccess ?? false;

      switch (requiredRole) {
        case 'admin': {
          // Must be admin or haveAdminAccess flag
          if (!roles.includes('admin') && !hasAdminAccess) {
            router.navigate(['/'], { queryParams: { error: 'access_denied' } });
            return of(false);
          }
          return of(true);
        }

        case 'student':
        case 'tutor': {
          // Must have matching role
          if (!roles.includes(requiredRole)) {
            router.navigate(['/'], { queryParams: { error: 'access_denied' } });
            return of(false);
          }

          // If wrong activeProfile, silently switch
          if (activeProfile !== requiredRole) {
            console.info(`[RoleGuard] Switching activeProfile → ${requiredRole}`);
            return authService.switchProfile(requiredRole).pipe(
              map(() => true),
              catchError((err) => {
                console.error(`[RoleGuard] switchProfile failed:`, err);
                return of(false);
              })
            );
          }
          return of(true);
        }

        default:
          // No role requirement, just authenticated
          return of(true);
      }
    }),
    catchError((err) => {
      console.error('[RoleGuard] Error:', err);
      authService.handleUnauthorized(state.url);
      return of(false);
    })
  );
};

export const roleChildGuard: CanActivateChildFn = (route, state) =>
  roleGuard(route, state);
