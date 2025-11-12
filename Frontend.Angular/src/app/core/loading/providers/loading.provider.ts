import { 
  DestroyRef, 
  EnvironmentProviders, 
  inject, 
  makeEnvironmentProviders, 
  provideAppInitializer 
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
} from '@angular/router';

import { LoadingService } from '../services/loading.service';

function initializeRouteLoading() {
  return () => {
    const router = inject(Router);
    const loader = inject(LoadingService);
    const destroyRef = inject(DestroyRef);

    router.events
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(event => {
        if (event instanceof NavigationStart) {
          loader.startRouteLoading(router.url, event.url);
        } else if (
          event instanceof NavigationEnd ||
          event instanceof NavigationCancel ||
          event instanceof NavigationError
        ) {
          loader.completeRouteLoading();
        }
      });
  };
}

export function provideLoading(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideAppInitializer(initializeRouteLoading())
  ]);
}