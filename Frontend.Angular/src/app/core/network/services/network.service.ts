import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, interval, of, Subscription, switchMap } from 'rxjs';

export interface NetworkStatus {
  online: boolean;
  healthy: boolean;
  consecutiveErrors: number;
  totalRequests: number;
  successfulRequests: number;
  successRate: number;
  lastCheck: Date;
}

export interface NetworkConfig {
  healthEndpoint: string;
  checkInterval: number;
  maxAttempts: number;
}

@Injectable({ providedIn: 'root' })
export class NetworkService {
  private readonly http = inject(HttpClient);

  // ═══════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════

  private readonly _online = signal(navigator.onLine);
  private readonly _consecutiveErrors = signal(0);
  private readonly _totalRequests = signal(0);
  private readonly _successfulRequests = signal(0);
  private readonly _lastCheck = signal(new Date());

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
   * 2. Fewer than 3 consecutive errors
   */
  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < 3;
  });

  private healthCheckSubscription?: Subscription;

  constructor() {
    this.setupBrowserListeners();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Start health check monitoring
   */
  startMonitoring(config: NetworkConfig): void {
    this.stopMonitoring();

    this.healthCheckSubscription = interval(config.checkInterval)
      .pipe(
        switchMap(() => this.performHealthCheck(config.healthEndpoint)),
        catchError(() => {
          this.markUnhealthy();
          return of(null);
        })
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
  trackError(): void {
    this._consecutiveErrors.update(n => n + 1);
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
      this._online.set(true);
      this._consecutiveErrors.set(0);
    });

    window.addEventListener('offline', () => {
      this._online.set(false);
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
      catchError(() => {
        this.markUnhealthy();
        return of(null);
      })
    );
  }

  /**
   * Mark network as unhealthy
   */
  private markUnhealthy(): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());
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