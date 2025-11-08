/**
 * Route Loading Initializer
 * ✅ KEEP THIS - Provides automatic route navigation tracking
 * 
 * What it does:
 * - Listens to Angular Router events
 * - Shows progress bar when navigation starts
 * - Hides progress bar when navigation completes
 * - Works with <app-top-progress-bar />
 */

import { DestroyRef, inject, provideAppInitializer } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
} from '@angular/router';

import { LoadingService } from './services/loading.service';

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
          // ✅ Start loading on navigation
          loader.startRouteLoading(router.url, event.url);
        } else if (
          event instanceof NavigationEnd ||
          event instanceof NavigationCancel ||
          event instanceof NavigationError
        ) {
          // ✅ Complete loading when navigation finishes
          loader.completeRouteLoading();
        }
      });
  });
}