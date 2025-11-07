import { computed, Injectable, signal } from '@angular/core';

/**
 * Network Error Tracker Service
 * ═══════════════════════════════════════════════════════════════════════
 * Tracks network error state using Angular signals
 * Replaces the fragile __isNetworkError flag pattern
 * 
 * Features:
 *   ✅ Type-safe signal-based state management
 *   ✅ Tracks error timing and frequency
 *   ✅ No object mutations
 *   ✅ Reactive computed properties
 *   ✅ Clean separation of concerns
 * 
 * Usage:
 *   // In networkInterceptor
 *   if (isNetworkError(error)) {
 *     errorTracker.markNetworkError();
 *   }
 * 
 *   // In retryInterceptor
 *   if (errorTracker.hasRecentNetworkError()) {
 *     return throwError(() => error); // Don't retry
 *   }
 */
@Injectable({ providedIn: 'root' })
export class NetworkErrorTracker {
  // ────────────────────────────────────────────────────────────────
  // SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  private readonly _hasNetworkError = signal(false);
  private readonly _lastNetworkError = signal<Date | null>(null);
  private readonly _errorCount = signal(0);
  private readonly _consecutiveErrors = signal(0);
  private readonly _lastSuccess = signal<Date | null>(null);
  
  // ────────────────────────────────────────────────────────────────
  // PUBLIC READONLY SIGNALS
  // ────────────────────────────────────────────────────────────────
  
  readonly hasNetworkError = this._hasNetworkError.asReadonly();
  readonly lastNetworkError = this._lastNetworkError.asReadonly();
  readonly errorCount = this._errorCount.asReadonly();
  readonly consecutiveErrors = this._consecutiveErrors.asReadonly();
  readonly lastSuccess = this._lastSuccess.asReadonly();
  
  /**
   * Check if there's a recent network error (within last 5 seconds)
   * Used by retry interceptor to avoid immediate retries during network issues
   */
  readonly hasRecentNetworkError = computed(() => {
    const last = this._lastNetworkError();
    if (!last) return false;
    
    const RECENT_THRESHOLD_MS = 5000;
    return Date.now() - last.getTime() < RECENT_THRESHOLD_MS;
  });
  
  /**
   * Network is unstable if multiple consecutive errors
   */
  readonly isUnstable = computed(() => this._consecutiveErrors() >= 3);
  
  /**
   * Time since last network error (in milliseconds)
   */
  readonly timeSinceLastError = computed(() => {
    const last = this._lastNetworkError();
    return last ? Date.now() - last.getTime() : null;
  });
  
  // ────────────────────────────────────────────────────────────────
  // PUBLIC METHODS
  // ────────────────────────────────────────────────────────────────
  
  /**
   * Mark that a network error occurred
   */
  markNetworkError(): void {
    this._hasNetworkError.set(true);
    this._lastNetworkError.set(new Date());
    this._errorCount.update(count => count + 1);
    this._consecutiveErrors.update(count => count + 1);
  }
  
  /**
   * Mark that a request succeeded (clears network error state)
   */
  markSuccess(): void {
    this._hasNetworkError.set(false);
    this._consecutiveErrors.set(0);
    this._lastSuccess.set(new Date());
  }
  
  /**
   * Clear network error state
   */
  clearNetworkError(): void {
    this._hasNetworkError.set(false);
    this._consecutiveErrors.set(0);
  }
  
  /**
   * Reset all state (useful for testing or manual reset)
   */
  reset(): void {
    this._hasNetworkError.set(false);
    this._lastNetworkError.set(null);
    this._errorCount.set(0);
    this._consecutiveErrors.set(0);
    this._lastSuccess.set(null);
  }
  
  /**
   * Get diagnostics for debugging
   */
  getDiagnostics() {
    return {
      hasNetworkError: this._hasNetworkError(),
      hasRecentNetworkError: this.hasRecentNetworkError(),
      isUnstable: this.isUnstable(),
      lastNetworkError: this._lastNetworkError(),
      errorCount: this._errorCount(),
      consecutiveErrors: this._consecutiveErrors(),
      lastSuccess: this._lastSuccess(),
      timeSinceLastError: this.timeSinceLastError()
    };
  }
}