// core/loading/providers/loading.provider.ts
/**
 * Loading Provider - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * Configures loading system with route tracking
 * 
 * CHANGES:
 * ✅ Moved from provide-loading.ts to loading.provider.ts
 * ✅ Removed LOADING_CONFIG token (config comes from environment now)
 * ✅ Simplified - only provides route tracking
 */

import { DestroyRef, EnvironmentProviders, inject, makeEnvironmentProviders, provideAppInitializer } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
} from '@angular/router';

import { LoadingService } from '../services/loading.service';

/**
 * Initialize route loading tracking
 */
function initializeRouteLoading() {
  return () => {
    const router = inject(Router);
    const loader = inject(LoadingService);
    const destroyRef = inject(DestroyRef);

    // Subscribe to router events with automatic cleanup
    router.events
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(event => {
        if (event instanceof NavigationStart) {
          // Start loading on navigation
          loader.startRouteLoading(router.url, event.url);
        } else if (
          event instanceof NavigationEnd ||
          event instanceof NavigationCancel ||
          event instanceof NavigationError
        ) {
          // Complete loading when navigation finishes
          loader.completeRouteLoading();
        }
      });
  };
}

/**
 * Provide loading system with route tracking
 * 
 * Usage:
 * ```typescript
 * export const appConfig = {
 *   providers: [
 *     provideLoading()
 *   ]
 * };
 * ```
 */
export function provideLoading(): EnvironmentProviders {
  return makeEnvironmentProviders([
    // Route loading tracking
    provideAppInitializer(initializeRouteLoading())
  ]);
}