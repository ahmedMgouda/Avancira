import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, interval, merge } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { ResilienceService } from '../services/resilience.service';
import { ToastService } from '../toast/toast.service';

/**
 * Network Status Service (Angular 19 - Signal-Based)
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized network connectivity monitoring with toast notifications
 * Uses ResilienceService for retry logic
 * 
 * Features:
 *   ✅ Real-time online/offline detection
 *   ✅ Actual internet connectivity verification
 *   ✅ Connection quality estimation (latency)
 *   ✅ Toast notifications for connection changes
 *   ✅ Uses ResilienceService for exponential backoff
 *   ✅ Angular 19 signal-based API
 */
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(ToastService);
  private readonly resilienceService = inject(ResilienceService);

  // ────────────────────────────────────────────────────────────────
  // SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  private readonly _isOnline = signal(navigator.onLine);
  private readonly _isVerifying = signal(false);
  private readonly _lastCheck = signal<number>(Date.now());
  private readonly _latency = signal<number | null>(null);
  private readonly _wasOffline = signal(false);
  
  // Track active wait promises for cleanup
  private readonly activeWaitPromises = new Set<{
    interval: number;
    timeout: number;
  }>();

  // Track toast IDs to prevent duplicates
  private offlineToastId: string | null = null;
  
  // ────────────────────────────────────────────────────────────────
  // PUBLIC SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  readonly isOnline = this._isOnline.asReadonly();
  readonly isOffline = computed(() => !this._isOnline());
  readonly isVerifying = this._isVerifying.asReadonly();
  readonly lastCheck = this._lastCheck.asReadonly();
  readonly latency = this._latency.asReadonly();
  readonly wasOffline = this._wasOffline.asReadonly();
  
  // Connection quality based on latency
  readonly quality = computed(() => {
    const lat = this._latency();
    if (lat === null || !this._isOnline()) return 'unknown';
    if (lat < 100) return 'excellent';
    if (lat < 300) return 'good';
    if (lat < 600) return 'fair';
    return 'poor';
  });

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

  // Status icon for UI
  readonly statusIcon = computed(() => {
    if (this._isVerifying()) return '⏳';
    if (!this._isOnline()) return '📴';
    
    const q = this.quality();
    const icons: Record<string, string> = {
      excellent: '🟢',
      good: '🟡',
      fair: '🟠',
      poor: '🔴',
      unknown: '⚪'
    };
    return icons[q] || icons['unknown'];
  });

  constructor() {
    this.initializeNetworkMonitoring();
  }

  // ────────────────────────────────────────────────────────────────
  // PUBLIC METHODS
  // ────────────────────────────────────────────────────────────────

  /**
   * Verify actual internet connectivity with exponential backoff retry
   */
  verifyConnection(): Promise<boolean> {
    if (this._isVerifying()) {
      return Promise.resolve(this._isOnline());
    }

    this._isVerifying.set(true);
    return this.attemptConnection(1);
  }

  /**
   * Manual retry with toast feedback
   */
  retry(): Promise<boolean> {
    const toastId = this.toastService.info('Checking connection...', 'Retrying', 0);
    
    return this.verifyConnection().then(isOnline => {
      this.toastService.dismiss(toastId);
      
      if (isOnline) {
        this.toastService.success(
          'Connection restored',
          'Connected',
          3000
        );
        this._wasOffline.set(false);
        
        // Dismiss offline toast
        if (this.offlineToastId) {
          this.toastService.dismiss(this.offlineToastId);
          this.offlineToastId = null;
        }
      } else {
        this.toastService.error(
          'Still offline. Please check your network.',
          'Connection Failed',
          5000
        );
      }
      
      return isOnline;
    });
  }

  /**
   * Wait for online connection with proper cleanup
   */
  waitForOnline(timeout = 30000): Promise<void> {
    if (this._isOnline()) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const checkInterval = window.setInterval(() => {
        if (this._isOnline()) {
          cleanup();
          resolve();
        }
      }, 500);

      const timeoutHandle = window.setTimeout(() => {
        cleanup();
        reject(new Error('Network connection timeout'));
      }, timeout);

      const handles = { interval: checkInterval, timeout: timeoutHandle };
      this.activeWaitPromises.add(handles);

      const cleanup = () => {
        clearInterval(checkInterval);
        clearTimeout(timeoutHandle);
        this.activeWaitPromises.delete(handles);
      };
    });
  }

  /**
   * Get diagnostics for debugging
   */
  getDiagnostics() {
    return {
      isOnline: this._isOnline(),
      isOffline: this.isOffline(),
      isVerifying: this._isVerifying(),
      wasOffline: this._wasOffline(),
      lastCheck: new Date(this._lastCheck()),
      latency: this._latency(),
      quality: this.quality(),
      qualityMessage: this.qualityMessage(),
      statusIcon: this.statusIcon(),
      navigatorOnLine: navigator.onLine,
      connectionType: (navigator as any).connection?.effectiveType || 'unknown',
      activeWaitCount: this.activeWaitPromises.size,
      retryStrategy: this.resilienceService.strategy()
    };
  }

  // ────────────────────────────────────────────────────────────────
  // PRIVATE METHODS
  // ────────────────────────────────────────────────────────────────

  /**
   * Initialize network monitoring with toast notifications
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
        
        if (online) {
          // Dismiss offline toast if showing
          if (this.offlineToastId) {
            this.toastService.dismiss(this.offlineToastId);
            this.offlineToastId = null;
          }

          // Verify actual connectivity
          this.verifyConnection().then(isActuallyOnline => {
            if (isActuallyOnline && this._wasOffline()) {
              this.toastService.success(
                'Connection restored',
                'Back Online',
                3000
              );
              this._wasOffline.set(false);
            }
          });
        } else {
          // Show offline toast
          this._latency.set(null);
          this._wasOffline.set(true);
          
          this.offlineToastId = this.toastService.showWithAction(
            'warning',
            'No internet connection',
            {
              label: 'Retry',
              action: () => this.retry()
            },
            'Offline',
            0 // Permanent
          );
        }
      });

    // Periodic connection check (every 30 seconds when online)
    interval(30000)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.verifyConnection())
      )
      .subscribe(isOnline => {
        // Detect silent disconnection
        if (!isOnline && !this._wasOffline()) {
          this._wasOffline.set(true);
          this.offlineToastId = this.toastService.warning(
            'Connection lost',
            'Offline',
            0
          );
        }
      });

    // Initial connection check
    if (navigator.onLine) {
      this.verifyConnection();
    } else {
      this._wasOffline.set(true);
    }

    // Cleanup on destroy
    this.destroyRef.onDestroy(() => {
      this.cleanupActiveWaitPromises();
      if (this.offlineToastId) {
        this.toastService.dismiss(this.offlineToastId);
      }
    });
  }

  /**
   * Attempt connection with retry using ResilienceService
   */
  private attemptConnection(attempt: number): Promise<boolean> {
    const startTime = performance.now();
    const maxAttempts = 3;

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
        // Use ResilienceService for retry delay calculation
        if (attempt < maxAttempts) {
          const delay = this.resilienceService.calculateDelay(attempt);

          return new Promise<boolean>((resolve) => {
            setTimeout(() => {
              resolve(this.attemptConnection(attempt + 1));
            }, delay);
          });
        }

        // All attempts failed
        this._isOnline.set(false);
        this._latency.set(null);
        this._lastCheck.set(Date.now());
        this._isVerifying.set(false);
        
        return false;
      });
  }

  /**
   * Cleanup all active wait promises
   */
  private cleanupActiveWaitPromises(): void {
    for (const handles of this.activeWaitPromises) {
      clearInterval(handles.interval);
      clearTimeout(handles.timeout);
    }
    this.activeWaitPromises.clear();
  }
}