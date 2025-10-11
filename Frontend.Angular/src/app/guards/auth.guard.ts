import { inject } from '@angular/core';
import {
  CanActivateChildFn,
  CanActivateFn,
  CanMatchFn,
  Router,
  UrlTree,
} from '@angular/router';
import { Observable, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { AuthService } from '../services/auth.service';

/**
 * Main authentication guard - redirects to signin if not authenticated
 */
export const authGuard: CanActivateFn = (_route, state) =>
  checkAuthentication(state.url);

/**
 * Guard for child routes
 */
export const authChildGuard: CanActivateChildFn = (_route, state) =>
  checkAuthentication(state.url);

/**
 * Guard for CanMatch (route matching)
 */
export const authMatchGuard: CanMatchFn = (_route, segments) => {
  const currentUrl = '/' + segments.map((s) => s.path).join('/');
  return checkAuthentication(currentUrl);
};

/**
 * Role-based guard: user must have at least one required role
 */
export const roleGuard = (requiredRoles: string[]): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return checkAuthentication(state.url).pipe(
      switchMap((isAuth) => {
        if (isAuth !== true) return of(isAuth);

        const userRoles = authService.roles();
        if (hasAnyRole(userRoles, requiredRoles)) {
          return of(true);
        }

        return of(createAccessDeniedRedirect(router, state.url));
      })
    );
  };
};

/**
 * Permission-based guard: user must have all required permissions
 */
export const permissionGuard = (requiredPermissions: string[]): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return checkAuthentication(state.url).pipe(
      switchMap((isAuth) => {
        if (isAuth !== true) return of(isAuth);

        const userPerms = authService.permissions();
        if (hasAllPermissions(userPerms, requiredPermissions)) {
          return of(true);
        }

        return of(createAccessDeniedRedirect(router, state.url));
      })
    );
  };
};

/**
 * Combined role + permission guard
 */
export const rolePermissionGuard = (
  requiredRoles: string[],
  requiredPermissions: string[]
): CanActivateFn => {
  return (_route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return checkAuthentication(state.url).pipe(
      switchMap((isAuth) => {
        if (isAuth !== true) return of(isAuth);

        const userRoles = authService.roles();
        const userPerms = authService.permissions();

        const hasRole = hasAnyRole(userRoles, requiredRoles);
        const hasPerms = hasAllPermissions(userPerms, requiredPermissions);

        if (!hasRole || !hasPerms) {
          return of(createAccessDeniedRedirect(router, state.url));
        }

        return of(true);
      })
    );
  };
};

/**
 * Guard for anonymous-only routes (login, register)
 * Redirects authenticated users to dashboard
 */
export const anonymousGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return of(router.createUrlTree(['/dashboard']));
  }

  return of(true);
};

/* ---------- Helper Functions ---------- */

/**
 * Core authentication check
 * Returns true if authenticated, UrlTree redirect if not
 */
function checkAuthentication(
  returnUrl: string
): Observable<boolean | UrlTree> {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return of(true);
  }

  return of(createSignInRedirect(router, returnUrl));
}

/**
 * Create signin redirect with return URL
 */
function createSignInRedirect(router: Router, returnUrl: string): UrlTree {
  const safeUrl = sanitizeReturnUrl(returnUrl);
  return router.createUrlTree(['/signin'], {
    queryParams: { returnUrl: safeUrl },
  });
}

/**
 * Create access denied redirect
 */
function createAccessDeniedRedirect(router: Router, returnUrl: string): UrlTree {
  return router.createUrlTree(['/access-denied'], {
    queryParams: { returnUrl },
  });
}

/**
 * Validate URL to prevent open redirect attacks
 */
function sanitizeReturnUrl(url: string): string {
  try {
    if (!url || url === '/') return '/';
    if (url.startsWith('/') && !url.startsWith('//')) return url;
    const parsed = new URL(url, window.location.origin);
    if (parsed.origin === window.location.origin) {
      return parsed.pathname + parsed.search + parsed.hash;
    }
    return '/';
  } catch {
    return '/';
  }
}

/**
 * Check if user has at least one required role (case-insensitive)
 */
function hasAnyRole(userRoles: string[], requiredRoles: string[]): boolean {
  if (!requiredRoles?.length) return true;
  if (!userRoles?.length) return false;
  const lowerUserRoles = userRoles.map((r) => r.toLowerCase());
  return requiredRoles.some((r) => lowerUserRoles.includes(r.toLowerCase()));
}

/**
 * Check if user has all required permissions (case-insensitive)
 */
function hasAllPermissions(
  userPerms: string[],
  requiredPerms: string[]
): boolean {
  if (!requiredPerms?.length) return true;
  if (!userPerms?.length) return false;
  const lowerUserPerms = userPerms.map((p) => p.toLowerCase());
  return requiredPerms.every((p) => lowerUserPerms.includes(p.toLowerCase()));
}

/* ---------- Convenience Helpers ---------- */

/**
 * Check if user is admin
 */
export function isAdmin(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, ['admin', 'administrator', 'super_admin']);
}

/**
 * Check if user is moderator
 */
export function isModerator(userRoles: string[]): boolean {
  return hasAnyRole(userRoles, ['moderator', 'mod']);
}

/**
 * Get highest role in hierarchy
 */
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
    'user',
  ];
  for (const role of roleHierarchy) {
    if (userRoles.some((r) => r.toLowerCase() === role.toLowerCase())) {
      return role;
    }
  }
  return userRoles[0];
}
