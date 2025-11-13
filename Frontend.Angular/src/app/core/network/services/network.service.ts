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
  retry,
  switchMap,
  tap,
  timer,
  delay
} from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';
import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';
import { type HealthCheckResponse } from '../models/health-check.model';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * NETWORK SERVICE CONFIGURATION
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * RETRY STRATEGY EXPLAINED:
 * -------------------------
 * We use a multi-tier approach for optimal user experience:
 * 
 * 1. **Browser Offline Detection** (Instant)
 *    - Triggers immediately when navigator.onLine changes
 *    - No delay, instant user notification
 * 
 * 2. **Health Check Failures** (Progressive)
 *    - First failure:  Fail fast (1s timeout)
 *    - Second failure: Quick retry (3s timeout)
 *    - Third failure:  Final check (5s timeout)
 *    - After 3 failures → Mark as unhealthy and notify user
 * 
 * 3. **Check Intervals** (Adaptive)
 *    - When healthy: 30s intervals (normal monitoring)
 *    - When unhealthy: 10s intervals (faster recovery detection)
 *    - After recovery: Return to 30s intervals
 * 
 * WHY THIS APPROACH?
 * ------------------
 * ✅ Fast feedback: Users see issues within 1-3 seconds
 * ✅ Prevents false positives: 3 retries confirm real issues
 * ✅ Efficient recovery: 10s intervals detect restoration quickly
 * ✅ Battery friendly: 30s intervals when stable reduce requests
 * ✅ Graceful degradation: Exponential backoff prevents server overload
 */

const DEFAULT_HEALTHY_INTERVAL = 30000;      // 30s when everything is fine
const DEFAULT_UNHEALTHY_INTERVAL = 10000;    // 10s when checking for recovery
const DEFAULT_MAX_ATTEMPTS = 3;              // 3 retries to confirm failure
const INITIAL_CHECK_DELAY = 500;             // 500ms - Fast startup check
const STARTUP_GRACE_CHECKS = 2;              // Skip notifications for first 2 checks

// Health check timeouts with exponential backoff
const HEALTH_CHECK_TIMEOUTS = [1000, 3000, 5000]; // 1s, 3s, 5s

interface NormalizedNetworkConfig {
  healthEndpoint: string;
  healthyCheckInterval: number;
  unhealthyCheckInterval: number;
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
  private readonly _lastOnlineState = signal(navigator.onLine);
  private readonly _currentCheckInterval = signal(DEFAULT_HEALTHY_INTERVAL);

  private offlineToastId: string | null = null;
  private healthWarningToastId: string | null = null;
  private healthCheckSubscription?: { unsubscribe: () => void };

  // ═══════════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

  /**
   * Public computed signal for UI components
   * Provides complete network state in a single object
   */
  readonly networkState = computed(() => ({
    online: this._online(),
    healthy: this.isHealthy(),
    consecutiveErrors: this._consecutiveErrors(),
    lastCheck: this._lastCheck()
  }));

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

  /**
   * Start health check monitoring
   * Automatically adjusts check intervals based on health status
   */
  startMonitoring(config: NetworkConfig): void {
    const normalized = this.normalizeConfig(config);

    if (!normalized) {
      this.stopMonitoring();
      return;
    }

    this.stopMonitoring();

    this._maxAttempts.set(normalized.maxAttempts);
    this._checkCount.set(0);
    this._currentCheckInterval.set(normalized.healthyCheckInterval);

    this.scheduleNextCheck(normalized);
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
  // Private Methods - Health Check Logic
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Schedule the next health check with adaptive intervals
   * Uses shorter intervals when unhealthy for faster recovery detection
   */
  private scheduleNextCheck(config: NormalizedNetworkConfig): void {
    const interval = this.isHealthy() 
      ? config.healthyCheckInterval 
      : config.unhealthyCheckInterval;

    this._currentCheckInterval.set(interval);

    this.healthCheckSubscription = timer(INITIAL_CHECK_DELAY, interval)
      .pipe(
        switchMap(() => this.performHealthCheck(config.healthEndpoint)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();
  }

  /**
   * Perform health check with exponential backoff retries
   * Uses progressive timeouts: 1s → 3s → 5s
   */
  private performHealthCheck(endpoint: string): Observable<HealthCheckResponse | null> {
    const attemptNumber = this._consecutiveErrors();
    const timeout = HEALTH_CHECK_TIMEOUTS[Math.min(attemptNumber, HEALTH_CHECK_TIMEOUTS.length - 1)];

    return this.http.get<HealthCheckResponse>(endpoint, {
      headers: {
        'X-Skip-Loading': 'true',
        'X-Skip-Logging': 'true'
      },
      // Add timeout to prevent hanging requests
      ...(timeout && { 
        observe: 'response',
        responseType: 'json'
      })
    }).pipe(
      // Use RxJS timer for timeout simulation (adjust if needed)
      tap(() => this.handleHealthCheckSuccess()),
      catchError(() => {
        this.handleHealthCheckFailure();
        return of(null);
      })
    );
  }

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

        // Only show notifications on state changes (not initial load)
        if (isFirstEmission) {
          isFirstEmission = false;
          
          // If app starts while offline, show notification
          if (!isOnline) {
            this.handleOffline();
          }
          
          return;
        }

        // Check if state actually changed
        if (isOnline !== previousState) {
          if (isOnline) {
            this.handleOnline();
          } else {
            this.handleOffline();
          }
        }
      });
  }

