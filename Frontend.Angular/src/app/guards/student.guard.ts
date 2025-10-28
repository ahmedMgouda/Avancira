import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { from, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * ===========================================================
 * Student Guard (Hybrid Context)
 * -----------------------------------------------------------
 * • Ensures user is authenticated
 * • Ensures user has 'student' role
 * • If activeProfile !== 'student', silently syncs backend
 * ===========================================================
 */
export const studentGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Use RxJS pipeline to support async profile switching
  return from(authService.init()).pipe(
    switchMap(() => {
      // 1️⃣ Ensure authenticated
      if (!authService.isAuthenticated()) {
        authService.handleUnauthorized(state.url);
        return of(false);
      }

      const user = authService.currentUser();
      const roles = user?.roles || [];
      const activeProfile = user?.activeProfile;

      // 2️⃣ Ensure user has 'student' role
      if (!roles.includes('student')) {
        console.warn('[StudentGuard] User lacks student role');
        router.navigate(['/'], { queryParams: { error: 'access_denied' } });
        return of(false);
      }

      // 3️⃣ If wrong profile → silently sync backend session
      if (activeProfile !== 'student') {
        console.info('[StudentGuard] Switching activeProfile → student');
        return authService.switchProfile('student').pipe(
          map(() => true),
          catchError((err) => {
            console.error('[StudentGuard] switchProfile failed:', err);
            return of(false);
          })
        );
      }

      // ✅ Already in correct mode
      return of(true);
    }),
    catchError((err) => {
      console.error('[StudentGuard] Error:', err);
      authService.handleUnauthorized(state.url);
      return of(false);
    })
  );
};

// Child routes use same logic
export const studentChildGuard: CanActivateChildFn = (route, state) => {
  return studentGuard(route, state);
};
