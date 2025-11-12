import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, interval, of, startWith, Subscription, switchMap, tap } from 'rxjs';

import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';
import { ToastManager } from '../../toast/services/toast-manager.service';

const DEFAULT_CHECK_INTERVAL = 30000;
const DEFAULT_MAX_ATTEMPTS = 3;

interface NormalizedNetworkConfig {
  healthEndpoint: string;
  checkInterval: number;
  maxAttempts: number;
}

export interface NetworkStatus {
  online: boolean;
  healthy: boolean;
  consecutiveErrors: number;
  totalRequests: number;
  successfulRequests: number;
  successRate: number;
  lastCheck: Date;
}

export type NetworkConfig = AppNetworkConfig;

@Injectable({ providedIn: 'root' })
export class NetworkService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastManager);
  private readonly providedConfig = inject(NETWORK_CONFIG, { optional: true });

  // ═══════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════

  private readonly _online = signal(navigator.onLine);
  private readonly _consecutiveErrors = signal(0);
  private readonly _totalRequests = signal(0);
  private readonly _successfulRequests = signal(0);
  private readonly _lastCheck = signal(new Date());
  private readonly _maxAttempts = signal(DEFAULT_MAX_ATTEMPTS);

  private offlineToastId: string | null = null;
  private healthFailureToastShown = false;

  // ═══════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly successRate = computed(() => {
    const total = this._totalRequests();
    return total > 0 ? (this._successfulRequests() / total) * 100 : 100;
  });

  /**
   * Network is healthy if:
   * 1. Browser reports online
   * 2. Consecutive errors stay below configured threshold
   */
  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

  private healthCheckSubscription?: Subscription;

  constructor() {
    this.setupBrowserListeners();
    if (this.providedConfig) {
      this.startMonitoring(this.providedConfig);
    }
    this.initializeOfflineState();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Start health check monitoring
   */
  startMonitoring(config: NetworkConfig): void {
    const normalized = this.normalizeConfig(config);

    if (!normalized) {
      this.stopMonitoring();
      return;
    }

    this.stopMonitoring();

    this._maxAttempts.set(normalized.maxAttempts);
    this.healthFailureToastShown = false;

    this.healthCheckSubscription = interval(normalized.checkInterval)
      .pipe(
        startWith(0),
        switchMap(() => this.performHealthCheck(normalized.healthEndpoint))
      )
      .subscribe();
  }

  /**
   * Stop health check monitoring
   */
  stopMonitoring(): void {
    if (this.healthCheckSubscription) {
      this.healthCheckSubscription.unsubscribe();
      this.healthCheckSubscription = undefined;
    }
  }

  /**
   * Get current network status
   */
  getStatus(): NetworkStatus {
    return {
      online: this._online(),
      healthy: this.isHealthy(),
      consecutiveErrors: this._consecutiveErrors(),
      totalRequests: this._totalRequests(),
      successfulRequests: this._successfulRequests(),
      successRate: this.successRate(),
      lastCheck: this._lastCheck()
    };
  }

  /**
   * Track successful request
   * Resets consecutive error counter
   */
  markSuccess(): void {
    this._consecutiveErrors.set(0);
    this._totalRequests.update(n => n + 1);
    this._successfulRequests.update(n => n + 1);
  }

  /**
   * Track failed request
   * ✅ Simple counting - no deduplication
   */
  trackError(networkRelated = true): void {
    if (networkRelated) {
      this._consecutiveErrors.update(n => n + 1);
    }

    this._totalRequests.update(n => n + 1);
  }

  /**
   * Check if network has recent errors
   */
  hasRecentNetworkError(): boolean {
    return this._consecutiveErrors() > 0;
  }

  /**
   * Reset error counters
   */
  reset(): void {
    this._consecutiveErrors.set(0);
    this._totalRequests.set(0);
    this._successfulRequests.set(0);
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Setup browser online/offline event listeners
   */
  private setupBrowserListeners(): void {
    window.addEventListener('online', () => {
      this.handleOnline();
    });

    window.addEventListener('offline', () => {
      this.handleOffline();
    });
  }

  /**
   * Perform health check against backend
   */
  private performHealthCheck(endpoint: string) {
    return this.http.get(endpoint, {
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

  private initializeOfflineState(): void {
    if (!navigator.onLine) {
      this.handleOffline();
    }
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
    this._online.set(true);
    this._consecutiveErrors.set(0);
    this.healthFailureToastShown = false;

    if (this.offlineToastId) {
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
      this.toast.success('Connection restored.', 'Back online');
    }
  }

  private handleOffline(): void {
    this._online.set(false);

    if (!this.offlineToastId) {
      this.offlineToastId = this.toast.error(
        'You appear to be offline. Please check your internet connection.',
        'No internet connection'
      );
    }
  }

  private handleHealthCheckSuccess(): void {
    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this.healthFailureToastShown = false;
  }

  private handleHealthCheckFailure(): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());

    if (this._online() && this._consecutiveErrors() >= this._maxAttempts()) {
      this.notifyHealthCheckIssue();
    }
  }

  private notifyHealthCheckIssue(): void {
    if (this.healthFailureToastShown) {
      return;
    }

    this.toast.warning(
      'We are having trouble reaching the server. We will keep trying in the background.',
      'Connection issue'
    );
    this.healthFailureToastShown = true;
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Get diagnostic information
   */
  getDiagnostics() {
    return {
      status: this.getStatus(),
      browserOnline: navigator.onLine,
      healthCheckActive: !!this.healthCheckSubscription
    };
  }

  ngOnDestroy(): void {
    this.stopMonitoring();
  }
}