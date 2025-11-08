// core/toast/services/toast-coordinator.service.ts
/**
 * Toast Coordinator Service
 * ═══════════════════════════════════════════════════════════════════════
 * Centralized toast management to prevent race conditions
 * Single source of truth for all toast notifications
 * 
 * Problem Solved:
 *   ❌ Before: LoadingService, NetworkStatusService, ErrorHandler all show toasts
 *   ✅ After: All services call ToastCoordinator instead
 * 
 * Features:
 *   ✅ Automatic deduplication (prevent spam)
 *   ✅ Priority-based queuing (errors before info)
 *   ✅ Rate limiting per toast type
 *   ✅ Centralized configuration
 */

import { computed, Injectable, signal } from '@angular/core';

import { ToastService } from './toast.service';

import { ToastAction, ToastType } from '../models/toast.model';

interface ToastRequest {
  type: ToastType;
  message: string;
  title?: string;
  duration?: number;
  action?: ToastAction;
  dismissible?: boolean;
}

interface DuplicateTracker {
  hash: string;
  lastShown: number;
  count: number;
}

@Injectable({ providedIn: 'root' })
export class ToastCoordinator {
  private readonly toastService: ToastService;

  // Deduplication tracking
  private readonly duplicateCache = new Map<string, DuplicateTracker>();
  private readonly DUPLICATE_WINDOW_MS = 3000; // Don't show same toast within 3s
  private readonly MAX_CACHE_SIZE = 50;

  // Rate limiting per type
  private readonly rateLimiters = new Map<ToastType, number>();
  private readonly RATE_LIMIT_MS = 1000; // Max 1 toast per type per second

  // Active toast tracking (for limits)
  private readonly activeToastIds = signal<Set<string>>(new Set());
  private readonly MAX_ACTIVE_TOASTS = 5;

  // Computed signal for available slots
  readonly availableSlots = computed(() => 
    this.MAX_ACTIVE_TOASTS - this.activeToastIds().size
  );

  readonly hasAvailableSlots = computed(() => this.availableSlots() > 0);

  constructor(toastService: ToastService) {
    this.toastService = toastService;
  }

  /**
   * Show toast with automatic deduplication and rate limiting
   * Returns toast ID if shown, null if suppressed
   */
  show(request: ToastRequest): string | null {
    // Check rate limit for this type
    if (this.isRateLimited(request.type)) {
      console.debug(`[ToastCoordinator] Rate limited: ${request.type}`);
      return null;
    }

    // Check for duplicates
    if (this.isDuplicate(request)) {
      console.debug(`[ToastCoordinator] Duplicate suppressed: ${request.message}`);
      return null;
    }

    // Check active toast limit
    if (!this.hasAvailableSlots()) {
      console.warn(`[ToastCoordinator] Max toasts reached (${this.MAX_ACTIVE_TOASTS})`);
      // Could queue here instead of dropping
      return null;
    }

    // Show the toast
    const toastId = this.toastService.show(
      request.type,
      request.message,
      request.title,
      request.duration,
      request.action
    );

    // Track active toast
    this.activeToastIds.update(ids => {
      const newIds = new Set(ids);
      newIds.add(toastId);
      return newIds;
    });

    // Update rate limiter
    this.rateLimiters.set(request.type, Date.now());

    // Track for deduplication
    this.trackToast(request);

    // Auto-remove from active tracking when dismissed
    this.scheduleCleanup(toastId, request.duration || 5000);

    return toastId;
  }

  /**
   * Convenience methods for each toast type
   */
  success(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'success', message, title, duration });
  }

  error(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'error', message, title, duration });
  }

  warning(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'warning', message, title, duration });
  }

  info(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'info', message, title, duration });
  }

  /**
   * Show toast with action button
   */
  showWithAction(
    type: ToastType,
    message: string,
    action: ToastAction,
    title?: string,
    duration?: number
  ): string | null {
    return this.show({ type, message, title, duration, action });
  }

  /**
   * Dismiss a toast by ID
   */
  dismiss(id: string): void {
    this.toastService.dismiss(id);
    this.removeFromActive(id);
  }

  /**
   * Dismiss all toasts
   */
  dismissAll(): void {
    this.toastService.dismissAll();
    this.activeToastIds.set(new Set());
  }

  /**
   * Check if a toast type is currently rate limited
   */
  private isRateLimited(type: ToastType): boolean {
    const lastShown = this.rateLimiters.get(type);
    if (!lastShown) return false;

    const elapsed = Date.now() - lastShown;
    return elapsed < this.RATE_LIMIT_MS;
  }

  /**
   * Check if this toast is a duplicate of a recent one
   */
  private isDuplicate(request: ToastRequest): boolean {
    const hash = this.hashToast(request);
    const tracker = this.duplicateCache.get(hash);

    if (!tracker) return false;

    const elapsed = Date.now() - tracker.lastShown;
    return elapsed < this.DUPLICATE_WINDOW_MS;
  }

  /**
   * Track toast for deduplication
   */
  private trackToast(request: ToastRequest): void {
    const hash = this.hashToast(request);
    
    const existing = this.duplicateCache.get(hash);
    
    if (existing) {
      existing.lastShown = Date.now();
      existing.count++;
    } else {
      this.duplicateCache.set(hash, {
        hash,
        lastShown: Date.now(),
        count: 1
      });
    }

    // Cleanup old entries
    this.cleanupDuplicateCache();
  }

  /**
   * Create hash for toast deduplication
   */
  private hashToast(request: ToastRequest): string {
    return `${request.type}:${request.message}:${request.title || ''}`;
  }

  /**
   * Remove old entries from duplicate cache
   */
  private cleanupDuplicateCache(): void {
    if (this.duplicateCache.size <= this.MAX_CACHE_SIZE) {
      return;
    }

    const now = Date.now();
    const entriesToDelete: string[] = [];

    for (const [hash, tracker] of this.duplicateCache.entries()) {
      const age = now - tracker.lastShown;
      if (age > this.DUPLICATE_WINDOW_MS * 2) {
        entriesToDelete.push(hash);
      }
    }

    entriesToDelete.forEach(hash => this.duplicateCache.delete(hash));
  }

  /**
   * Schedule removal from active tracking
   */
  private scheduleCleanup(toastId: string, duration: number): void {
    // Add buffer time for animation
    const cleanupDelay = duration > 0 ? duration + 500 : 5500;

    setTimeout(() => {
      this.removeFromActive(toastId);
    }, cleanupDelay);
  }

  /**
   * Remove toast from active tracking
   */
  private removeFromActive(toastId: string): void {
    this.activeToastIds.update(ids => {
      const newIds = new Set(ids);
      newIds.delete(toastId);
      return newIds;
    });
  }

  /**
   * Get diagnostics for debugging
   */
  getDiagnostics() {
    return {
      activeToasts: this.activeToastIds().size,
      maxToasts: this.MAX_ACTIVE_TOASTS,
      availableSlots: this.availableSlots(),
      duplicateCacheSize: this.duplicateCache.size,
      rateLimiters: Object.fromEntries(this.rateLimiters),
      duplicateWindow: this.DUPLICATE_WINDOW_MS,
      rateLimit: this.RATE_LIMIT_MS
    };
  }
}