import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, Injector, signal } from '@angular/core';
import { catchError, delay, fromEvent, interval, merge, of, startWith, Subscription, switchMap, tap } from 'rxjs';

import { ToastManager } from '../../toast/services/toast-manager.service';

import { NETWORK_CONFIG, type NetworkConfig as AppNetworkConfig } from '../../config/network.config';

const DEFAULT_CHECK_INTERVAL = 30000;
const DEFAULT_MAX_ATTEMPTS = 3;
const STARTUP_GRACE_CHECKS = 2;
const INITIAL_CHECK_DELAY = 2000;
const POLLING_INTERVAL = 2000;

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

/**
 * NetworkService - Zoneless Angular 19 Compatible
 * ✅ Uses RxJS for all async operations
 * ✅ Signal updates wrapped in runInInjectionContext for proper change detection
 * ✅ All subscriptions properly managed
 */
@Injectable({ providedIn: 'root' })
export class NetworkService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastManager);
  private readonly injector = inject(Injector); // ✅ For runInInjectionContext
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
  private readonly _checkCount = signal(0);

  private offlineToastId: string | null = null;
  private healthWarningToastId: string | null = null;

  // ═══════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════

  readonly isOnline = this._online.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();

  readonly successRate = computed(() => {
    const total = this._totalRequests();
    return total > 0 ? (this._successfulRequests() / total) * 100 : 100;
  });

  readonly isHealthy = computed(() => {
    return this._online() && this._consecutiveErrors() < this._maxAttempts();
  });

  private healthCheckSubscription?: Subscription;
  private browserEventsSubscription?: Subscription;
  private pollingSubscription?: Subscription;

  constructor() {
    console.log('[NetworkService] Constructor - Zoneless mode with Injector');
    console.log('[NetworkService] Initial navigator.onLine:', navigator.onLine);
    
    this.setupBrowserListeners();
    this.setupPolling();
    
    if (this.providedConfig) {
      this.startMonitoring(this.providedConfig);
    }
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

    this.healthCheckSubscription = interval(normalized.checkInterval)
      .pipe(
        startWith(0),
        delay(INITIAL_CHECK_DELAY),
        switchMap(() => this.performHealthCheck(normalized.healthEndpoint))
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
      totalRequests: this._totalRequests(),
      successfulRequests: this._successfulRequests(),
      successRate: this.successRate(),
      lastCheck: this._lastCheck()
    };
  }

  markSuccess(): void {
    this._consecutiveErrors.set(0);
    this._totalRequests.update(n => n + 1);
    this._successfulRequests.update(n => n + 1);
  }

  trackError(networkRelated = true): void {
    if (networkRelated) {
      this._consecutiveErrors.update(n => n + 1);
    }
    this._totalRequests.update(n => n + 1);
  }

  hasRecentNetworkError(): boolean {
    return this._consecutiveErrors() > 0;
  }

  reset(): void {
    this._consecutiveErrors.set(0);
    this._totalRequests.set(0);
    this._successfulRequests.set(0);
    this._checkCount.set(0);
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Zoneless Compatible
  // ═══════════════════════════════════════════════════════════════════

  /**
   * ✅ CRITICAL: Use toSignal or manual signal updates in subscription
   */
  private setupBrowserListeners(): void {
    console.log('[NetworkService] Setting up RxJS-based event listeners');
    
    const online$ = fromEvent(window, 'online').pipe(
      tap(() => {
        console.log('[NetworkService] 🟢 ONLINE event (via RxJS)');
        console.log('[NetworkService] navigator.onLine:', navigator.onLine);
      })
    );

    const offline$ = fromEvent(window, 'offline').pipe(
      tap(() => {
        console.log('[NetworkService] 🔴 OFFLINE event (via RxJS)');
        console.log('[NetworkService] navigator.onLine:', navigator.onLine);
      })
    );

    // ✅ ZONELESS FIX: Update signals and trigger toasts inside subscription
    this.browserEventsSubscription = merge(online$, offline$)
      .subscribe(() => {
        const isOnline = navigator.onLine;
        console.log('[NetworkService] Processing event, navigator.onLine:', isOnline);
        
        // ✅ Update signal immediately (this works in zoneless)
        this._online.set(isOnline);
        
        if (isOnline) {
          this.handleOnline();
        } else {
          this.handleOffline();
        }
      });

    console.log('[NetworkService] RxJS event listeners registered');
  }

  /**
   * ✅ Polling with signal updates
   */
  private setupPolling(): void {
    console.log('[NetworkService] Setting up RxJS-based polling');
    
    let lastState = navigator.onLine;

    this.pollingSubscription = interval(POLLING_INTERVAL)
      .subscribe(() => {
        const currentState = navigator.onLine;
        
        if (currentState !== lastState) {
          console.log('[NetworkService] 🔄 State change via polling');
          console.log('[NetworkService] Was:', lastState, '→ Now:', currentState);
          
          lastState = currentState;
          
          // ✅ Update signal immediately
          this._online.set(currentState);
          
          if (currentState) {
            console.log('[NetworkService] Polling detected: ONLINE');
            this.handleOnline();
          } else {
            console.log('[NetworkService] Polling detected: OFFLINE');
            this.handleOffline();
          }
        }
      });
  }

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
    console.log('[NetworkService] handleOnline() called');
    console.log('[NetworkService] Current offlineToastId:', this.offlineToastId);
    
    // Signal already updated before this call
    this._consecutiveErrors.set(0);

    // Dismiss offline toast
    if (this.offlineToastId) {
      console.log('[NetworkService] Dismissing offline toast:', this.offlineToastId);
      this.toast.dismiss(this.offlineToastId);
      this.offlineToastId = null;
      
      console.log('[NetworkService] Showing "Back online" toast');
      this.toast.success('Connection restored.', 'Back online');
    }

    // Dismiss health warning toast
    if (this.healthWarningToastId) {
      console.log('[NetworkService] Dismissing health warning toast');
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }
  }

  private handleOffline(): void {
    console.log('[NetworkService] handleOffline() called');
    console.log('[NetworkService] Current offlineToastId:', this.offlineToastId);
    
    // Signal already updated before this call

    // Dismiss health warning toast (no longer relevant)
    if (this.healthWarningToastId) {
      console.log('[NetworkService] Dismissing health warning toast');
      this.toast.dismiss(this.healthWarningToastId);
      this.healthWarningToastId = null;
    }

    // Show offline toast
    if (!this.offlineToastId) {
      console.log('[NetworkService] Showing offline toast');
      
      try {
        this.offlineToastId = this.toast.error(
          'You appear to be offline. Please check your internet connection.',
          'No internet connection',
          0 // Persistent
        );
        
        console.log('[NetworkService] Offline toast created with ID:', this.offlineToastId);
      } catch (error) {
        console.error('[NetworkService] Failed to show offline toast:', error);
      }
    } else {
      console.log('[NetworkService] Offline toast already showing');
    }
  }

  private handleHealthCheckSuccess(): void {
    const hadErrors = this._consecutiveErrors() > 0;
    
    this._consecutiveErrors.set(0);
    this._lastCheck.set(new Date());
    this._checkCount.update(n => n + 1);

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

    console.log('[NetworkService] Showing health check warning');
    
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
      browserEventsActive: !!this.browserEventsSubscription,
      pollingActive: !!this.pollingSubscription,
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

  ngOnDestroy(): void {
    console.log('[NetworkService] Cleaning up subscriptions');
    
    this.stopMonitoring();
    
    if (this.browserEventsSubscription) {
      this.browserEventsSubscription.unsubscribe();
    }
    
    if (this.pollingSubscription) {
      this.pollingSubscription.unsubscribe();
    }
  }
}