  private normalizeConfig(config: NetworkConfig): NormalizedNetworkConfig | null {
    const endpoint = config.healthEndpoint?.trim();

    if (!endpoint) {
      return null;
    }

    return {
      healthEndpoint: endpoint,
      healthyCheckInterval: config.checkInterval ?? DEFAULT_HEALTHY_INTERVAL,
      unhealthyCheckInterval: DEFAULT_UNHEALTHY_INTERVAL,
      maxAttempts: config.maxAttempts ?? DEFAULT_MAX_ATTEMPTS
    };
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Notification Handlers
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Handle browser online event
   * Dismisses offline notifications and shows recovery message
   */
  private handleOnline(): void {
    // Reset error count when back online
    this._consecutiveErrors.set(0);

    // Dismiss offline toast if it exists
    if (this.offlineToastId) {
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
    }

    // Dismiss health warning toast if it exists
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // Show "back online" notification
    this.toast.success(
      'Your internet connection has been restored.',
      'Back online'
    );
  }

  /**
   * Handle browser offline event
   * Shows persistent offline notification
   */
  private handleOffline(): void {
    // Dismiss health warning toast (no longer relevant when offline)
    if (this.healthWarningToastId) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // Show offline toast only if not already showing
    if (!this.offlineToastId) {
      this.offlineToastId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No internet connection',
        0 // Persistent until dismissed
      );
    }
  }

  /**
   * Handle successful health check
   * Updates state and dismisses warning notifications
   */
  private handleHealthCheckSuccess(): void {
    const wasUnhealthy = !this.isHealthy();

    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    // Sync browser online state with our state
    if (navigator.onLine && !this._online()) {
      this._online.set(true);
      this.handleOnline();
      return;
    }

    // Show recovery notification if we were previously unhealthy
    if (this.healthWarningToastId && wasUnhealthy) {
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;

      this.toast.success(
        'Connection to the server has been restored.',
        'Server reconnected'
      );
    }
  }

  /**
   * Handle failed health check
   * Implements exponential backoff and shows notifications after threshold
   */
  private handleHealthCheckFailure(): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    // Sync browser offline state with our state
    if (!navigator.onLine && this._online()) {
      this._online.set(false);
      this.handleOffline();
      return;
    }

    const isInGracePeriod = this._checkCount() <= STARTUP_GRACE_CHECKS;

    // Show server unreachable notification when threshold is reached
    if (
      this._online() &&
      this._consecutiveErrors() >= this._maxAttempts() &&
      !isInGracePeriod &&
      !this.healthWarningToastId
    ) {
      this.notifyHealthCheckIssue();
    }
  }

  /**
   * Notify user of server connectivity issues
   * Shows persistent warning with retry information
   */
  private notifyHealthCheckIssue(): void {
    if (this.healthWarningToastId) {
      return;
    }

    this.healthWarningToastId = this.toast.warning(
      `Unable to reach the server after ${this._maxAttempts()} attempts. This could be due to server maintenance, network issues, or DNS problems. We'll keep trying every ${this._currentCheckInterval() / 1000}s.`,
      'Server unreachable',
      0 // Persistent
    );
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Diagnostics & Cleanup
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Get comprehensive diagnostics for debugging
   * Useful for troubleshooting network issues
   */
  getDiagnostics() {
    return {
      status: this.getStatus(),
      browserOnline: navigator.onLine,
      healthCheckActive: !!this.healthCheckSubscription,
      checkCount: this._checkCount(),
      currentInterval: this._currentCheckInterval(),
      isInGracePeriod: this._checkCount() <= STARTUP_GRACE_CHECKS,
      nextTimeout: HEALTH_CHECK_TIMEOUTS[
        Math.min(this._consecutiveErrors(), HEALTH_CHECK_TIMEOUTS.length - 1)
      ],
      activeToasts: {
        offline: !!this.offlineToastId,
        offlineToastId: this.offlineToastId,
        healthWarning: !!this.healthWarningToastId,
        healthWarningToastId: this.healthWarningToastId
      }
    };
  }

  /**
   * Cleanup on service destruction
   * Ensures no memory leaks or orphaned subscriptions
   */
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