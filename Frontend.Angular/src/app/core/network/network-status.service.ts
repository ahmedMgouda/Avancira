import { HttpBackend, HttpClient } from '@angular/common/http';
import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, interval, merge } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { ResilienceService } from '../services/resilience.service';
import { ToastService } from '../toast/toast.service';

import { ConnectionQuality, HealthCheckResponse, NetworkDiagnostics } from './health-check.model';
import { NETWORK_STATUS_CONFIG } from './network-status.config';

/**
 * Network Status Service - Industry Best Practice Implementation
 * ═══════════════════════════════════════════════════════════════════════
 * Follows Kubernetes/Docker health check patterns
 * Uses HttpBackend to bypass interceptors (recommended for health checks)
 * 
 * FIXES IMPLEMENTED:
 *   ✅ Promise deduplication prevents duplicate health checks
 *   ✅ Reactive toast management with effect()
 *   ✅ Proper cleanup of all resources
 *   ✅ Centralized state management
 * 
 * Architecture:
 *   - Bypasses ALL interceptors using HttpBackend (no circular dependencies)
 *   - Uses BFF /health endpoint (aggregates backend service health)
 *   - Implements exponential backoff via ResilienceService
 *   - Signal-based reactive state management
 * 
 * BFF Health Check Pattern:
 *   Frontend → BFF /health → Backend Services
 *   The BFF aggregates health from all downstream services
 */
@Injectable({ providedIn: 'root' })
export class NetworkStatusService {
  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(ToastService);
  private readonly resilienceService = inject(ResilienceService);
  private readonly config = inject(NETWORK_STATUS_CONFIG);
  
  /**
   * Interceptor-free HttpClient for health checks
   * Best Practice: Health checks should NOT go through interceptors
   * This prevents circular dependencies and ensures pure connectivity testing
   */
  private readonly httpClient: HttpClient;

  // ────────────────────────────────────────────────────────────────
  // CONFIGURATION
  // ────────────────────────────────────────────────────────────────
  
  private readonly HEALTH_ENDPOINT: string;
  private readonly CHECK_INTERVAL: number;
  private readonly MAX_ATTEMPTS: number;

  // ────────────────────────────────────────────────────────────────
  // SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  private readonly _isOnline = signal(navigator.onLine);
  private readonly _isVerifying = signal(false);
  private readonly _lastCheck = signal<number>(Date.now());
  private readonly _latency = signal<number | null>(null);
  private readonly _wasOffline = signal(false);
  
  // ────────────────────────────────────────────────────────────────
  // DEDUPLICATION & CLEANUP
  // ────────────────────────────────────────────────────────────────
  
  /**
   * FIX #1: Promise deduplication prevents duplicate health checks
   * If verification is in progress, return the existing promise
   */
  private verificationPromise: Promise<boolean> | null = null;
  
  /**
   * Track active wait promises for cleanup
   */
  private readonly activeWaitPromises = new Set<{
    interval: number;
    timeout: number;
  }>();

  /**
   * FIX #2: Toast management via effect (reactive)
   * Single source of truth for toast state
   */
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
  
  // Connection quality based on latency (standard thresholds)
  readonly quality = computed<ConnectionQuality>(() => {
    const lat = this._latency();
    if (lat === null || !this._isOnline()) return 'unknown';
    if (lat < 100) return 'excellent';  // < 100ms
    if (lat < 300) return 'good';       // 100-300ms
    if (lat < 600) return 'fair';       // 300-600ms
    return 'poor';                       // > 600ms
  });

  readonly qualityMessage = computed(() => {
    const q = this.quality();
    const map: Record<ConnectionQuality, string> = {
      excellent: 'Excellent connection',
      good: 'Good connection',
      fair: 'Fair connection',
      poor: 'Poor connection',
      unknown: 'Connection quality unknown'
    };
    return map[q];
  });

  // Status icon for UI
  readonly statusIcon = computed(() => {
    if (this._isVerifying()) return '⏳';
    if (!this._isOnline()) return '📴';
    
    const q = this.quality();
    const icons: Record<ConnectionQuality, string> = {
      excellent: '🟢',
      good: '🟡',
      fair: '🟠',
      poor: '🔴',
      unknown: '⚪'
    };
    return icons[q];
  });

  constructor(handler: HttpBackend) {
    // Create interceptor-free HttpClient
    // Best Practice: Health checks bypass all interceptors
    this.httpClient = new HttpClient(handler);
    
    // Load configuration
    this.HEALTH_ENDPOINT = this.config.healthEndpoint || '/health';
    this.CHECK_INTERVAL = this.config.checkInterval || 30000;
    this.MAX_ATTEMPTS = this.config.maxAttempts || 3;
    
    this.initializeNetworkMonitoring();
    this.initializeToastManagement();
  }

