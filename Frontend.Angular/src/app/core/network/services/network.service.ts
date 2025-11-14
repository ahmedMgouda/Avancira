import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { computed, DestroyRef, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  catchError,
  distinctUntilChanged,
  firstValueFrom,
  fromEvent,
  map,
  merge,
  Observable,
  of,
  switchMap,
  tap,
  timeout,
  timer} from 'rxjs';

import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';
import { type HealthCheckResponse } from '../models/health-check.model';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * NETWORK SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added platform checks for navigator.onLine
 * ✅ Added platform checks for window events
 * ✅ Fixed health-check interval override issue
 * ✅ SSR-safe with fallback behavior (assumes online on server)
 * ✅ Respects user-provided checkInterval config
 */

const DEFAULT_HEALTHY_INTERVAL = 30000;
const DEFAULT_UNHEALTHY_INTERVAL = 10000;
const DEFAULT_MAX_ATTEMPTS = 2;
const INITIAL_CHECK_DELAY = 500;
const STARTUP_GRACE_CHECKS = 2;
const HEALTH_CHECK_TIMEOUTS = [1000, 3000];
const MIN_CHECK_INTERVAL = 5000; // 5 seconds minimum
const MAX_CHECK_INTERVAL = 300000; // 5 minutes maximum

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
  private readonly providedConfig = inject(NETWORK_CONFIG, { optional: true });
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // ═══════════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════════

  private readonly _online = signal(this.getInitialOnlineStatus());
  private readonly _consecutiveErrors = signal(0);
  private readonly _lastCheck = signal<Date | null>(null);
  private readonly _maxAttempts = signal(DEFAULT_MAX_ATTEMPTS);
  private readonly _checkCount = signal(0);
  private readonly _lastOnlineState = signal(this.getInitialOnlineStatus());
  private readonly _lastHealthyState = signal(true);
  private readonly _currentCheckInterval = signal(DEFAULT_HEALTHY_INTERVAL);

  private healthCheckSubscription?: { unsubscribe: () => void };
  private normalizedConfig: NormalizedNetworkConfig | null = null;

  // ═══════════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

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

  startMonitoring(config: NetworkConfig): void {
    const normalized = this.normalizeConfig(config);

    if (!normalized) {
      this.stopMonitoring();
      return;
    }

    this.stopMonitoring();

    this.normalizedConfig = normalized;
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

  async performImmediateHealthCheck(): Promise<boolean> {
    if (!this.normalizedConfig) {
      return false;
    }

    try {
      const result = await firstValueFrom(
        this.performSingleHealthCheck(this.normalizedConfig.healthEndpoint)
      );
      return result !== null;
    } catch {
      return false;
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
  // Private Methods - Platform-Safe
  // ═══════════════════════════════════════════════════════════════════════

  private getInitialOnlineStatus(): boolean {
    // SSR: Assume online
    if (!this.isBrowser) {
      return true;
    }
    return navigator.onLine;
  }

  private scheduleNextCheck(config: NormalizedNetworkConfig): void {
    const interval = this.isHealthy() 
      ? config.healthyCheckInterval 
      : config.unhealthyCheckInterval;

    this._currentCheckInterval.set(interval);

    this.healthCheckSubscription = timer(INITIAL_CHECK_DELAY, interval)
      .pipe(
        switchMap(() => this.performSingleHealthCheck(config.healthEndpoint)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();
  }

  private performSingleHealthCheck(endpoint: string): Observable<HealthCheckResponse | null> {
    const attemptNumber = this._consecutiveErrors();
    const timeoutMs = HEALTH_CHECK_TIMEOUTS[
      Math.min(attemptNumber, HEALTH_CHECK_TIMEOUTS.length - 1)
    ];

    return this.http.get<HealthCheckResponse>(endpoint, {
      headers: {
        'X-Skip-Loading': 'true',
        'X-Skip-Logging': 'true'
      }
    }).pipe(
      timeout(timeoutMs),
      tap(() => this.handleHealthCheckSuccess()),
      catchError((error) => {
        this.handleHealthCheckFailure(error);
        return of(null);
      })
    );
  }

  private monitorBrowserStatus(): void {
    // SSR: Skip browser event monitoring
    if (!this.isBrowser) {
      return;
    }

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

        if (isFirstEmission) {
          isFirstEmission = false;
          return;
        }

        if (isOnline !== previousState) {
          if (isOnline) {
            this._consecutiveErrors.set(0);
          }
        }
      });
  }

  private normalizeConfig(config: NetworkConfig): NormalizedNetworkConfig | null {
    const endpoint = config.healthEndpoint?.trim();

    if (!endpoint) {
      return null;
    }

    // FIX: Respect user-provided checkInterval, with bounds validation
    let checkInterval = config.checkInterval ?? DEFAULT_HEALTHY_INTERVAL;
    
    // Validate bounds
    if (checkInterval < MIN_CHECK_INTERVAL) {
      console.warn(
        `[NetworkService] checkInterval ${checkInterval}ms is below minimum ${MIN_CHECK_INTERVAL}ms. Using minimum.`
      );
      checkInterval = MIN_CHECK_INTERVAL;
    }
    
    if (checkInterval > MAX_CHECK_INTERVAL) {
      console.warn(
        `[NetworkService] checkInterval ${checkInterval}ms exceeds maximum ${MAX_CHECK_INTERVAL}ms. Using maximum.`
      );
      checkInterval = MAX_CHECK_INTERVAL;
    }

    return {
      healthEndpoint: endpoint,
      healthyCheckInterval: checkInterval,
      unhealthyCheckInterval: DEFAULT_UNHEALTHY_INTERVAL,
      maxAttempts: config.maxAttempts ?? DEFAULT_MAX_ATTEMPTS
    };
  }

  private handleHealthCheckSuccess(): void {
    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);
    this._lastHealthyState.set(true);

    if (this.isBrowser && navigator.onLine && !this._online()) {
      this._online.set(true);
    }
  }

  private handleHealthCheckFailure(_error?: Error): void {
    this._consecutiveErrors.update(n => n + 1);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

    if (this.isBrowser && !navigator.onLine && this._online()) {
      this._online.set(false);
    }

    const isInGracePeriod = this._checkCount() <= STARTUP_GRACE_CHECKS;
    const wasHealthy = this._lastHealthyState();

    if (
      this._online() &&
      this._consecutiveErrors() >= this._maxAttempts() &&
      !isInGracePeriod &&
      wasHealthy
    ) {
      this._lastHealthyState.set(false);
    }
  }

  getDiagnostics() {
    return {
      status: this.getStatus(),
      browserOnline: this.isBrowser ? navigator.onLine : 'N/A (SSR)',
      healthCheckActive: !!this.healthCheckSubscription,
      checkCount: this._checkCount(),
      currentInterval: this._currentCheckInterval(),
      isInGracePeriod: this._checkCount() <= STARTUP_GRACE_CHECKS,
      maxAttempts: this._maxAttempts(),
      nextTimeout: HEALTH_CHECK_TIMEOUTS[
        Math.min(this._consecutiveErrors(), HEALTH_CHECK_TIMEOUTS.length - 1)
      ],
      retryStrategy: {
        attempts: DEFAULT_MAX_ATTEMPTS,
        timeouts: HEALTH_CHECK_TIMEOUTS,
        totalMaxTime: HEALTH_CHECK_TIMEOUTS.reduce((a, b) => a + b, 0),
        healthyInterval: DEFAULT_HEALTHY_INTERVAL,
        unhealthyInterval: DEFAULT_UNHEALTHY_INTERVAL
      }
    };
  }

  private teardown(): void {
    this.stopMonitoring();
  }
}
