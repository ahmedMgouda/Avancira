// src/app/guards/auth.guard.ts
import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import {
  CanActivateChildFn,
  CanActivateFn, 
  CanMatchFn,
  Router,
  UrlTree
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map, switchMap, take } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * Main authentication guard (for routes requiring login).
 */
export const authGuard: CanActivateFn = (route, state) =>
  runAuthCheck(state.url);

/**
 * Same logic for child routes.
 */
export const authChildGuard: CanActivateChildFn = (route, state) =>
  runAuthCheck(state.url);

/**
 * Same logic for CanMatch.
 */
export const authMatchGuard: CanMatchFn = (_route, segments) => {
  const currentUrl = '/' + segments.map(s => s.path).join('/');
  return runAuthCheck(currentUrl);
};

/**
 * Role-based guard.
 */
export const roleGuard = (requiredRoles: string[]): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return runAuthCheck(state.url).pipe(
      switchMap(result => {
        if (result !== true) return of(result);

        const user = authService.currentUser();
        if (!user) {
          return of(createSignInRedirect(router, state.url));
        }

        if (!hasRequiredRole(user.roles, requiredRoles)) {
          return of(createAccessDeniedRedirect(router, state.url));
        }

        return of(true);
      })
    );
  };
};

/**
 * Permission-based guard.
 */
export const permissionGuard = (requiredPermissions: string[]): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return runAuthCheck(state.url).pipe(
      switchMap(result => {
        if (result !== true) return of(result);

        const user = authService.currentUser();
        if (!user) {
          return of(createSignInRedirect(router, state.url));
        }

        if (!hasRequiredPermissions(user.permissions, requiredPermissions)) {
          return of(createAccessDeniedRedirect(router, state.url));
        }

        return of(true);
      })
    );
  };
};

/**
 * Combined role + permission guard.
 */
export const rolePermissionGuard = (
  requiredRoles: string[], 
  requiredPermissions: string[]
): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return runAuthCheck(state.url).pipe(
      switchMap(result => {
        if (result !== true) return of(result);

        const user = authService.currentUser();
        if (!user) {
          return of(createSignInRedirect(router, state.url));
        }

        const hasRole = hasRequiredRole(user.roles, requiredRoles);
        const hasPerm = hasRequiredPermissions(user.permissions, requiredPermissions);

        if (!hasRole || !hasPerm) {
          return of(createAccessDeniedRedirect(router, state.url));
        }

        return of(true);
      })
    );
  };
};

/**
 * Guard for anonymous-only routes (e.g. login/register).
 */
export const anonymousGuard: CanActivateFn = (route, _state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    const redirectUrl = route.queryParams?.['redirect'] || '/dashboard';
    return router.createUrlTree([redirectUrl]);
  }

  return true;
};

/* -------------------------------------------------------------------------- */
/* 🔑 Core authentication check logic                                         */
/* -------------------------------------------------------------------------- */

function runAuthCheck(returnUrl: string): Observable<boolean | UrlTree> {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Fast path
  if (authService.isAuthenticated()) return of(true);

  // Wait until authentication state resolves (reactive signals → observable)
  return toObservable(authService.isAuthenticated).pipe(
    take(1),
    map(isAuth => (isAuth ? true : createSignInRedirect(router, returnUrl))),
    catchError(() => of(createSignInRedirect(router, returnUrl)))
  );
}

/* -------------------------------------------------------------------------- */
/* 🔑 Redirect helpers                                                        */
/* -------------------------------------------------------------------------- */

function createSignInRedirect(router: Router, returnUrl: string): UrlTree {
  const safeUrl = sanitizeReturnUrl(returnUrl);
  return router.createUrlTree(['/signin'], { queryParams: { returnUrl: safeUrl } });
}

function createAccessDeniedRedirect(router: Router, returnUrl: string): UrlTree {
  return router.createUrlTree(['/access-denied'], { queryParams: { returnUrl } });
}

function sanitizeReturnUrl(url: string): string {
  try {
    if (url.startsWith('/')) return url;
    const parsed = new URL(url, window.location.origin);
    if (parsed.origin === window.location.origin) {
      return parsed.pathname + parsed.search + parsed.hash;
    }
    console.warn('⚠️ Suspicious return URL blocked:', url);
    return '/';
  } catch {
    console.warn('⚠️ Invalid return URL, defaulting to /');
    return '/';
  }
}

/* -------------------------------------------------------------------------- */
/* 🔑 Role & permission checks                                                */
/* -------------------------------------------------------------------------- */

function hasRequiredRole(userRoles: string[], requiredRoles: string[]): boolean {
  if (!requiredRoles?.length) return true;
  if (!userRoles?.length) return false;
  const set = new Set(userRoles.map(r => r.toLowerCase()));
  return requiredRoles.some(r => set.has(r.toLowerCase()));
}

function hasRequiredPermissions(userPermissions: string[], requiredPermissions: string[]): boolean {
  if (!requiredPermissions?.length) return true;
  if (!userPermissions?.length) return false;
  const set = new Set(userPermissions.map(p => p.toLowerCase()));
  return requiredPermissions.every(p => set.has(p.toLowerCase()));
}

/* -------------------------------------------------------------------------- */
/* 🔑 Convenience helpers                                                     */
/* -------------------------------------------------------------------------- */

export function isAdmin(userRoles: string[]): boolean {
  return hasRequiredRole(userRoles, ['admin', 'administrator', 'super_admin']);
}

export function isModerator(userRoles: string[]): boolean {
  return hasRequiredRole(userRoles, ['moderator', 'mod']);
}

export function getHighestRole(userRoles: string[]): string | null {
  if (!userRoles?.length) return null;
  const roleHierarchy = [
    'super_admin',
    'admin',
    'administrator',
    'moderator',
    'mod',
    'editor',
    'author',
    'contributor',
    'user'
  ];
  for (const role of roleHierarchy) {
    if (userRoles.some(r => r.toLowerCase() === role.toLowerCase())) {
      return role;
    }
  }
  return userRoles[0];
}
