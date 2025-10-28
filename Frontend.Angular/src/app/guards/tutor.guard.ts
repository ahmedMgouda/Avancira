import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';
import { from, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * Tutor Guard (Hybrid Context)
 * - Ensures user is authenticated
 * - Ensures user has 'tutor' role
 * - Silently updates backend if activeProfile is not 'tutor'
 */
export const tutorGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return from(authService.init()).pipe(
    switchMap(() => {
      // 1️⃣ Check authentication
      if (!authService.isAuthenticated()) {
        authService.handleUnauthorized(state.url);
        return of(false);
      }

      const user = authService.currentUser();
      const roles = user?.roles || [];
      const activeProfile = user?.activeProfile;

      // 2️⃣ Must have tutor role
      if (!roles.includes('tutor')) {
        router.navigate(['/'], { queryParams: { error: 'access_denied' } });
        return of(false);
      }

      // 3️⃣ If wrong profile, silently sync backend session
      if (activeProfile !== 'tutor') {
        console.info('[TutorGuard] Switching activeProfile → tutor');
        return authService.switchProfile('tutor').pipe(
          map(() => true),
          catchError(() => of(false)) // simple fail-safe, navigation stops
        );
      }

      // ✅ Already correct
      return of(true);
    }),
    catchError(() => {
      authService.handleUnauthorized(state.url);
      return of(false);
    })
  );
};

export const tutorChildGuard: CanActivateChildFn = (route, state) =>
  tutorGuard(route, state);
