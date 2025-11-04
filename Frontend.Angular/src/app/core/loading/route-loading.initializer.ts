import { DestroyRef, inject, provideAppInitializer } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
} from '@angular/router';

import { LoadingService } from './loading.service';

/**
 * Route Loading Initializer (Zoneless Compatible)
 * ═══════════════════════════════════════════════════════════════════════
 * Automatically tracks route navigation and updates loading state
 * 
 * Features:
 *   ✅ Automatic route change detection
 *   ✅ Loading state for navigation transitions
 *   ✅ Proper cleanup on app destroy
 *   ✅ Zoneless compatible
 * 
 * Usage:
 *   Add to app.config.ts providers:
 *   export const appConfig: ApplicationConfig = {
 *     providers: [
 *       provideRouter(routes),
 *       provideRouteLoading(),
 *       // ... other providers
 *     ]
 *   };
 * 
 * @example
 * provideRouteLoading()
 */
export function provideRouteLoading() {
  return provideAppInitializer(() => {
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
  });
}