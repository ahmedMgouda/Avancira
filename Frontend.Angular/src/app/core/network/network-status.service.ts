import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { fromEvent, interval, merge, of } from 'rxjs';
import { catchError, map, switchMap, timeout } from 'rxjs/operators';
import { ToastService } from '../../services/toast.service';

/**
 * Health Check Response from BFF
 */
interface HealthCheckResponse {
  status: string;
  timestamp: string;
  version?: string;
}

/**
 * Network Status Service with Health Check Integration
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized network connectivity and backend health monitoring
 * 
 * Features:
 *   ✅ Real-time online/offline detection
 *   ✅ Backend health check verification via /health endpoint
 *   ✅ Periodic connection health checks
 *   ✅ Connection quality estimation (latency)
 *   ✅ Retry mechanism for failed requests
 *   ✅ Toast notifications for status changes
 *   ✅ Zoneless compatible
 * 
 * Best Practices:
 *   - Uses dedicated /health endpoint (not /favicon.ico)
 *   - Minimal data transfer for health checks
 *   - Proper timeout handling
 *   - User-friendly toast notifications
 *   - Automatic retry on connection restore
 * 
 * Usage:
 *   Inject in components/services:
 *   private networkStatus = inject(NetworkStatusService);
 *   
 *   Check status:
 *   if (this.networkStatus.isOnline()) { ... }
 *   
 *   React to changes:
 *   effect(() => {
 *     if (this.networkStatus.isOnline()) {
 *       // Retry failed operations
 *     }
 *   });
 */
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  private readonly destroyRef = inject(DestroyRef);
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  // Configuration
  private readonly HEALTH_ENDPOINT = '/health';
  private readonly CHECK_INTERVAL = 30000; // 30 seconds
  private readonly REQUEST_TIMEOUT = 5000; // 5 seconds
  private readonly RETRY_ATTEMPTS = 3;
  private readonly RETRY_DELAY = 2000; // 2 seconds

  // State signals
  private readonly _isOnline = signal(navigator.onLine);
  private readonly _isVerifying = signal(false);
  private readonly _lastCheck = signal<number>(Date.now());
  private readonly _latency = signal<number | null>(null);
  private readonly _backendStatus = signal<string>('unknown');
  private readonly _lastNotification = signal<'online' | 'offline' | null>(null);
  
  // Public readonly signals
  readonly isOnline = this._isOnline.asReadonly();
  readonly isOffline = computed(() => !this._isOnline());
  readonly isVerifying = this._isVerifying.asReadonly();
  readonly lastCheck = this._lastCheck.asReadonly();
  readonly latency = this._latency.asReadonly();
  readonly backendStatus = this._backendStatus.asReadonly();
  
  // Connection quality based on latency
  readonly quality = computed(() => {
    const lat = this._latency();
    if (lat === null || !this._isOnline()) return 'unknown';
    if (lat < 100) return 'excellent';
    if (lat < 300) return 'good';
    if (lat < 600) return 'fair';
    return 'poor';
  });

  // Human-readable quality message
  readonly qualityMessage = computed(() => {
    const q = this.quality();
    const map: Record<string, string> = {
      excellent: 'Excellent connection',
      good: 'Good connection',
      fair: 'Fair connection',
      poor: 'Poor connection',
      unknown: 'Connection quality unknown'
    };
    return map[q] || map['unknown'];
  });

  constructor() {
    this.initializeNetworkMonitoring();
  }

  /**
   * Initialize network monitoring
   */
  private initializeNetworkMonitoring(): void {
    // Listen to browser online/offline events
    merge(
      fromEvent(window, 'online').pipe(map(() => true)),
      fromEvent(window, 'offline').pipe(map(() => false))
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(online => {
        const wasOffline = !this._isOnline();
        this._isOnline.set(online);
        
        if (online) {
          // Verify actual connectivity when browser says we're online
          this.verifyConnection().then(isActuallyOnline => {
            if (isActuallyOnline && wasOffline) {
              this.showOnlineNotification();
            }
          });
        } else {
          this._latency.set(null);
          this._backendStatus.set('unknown');
          this.showOfflineNotification();
        }
      });

    // Periodic connection check (every 30 seconds when online)
    interval(this.CHECK_INTERVAL)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.verifyConnection())
      )
      .subscribe();

    // Initial connection check
    if (navigator.onLine) {
      this.verifyConnection();
    }
  }

  /**
   * Verify actual backend connectivity via /health endpoint
   * Best practice: Use dedicated health endpoint, not favicon
   */
  verifyConnection(): Promise<boolean> {
    if (this._isVerifying()) {
      return Promise.resolve(this._isOnline());
    }

    this._isVerifying.set(true);
    const startTime = performance.now();

    return this.http.get<HealthCheckResponse>(this.HEALTH_ENDPOINT, {
      headers: { 'Cache-Control': 'no-cache' }
    })
      .pipe(
        timeout(this.REQUEST_TIMEOUT),
        map(response => {
          const endTime = performance.now();
          const latency = Math.round(endTime - startTime);
          
          this._isOnline.set(true);
          this._latency.set(latency);
          this._backendStatus.set(response.status || 'healthy');
          this._lastCheck.set(Date.now());
          this._isVerifying.set(false);
          
          return true;
        }),
        catchError((error) => {
          console.warn('Health check failed:', error);
          
          this._isOnline.set(false);
          this._latency.set(null);
          this._backendStatus.set('unhealthy');
          this._lastCheck.set(Date.now());
          this._isVerifying.set(false);
          
          return of(false);
        })
      )
      .toPromise()
      .then(result => result ?? false);
  }

  /**
   * Manual retry connection check with exponential backoff
   * Useful for "Retry" buttons or automatic retry logic
   */
  async retry(attempt: number = 1): Promise<boolean> {
    const isOnline = await this.verifyConnection();
    
    if (!isOnline && attempt < this.RETRY_ATTEMPTS) {
      // Wait before next attempt with exponential backoff
      await new Promise(resolve => 
        setTimeout(resolve, this.RETRY_DELAY * Math.pow(2, attempt - 1))
      );
      return this.retry(attempt + 1);
    }
    
    return isOnline;
  }

  /**
   * Wait for online connection
   * Useful for retrying operations after reconnection
   */
  waitForOnline(timeout = 30000): Promise<void> {
    if (this._isOnline()) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const checkInterval = setInterval(() => {
        if (this._isOnline()) {
          clearInterval(checkInterval);
          clearTimeout(timeoutHandle);
          resolve();
        }
      }, 500);

      const timeoutHandle = setTimeout(() => {
        clearInterval(checkInterval);
        reject(new Error('Network connection timeout'));
      }, timeout);

      // Cleanup on destroy
      this.destroyRef.onDestroy(() => {
        clearInterval(checkInterval);
        clearTimeout(timeoutHandle);
      });
    });
  }

  /**
   * Show online notification (only once per online event)
   */
  private showOnlineNotification(): void {
    if (this._lastNotification() !== 'online') {
      this.toast.showSuccess('Connection restored. You are back online.');
      this._lastNotification.set('online');
    }
  }

  /**
   * Show offline notification (only once per offline event)
   */
  private showOfflineNotification(): void {
    if (this._lastNotification() !== 'offline') {
      this.toast.showError('No internet connection. Please check your network.');
      this._lastNotification.set('offline');
    }
  }

  /**
   * Show backend degraded notification
   */
  showDegradedNotification(): void {
    this.toast.showWarning('Service is running with reduced functionality.');
  }

  /**
   * Get diagnostics for debugging
   */
  getDiagnostics() {
    return {
      isOnline: this._isOnline(),
      isVerifying: this._isVerifying(),
      lastCheck: new Date(this._lastCheck()),
      latency: this._latency(),
      quality: this.quality(),
      qualityMessage: this.qualityMessage(),
      backendStatus: this._backendStatus(),
      navigatorOnLine: navigator.onLine,
      connectionType: (navigator as any).connection?.effectiveType || 'unknown',
      healthEndpoint: this.HEALTH_ENDPOINT
    };
  }
}
