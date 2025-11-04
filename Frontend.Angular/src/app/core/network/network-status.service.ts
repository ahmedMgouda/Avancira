import { computed, DestroyRef, inject,Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, interval,merge } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

/**
 * Network Status Service
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized network connectivity monitoring
 * 
 * Features:
 *   ✅ Real-time online/offline detection
 *   ✅ Actual internet connectivity verification (not just adapter status)
 *   ✅ Periodic connection health checks
 *   ✅ Connection quality estimation (latency)
 *   ✅ Retry mechanism for failed requests
 *   ✅ Event hooks for connection changes
 *   ✅ Zoneless compatible
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
 * 
 * @example
 * export class MyComponent {
 *   private network = inject(NetworkStatusService);
 *   readonly isOnline = this.network.isOnline;
 *   readonly connectionQuality = this.network.quality;
 * }
 */
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  private readonly destroyRef = inject(DestroyRef);

  // State signals
  private readonly _isOnline = signal(navigator.onLine);
  private readonly _isVerifying = signal(false);
  private readonly _lastCheck = signal<number>(Date.now());
  private readonly _latency = signal<number | null>(null);
  
  // Public readonly signals
  readonly isOnline = this._isOnline.asReadonly();
  readonly isOffline = computed(() => !this._isOnline());
  readonly isVerifying = this._isVerifying.asReadonly();
  readonly lastCheck = this._lastCheck.asReadonly();
  readonly latency = this._latency.asReadonly();
  
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
        this._isOnline.set(online);
        
        // Verify actual connectivity when browser says we're online
        if (online) {
          this.verifyConnection();
        } else {
          this._latency.set(null);
        }
      });

    // Periodic connection check (every 30 seconds when online)
    interval(30000)
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
   * Verify actual internet connectivity (not just network adapter)
   * Tests with a lightweight request and measures latency
   */
  verifyConnection(): Promise<boolean> {
    if (this._isVerifying()) {
      return Promise.resolve(this._isOnline());
    }

    this._isVerifying.set(true);
    const startTime = performance.now();

    return fetch('/favicon.ico', {
      method: 'HEAD',
      cache: 'no-cache',
      mode: 'no-cors'
    })
      .then(() => {
        const endTime = performance.now();
        const latency = Math.round(endTime - startTime);
        
        this._isOnline.set(true);
        this._latency.set(latency);
        this._lastCheck.set(Date.now());
        this._isVerifying.set(false);
        
        return true;
      })
      .catch(() => {
        this._isOnline.set(false);
        this._latency.set(null);
        this._lastCheck.set(Date.now());
        this._isVerifying.set(false);
        
        return false;
      });
  }

  /**
   * Manual retry connection check
   * Useful for "Retry" buttons in UI
   */
  retry(): Promise<boolean> {
    return this.verifyConnection();
  }

  /**
   * Wait for online connection
   * Useful for retrying operations after reconnection
   * 
   * @param timeout Optional timeout in milliseconds (default: 30000)
   * @returns Promise that resolves when online or rejects on timeout
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
      navigatorOnLine: navigator.onLine,
      connectionType: (navigator as any).connection?.effectiveType || 'unknown'
    };
  }
}