  // ────────────────────────────────────────────────────────────────
  // PUBLIC METHODS
  // ────────────────────────────────────────────────────────────────

  /**
   * FIX #1: Promise deduplication prevents duplicate health checks
   * Verify actual internet connectivity with exponential backoff retry
   * Calls BFF /health endpoint which aggregates backend service health
   */
  verifyConnection(): Promise<boolean> {
    // Return existing promise if verification already in progress
    if (this.verificationPromise) {
      return this.verificationPromise;
    }

    this._isVerifying.set(true);
    
    this.verificationPromise = this.attemptConnection(1)
      .finally(() => {
        // Clear cache and verification state
        this.verificationPromise = null;
        this._isVerifying.set(false);
      });
      
    return this.verificationPromise;
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
   * Standard timeout: 30 seconds (Kubernetes default)
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
  getDiagnostics(): NetworkDiagnostics {
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
      retryStrategy: this.resilienceService.strategy(),
      healthEndpoint: this.HEALTH_ENDPOINT,
      checkInterval: this.CHECK_INTERVAL,
      maxAttempts: this.MAX_ATTEMPTS,
      bypassesInterceptors: true
    };
  }

  // ────────────────────────────────────────────────────────────────
  // PRIVATE METHODS
  // ────────────────────────────────────────────────────────────────

  /**
   * Initialize network monitoring (browser events and periodic checks)
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
          // Verify actual connectivity (browser might lie)
          this.verifyConnection().then(isActuallyOnline => {
            if (isActuallyOnline && this._wasOffline()) {
              this._wasOffline.set(false);
            }
          });
        } else {
          // Mark as offline
          this._latency.set(null);
          this._wasOffline.set(true);
        }
      });

    // Periodic connection check (every 30s - industry standard)
    interval(this.CHECK_INTERVAL)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        switchMap(() => this.verifyConnection())
      )
      .subscribe(isOnline => {
        // Detect silent disconnection
        if (!isOnline && !this._wasOffline()) {
          this._wasOffline.set(true);
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
        this.offlineToastId = null;
      }
    });
  }

  /**
   * FIX #2: Centralized toast management using effect()
   * Automatically reacts to online/offline state changes
   * Single source of truth for toast behavior
   */
  private initializeToastManagement(): void {
    effect(() => {
      const online = this._isOnline();
      const wasOff = this._wasOffline();
      
      if (!online) {
        // Show offline toast if not already showing
        if (!this.offlineToastId) {
          this.offlineToastId = this.toastService.showWithAction(
            'warning',
            'No internet connection',
            {
              label: 'Retry',
              action: () => this.retry()
            },
            'Offline',
            0 // Permanent until dismissed
          );
        }
      } else {
        // Online - dismiss offline toast if exists
        if (this.offlineToastId) {
          this.toastService.dismiss(this.offlineToastId);
          this.offlineToastId = null;
          
          // Show reconnection success if was offline
          if (wasOff) {
            this.toastService.success(
              'Connection restored',
              'Back Online',
              3000
            );
            this._wasOffline.set(false);
          }
        }
      }
    });
  }

  /**
   * Attempt connection with retry using ResilienceService
   * Implements exponential backoff (industry standard)
   */
  private attemptConnection(attempt: number): Promise<boolean> {
    const startTime = performance.now();

    return this.httpClient
      .get<HealthCheckResponse>(this.HEALTH_ENDPOINT, {
        headers: { 'Cache-Control': 'no-cache' }
      })
      .toPromise()
      .then((response) => {
        const endTime = performance.now();
        const latency = Math.round(endTime - startTime);
        
        // Validate response structure
        if (!response || !response.status) {
          throw new Error('Invalid health check response');
        }
        
        // Standard: healthy and degraded = online
        // Only 'unhealthy' status = offline
        const isHealthy = response.status === 'healthy' || response.status === 'degraded';
        
        if (isHealthy) {
          this._isOnline.set(true);
          this._latency.set(latency);
          this._lastCheck.set(Date.now());
          
          // Log degraded status for monitoring
          if (response.status === 'degraded') {
            console.warn('[NetworkStatus] BFF reporting degraded health', {
              latency,
              endpoint: this.HEALTH_ENDPOINT
            });
          }
          
          return true;
        }
        
        throw new Error(`BFF unhealthy: ${response.status}`);
      })
      .catch((error) => {
        // Retry with exponential backoff
        if (attempt < this.MAX_ATTEMPTS) {
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
        
        console.warn('[NetworkStatus] BFF health check failed after all retries:', {
          attempt,
          maxAttempts: this.MAX_ATTEMPTS,
          endpoint: this.HEALTH_ENDPOINT,
          error: error?.message || error
        });
        
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