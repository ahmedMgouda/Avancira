/**
 * Cleanup Manager
 * ═══════════════════════════════════════════════════════════════════════
 * Phase 3: Standardized cleanup pattern for all services
 * 
 * Problem Solved:
 *   ❌ Before: Each service implements its own cleanup logic
 *   ✅ After: Unified cleanup management with automatic tracking
 * 
 * Features:
 *   ✅ Automatic timeout/interval tracking
 *   ✅ Promise cancellation support
 *   ✅ Subscription management
 *   ✅ Single cleanup() call clears everything
 *   ✅ Memory leak prevention
 */

import { Subscription } from 'rxjs';

type CleanupCallback = () => void;
type TimerId = ReturnType<typeof setTimeout> | ReturnType<typeof setInterval>;

interface PromiseHandle {
  promise: Promise<any>;
  cancel: () => void;
}

export class CleanupManager {
  private timeouts = new Set<TimerId>();
  private intervals = new Set<TimerId>();
  private subscriptions = new Set<Subscription>();
  private callbacks = new Set<CleanupCallback>();
  private promises = new Set<PromiseHandle>();
  private cleanupPerformed = false;

  /**
   * Register a timeout for automatic cleanup
   * @returns timeout ID
   */
  setTimeout(callback: () => void, delay: number): ReturnType<typeof setTimeout> {
    if (this.cleanupPerformed) {
      console.warn('[CleanupManager] Attempted to add timeout after cleanup');
      return 0 as any;
    }

    const id = setTimeout(() => {
      callback();
      this.timeouts.delete(id);
    }, delay);

    this.timeouts.add(id);
    return id;
  }

  /**
   * Register an interval for automatic cleanup
   * @returns interval ID
   */
  setInterval(callback: () => void, delay: number): ReturnType<typeof setInterval> {
    if (this.cleanupPerformed) {
      console.warn('[CleanupManager] Attempted to add interval after cleanup');
      return 0 as any;
    }

    const id = setInterval(callback, delay);
    this.intervals.add(id);
    return id;
  }

  /**
   * Clear a specific timeout
   */
  clearTimeout(id: TimerId): void {
    clearTimeout(id);
    this.timeouts.delete(id);
  }

  /**
   * Clear a specific interval
   */
  clearInterval(id: TimerId): void {
    clearInterval(id);
    this.intervals.delete(id);
  }

  /**
   * Register a subscription for automatic cleanup
   */
  addSubscription(subscription: Subscription): void {
    if (this.cleanupPerformed) {
      console.warn('[CleanupManager] Attempted to add subscription after cleanup');
      subscription.unsubscribe();
      return;
    }

    this.subscriptions.add(subscription);
  }

  /**
   * Register a custom cleanup callback
   */
  addCallback(callback: CleanupCallback): void {
    if (this.cleanupPerformed) {
      console.warn('[CleanupManager] Attempted to add callback after cleanup');
      return;
    }

    this.callbacks.add(callback);
  }

  /**
   * Register a cancellable promise
   * @param promise Promise to track
   * @param cancelFn Function to cancel the promise
   */
  addPromise(promise: Promise<any>, cancelFn: () => void): void {
    if (this.cleanupPerformed) {
      console.warn('[CleanupManager] Attempted to add promise after cleanup');
      cancelFn();
      return;
    }

    const handle: PromiseHandle = { promise, cancel: cancelFn };
    this.promises.add(handle);

    // Auto-remove when promise settles
    promise.finally(() => {
      this.promises.delete(handle);
    });
  }

  /**
   * Clean up all registered resources
   * Safe to call multiple times
   */
  cleanup(): void {
    if (this.cleanupPerformed) {
      return;
    }

    this.cleanupPerformed = true;

    // Clear all timeouts
    for (const id of this.timeouts) {
      clearTimeout(id);
    }
    this.timeouts.clear();

    // Clear all intervals
    for (const id of this.intervals) {
      clearInterval(id);
    }
    this.intervals.clear();

    // Unsubscribe all subscriptions
    for (const subscription of this.subscriptions) {
      if (!subscription.closed) {
        subscription.unsubscribe();
      }
    }
    this.subscriptions.clear();

    // Cancel all promises
    for (const handle of this.promises) {
      try {
        handle.cancel();
      } catch (error) {
        console.warn('[CleanupManager] Promise cancellation failed:', error);
      }
    }
    this.promises.clear();

    // Execute all cleanup callbacks
    for (const callback of this.callbacks) {
      try {
        callback();
      } catch (error) {
        console.error('[CleanupManager] Cleanup callback failed:', error);
      }
    }
    this.callbacks.clear();
  }

  /**
   * Check if cleanup has been performed
   */
  isCleanedUp(): boolean {
    return this.cleanupPerformed;
  }

  /**
   * Get diagnostic information
   */
  getDiagnostics() {
    return {
      timeouts: this.timeouts.size,
      intervals: this.intervals.size,
      subscriptions: this.subscriptions.size,
      callbacks: this.callbacks.size,
      promises: this.promises.size,
      isCleanedUp: this.cleanupPerformed
    };
  }
}

/**
 * Base class for services that need cleanup
 * Provides automatic cleanup via Angular's DestroyRef
 */
import { DestroyRef, inject } from '@angular/core';

export abstract class CleanableService {
  protected readonly cleanup = new CleanupManager();
  private readonly destroyRef = inject(DestroyRef);

  constructor() {
    // Automatically cleanup on destroy
    this.destroyRef.onDestroy(() => {
      this.cleanup.cleanup();
      this.onCleanup?.();
    });
  }

  /**
   * Optional hook for subclasses to implement custom cleanup
   */
  protected onCleanup?(): void;

  /**
   * Get cleanup diagnostics
   */
  getCleanupStats() {
    return this.cleanup.getDiagnostics();
  }
}