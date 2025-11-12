import { HttpClient } from '@angular/common/http';
import { DestroyRef, OnDestroy, computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  distinctUntilChanged,
  fromEvent,
  map,
  merge,
  of,
  Observable,
  Subscription,
  switchMap,
  tap,
  timer
} from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';

import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';
import { type HealthCheckResponse } from '../models/health-check.model';

const DEFAULT_CHECK_INTERVAL = 30000;
const DEFAULT_MAX_ATTEMPTS = 3;
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
export class NetworkService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastManager);
  private readonly providedConfig = inject(NETWORK_CONFIG, { optional: true });
  private readonly destroyRef = inject(DestroyRef);

  // ═══════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════

  private readonly _online = signal(navigator.onLine);
  private readonly _consecutiveErrors = signal(0);
  private readonly _lastCheck = signal<Date | null>(null);
  private readonly _maxAttempts = signal(DEFAULT_MAX_ATTEMPTS);
  private readonly _checkCount = signal(0);

  private offlineToastId: string | null = null;
  private healthWarningToastId: string | null = null;

  // ═══════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

  private healthCheckSubscription?: Subscription;

  constructor() {
    this.monitorBrowserStatus();

    if (this.providedConfig) {
      this.startMonitoring(this.providedConfig);
    }

    this.destroyRef.onDestroy(() => this.teardown());
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════

  startMonitoring(config: NetworkConfig): void {
    const normalized = this.normalizeConfig(config);

    if (!normalized) {
      this.stopMonitoring();
      return;
    }

    this.stopMonitoring();

    // ✅ Signal updates in injection context
    this._maxAttempts.set(normalized.maxAttempts);
    this._checkCount.set(0);

    this.healthCheckSubscription = timer(INITIAL_CHECK_DELAY, normalized.checkInterval)
      .pipe(switchMap(() => this.performHealthCheck(normalized.healthEndpoint)))
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

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Zoneless Compatible
  // ═══════════════════════════════════════════════════════════════════

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
        this._online.set(isOnline);

        if (isFirstEmission) {
          isFirstEmission = false;

          if (!isOnline) {
            this.handleOffline();
          }

          return;
        }

        if (isOnline) {
          this.handleOnline();
        } else {
          this.handleOffline();
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
    // Signal already updated before this call
    this._consecutiveErrors.set(0);

    // Dismiss offline toast
    if (this.offlineToastId) {
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
      this.toast.success('Connection restored.', 'Back online');
    }

    // Dismiss health warning toast
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }
  }

  private handleOffline(): void {
    // Signal already updated before this call

    // Dismiss health warning toast (no longer relevant)
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // Show offline toast
    if (!this.offlineToastId) {
      this.offlineToastId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No internet connection',
        0 // Persistent
      );
    }
  }

  private handleHealthCheckSuccess(): void {
    const hadErrors = this._consecutiveErrors() > 0;

    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    if (navigator.onLine && !this._online()) {
      this._online.set(true);
      this.handleOnline();
    }

    if (this.healthWarningToastId && hadErrors) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;

      if (hadErrors) {
        this.toast.success('Server connection restored.', 'Connection restored');
      }
    }
  }

  private handleHealthCheckFailure(): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    if (!navigator.onLine && this._online()) {
      this._online.set(false);
      this.handleOffline();
    }

    const isInGracePeriod = this._checkCount() <= STARTUP_GRACE_CHECKS;

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
      'We are having trouble reaching the server. We will keep trying in the background.',
      'Connection issue',
      0
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════

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

  ngOnDestroy(): void {
    this.teardown();
  }
}
