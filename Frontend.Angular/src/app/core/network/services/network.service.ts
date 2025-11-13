import { HttpClient } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  distinctUntilChanged,
  fromEvent,
  map,
  merge,
  Observable,
  of,
  switchMap,
  tap,
  timer
} from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';

import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';
import { type HealthCheckResponse } from '../models/health-check.model';

const DEFAULT_CHECK_INTERVAL = 30000;
const DEFAULT_MAX_ATTEMPTS = 2; // ✅ Changed to 2 for faster feedback
const STARTUP_GRACE_CHECKS = 2;
const INITIAL_CHECK_DELAY = 2000;

interface NormalizedNetworkConfig {
  healthEndpoint: string;
  checkInterval: number;
  maxAttempts: number;
}

export interface NetworkStatus {
  online: boolean;
  healthy: boolean;
  consecutiveErrors: number;
  lastCheck: Date | null;
}

export type NetworkConfig = AppNetworkConfig;

@Injectable({ providedIn: 'root' })
export class NetworkService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastManager);
  private readonly providedConfig = inject(NETWORK_CONFIG, { optional: true });
  private readonly destroyRef = inject(DestroyRef);

  // ═══════════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════════

  private readonly _online = signal(navigator.onLine);
  private readonly _consecutiveErrors = signal(0);
  private readonly _lastCheck = signal<Date | null>(null);
  private readonly _maxAttempts = signal(DEFAULT_MAX_ATTEMPTS);
  private readonly _checkCount = signal(0);
  private readonly _lastOnlineState = signal(navigator.onLine); // ✅ Track previous state

  private offlineToastId: string | null = null;
  private healthWarningToastId: string | null = null;

  // ═══════════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

  // ✅ NEW: Public computed signal for UI components
  readonly networkState = computed(() => ({
    online: this._online(),
    healthy: this.isHealthy(),
    consecutiveErrors: this._consecutiveErrors(),
    lastCheck: this._lastCheck()
  }));

  private healthCheckSubscription?: { unsubscribe: () => void };

  constructor() {
    this.monitorBrowserStatus();

    if (this.providedConfig) {
      this.startMonitoring(this.providedConfig);
    }

    this.destroyRef.onDestroy(() => this.teardown());
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════════

  startMonitoring(config: NetworkConfig): void {
    const normalized = this.normalizeConfig(config);

    if (!normalized) {
      this.stopMonitoring();
      return;
    }

    this.stopMonitoring();

    this._maxAttempts.set(normalized.maxAttempts);
    this._checkCount.set(0);

    this.healthCheckSubscription = timer(INITIAL_CHECK_DELAY, normalized.checkInterval)
      .pipe(
        switchMap(() => this.performHealthCheck(normalized.healthEndpoint)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();
  }

  stopMonitoring(): void {
    if (this.healthCheckSubscription) {
      this.healthCheckSubscription.unsubscribe();
      this.healthCheckSubscription = undefined;
    }
  }

  getStatus(): NetworkStatus {
    return {
      online: this._online(),
      healthy: this.isHealthy(),
      consecutiveErrors: this._consecutiveErrors(),
      lastCheck: this._lastCheck()
    };
  }

  markSuccess(): void {
    this._consecutiveErrors.set(0);
  }

  trackError(networkRelated = true): void {
    if (networkRelated) {
      this._consecutiveErrors.update(n => n + 1);
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Private Methods - Zoneless Compatible
  // ═══════════════════════════════════════════════════════════════════════

  private monitorBrowserStatus(): void {
    const status$ = merge(
      fromEvent(window, 'online').pipe(map(() => true)),
      fromEvent(window, 'offline').pipe(map(() => false)),
      of(navigator.onLine)
    ).pipe(distinctUntilChanged());

    let isFirstEmission = true;

    status$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(isOnline => {
        const previousState = this._lastOnlineState();
        this._online.set(isOnline);
        this._lastOnlineState.set(isOnline);

        // ✅ FIX: Only show notifications on state changes (not initial load)
        if (isFirstEmission) {
          isFirstEmission = false;
          
          // If app starts while offline, show notification
          if (!isOnline) {
            this.handleOffline();
          }
          
          return;
        }

        // ✅ FIX: Check if state actually changed
        if (isOnline !== previousState) {
          if (isOnline) {
            this.handleOnline();
          } else {
            this.handleOffline();
          }
        }
      });
  }

  private performHealthCheck(endpoint: string): Observable<HealthCheckResponse | null> {
    return this.http.get<HealthCheckResponse>(endpoint, {
      headers: {
        'X-Skip-Loading': 'true',
        'X-Skip-Logging': 'true'
      }
    }).pipe(
      tap(() => this.handleHealthCheckSuccess()),
      catchError(() => {
        this.handleHealthCheckFailure();
        return of(null);
      })
    );
  }

  private normalizeConfig(config: NetworkConfig): NormalizedNetworkConfig | null {
    const endpoint = config.healthEndpoint?.trim();

    if (!endpoint) {
      return null;
    }

    return {
      healthEndpoint: endpoint,
      checkInterval: config.checkInterval ?? DEFAULT_CHECK_INTERVAL,
      maxAttempts: config.maxAttempts ?? DEFAULT_MAX_ATTEMPTS
    };
  }

  private handleOnline(): void {
    // Reset error count when back online
    this._consecutiveErrors.set(0);

    // ✅ FIX: Dismiss offline toast if it exists
    if (this.offlineToastId) {
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
    }

    // ✅ FIX: Dismiss health warning toast if it exists
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // ✅ FIX: Always show "back online" notification
    this.toast.success(
      'Your internet connection has been restored.',
      'Back online'
    );
  }

  private handleOffline(): void {
    // ✅ FIX: Dismiss health warning toast (no longer relevant when offline)
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // ✅ FIX: Show offline toast only if not already showing
    if (!this.offlineToastId) {
      this.offlineToastId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No internet connection',
        0 // Persistent
      );
    }
  }

  private handleHealthCheckSuccess(): void {
    const wasUnhealthy = !this.isHealthy(); // Check before resetting

    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    // ✅ Sync browser online state with our state
    if (navigator.onLine && !this._online()) {
      this._online.set(true);
      this.handleOnline();
      return;
    }

    // ✅ FIX: Show recovery notification if we were previously unhealthy
    if (this.healthWarningToastId && wasUnhealthy) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;

      this.toast.success(
        'Connection to the server has been restored.',
        'Server reconnected'
      );
    }
  }

  private handleHealthCheckFailure(): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    // ✅ Sync browser offline state with our state
    if (!navigator.onLine && this._online()) {
      this._online.set(false);
      this.handleOffline();
      return;
    }

    const isInGracePeriod = this._checkCount() <= STARTUP_GRACE_CHECKS;

    // ✅ FIX: Show server unreachable notification when threshold is reached
    if (
      this._online() &&
      this._consecutiveErrors() >= this._maxAttempts() &&
      !isInGracePeriod &&
      !this.healthWarningToastId
    ) {
      this.notifyHealthCheckIssue();
    }
  }

  private notifyHealthCheckIssue(): void {
    if (this.healthWarningToastId) {
      return;
    }

    this.healthWarningToastId = this.toast.warning(
      'Unable to reach the server. This could be due to server maintenance or network issues. We\'ll keep trying in the background.',
      'Server unreachable',
      0 // Persistent
    );
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════════

  getDiagnostics() {
    return {
      status: this.getStatus(),
      browserOnline: navigator.onLine,
      healthCheckActive: !!this.healthCheckSubscription,
      checkCount: this._checkCount(),
      isInGracePeriod: this._checkCount() <= STARTUP_GRACE_CHECKS,
      activeToasts: {
        offline: !!this.offlineToastId,
        offlineToastId: this.offlineToastId,
        healthWarning: !!this.healthWarningToastId,
        healthWarningToastId: this.healthWarningToastId
      }
    };
  }

  private teardown(): void {
    this.stopMonitoring();

    if (this.offlineToastId) {
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
    }

    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }
  }
}