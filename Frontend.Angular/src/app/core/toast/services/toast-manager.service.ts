import { inject, Injectable } from '@angular/core';

import { ToastService } from './toast.service';

import { getToastDeduplicationConfig } from '../../config/toast.config';
import { ToastAction, ToastRecord, ToastRequest, ToastType } from '../models/toast.model';

@Injectable({ providedIn: 'root' })
export class ToastManager {
  private readonly toastService = inject(ToastService);
  private readonly config = getToastDeduplicationConfig();

  // Time-window based tracking
  private readonly recentToasts = new Map<string, ToastRecord>();
  private cleanupTimer?: ReturnType<typeof setInterval>;

  constructor() {
    this.initializeCleanup();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Show a toast with deduplication
   */
  show(request: ToastRequest): string | null {
    if (!this.config.enabled) {
      return this.toastService.show(
        request.type,
        request.message,
        request.title,
        request.duration,
        request.action,
        request.dismissible
      );
    }

    const hash = this.createHash(request);
    const recent = this.recentToasts.get(hash);

    // Check if shown recently (within time window)
    if (recent && this.isWithinWindow(recent.lastShown)) {
      return this.handleDuplicate(request, recent);
    }

    // Show new toast
    return this.showNewToast(request, hash);
  }

  /**
   * Show success toast
   */
  success(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'success', message, title, duration });
  }

  /**
   * Show error toast
   */
  error(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'error', message, title, duration });
  }

  /**
   * Show warning toast
   */
  warning(message: string, title?: string, duration?: number): string | null {
    return this.show({ type: 'warning', message, title, duration });
  }

  /**
   * Show info toast
   */
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
   * Dismiss a specific toast
   */
  dismiss(id: string): void {
    this.toastService.dismiss(id);
  }

  /**
   * Dismiss all toasts
   */
  dismissAll(): void {
    this.toastService.dismissAll();
  }

  // ═══════════════════════════════════════════════════════════════════
  // Private Methods - Deduplication Logic
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Create hash for deduplication
   * Based on: type + title + message (what user sees)
   */
  private createHash(request: ToastRequest): string {
    return `${request.type}:${request.title || ''}:${request.message}`;
  }

  /**
   * Check if toast was shown within time window
   */
  private isWithinWindow(lastShown: Date): boolean {
    const elapsed = Date.now() - lastShown.getTime();
    return elapsed < this.config.windowMs;
  }

  /**
   * Handle duplicate toast
   */
  private handleDuplicate(request: ToastRequest, record: ToastRecord): string | null {
    // Update last shown time
    record.lastShown = new Date();
    
    // Increment suppression counter
    record.suppressedCount++;

    // After threshold, show summary toast
    if (record.suppressedCount >= this.config.maxSuppressedBeforeShow) {
      const summaryId = this.showSuppressedSummary(request, record.suppressedCount);
      
      // Reset counter after showing summary
      record.suppressedCount = 0;
      
      return summaryId;
    }

    // Silently suppressed
    return null;
  }

  /**
   * Show new toast and record it
   */
  private showNewToast(request: ToastRequest, hash: string): string {
    const toastId = this.toastService.show(
      request.type,
      request.message,
      request.title,
      request.duration,
      request.action,
      request.dismissible
    );

    // Record this toast
    this.recentToasts.set(hash, {
      hash,
      lastShown: new Date(),
      suppressedCount: 0,
      type: request.type,
      message: request.message,
      title: request.title
    });

    return toastId;
  }

  /**
   * Show summary toast for suppressed notifications
   */
  private showSuppressedSummary(
    request: ToastRequest,
    count: number
  ): string | null {
    if (!this.config.showSuppressedCount) {
      return null;
    }

    const summaryMessage = `${request.message} (${count} similar suppressed)`;

    return this.toastService.show(
      request.type,
      summaryMessage,
      request.title,
      request.duration,
      request.action,
      request.dismissible
    );
  }

  // ═══════════════════════════════════════════════════════════════════
  // Cleanup Management
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Initialize periodic cleanup of old records
   */
  private initializeCleanup(): void {
    this.cleanupTimer = setInterval(() => {
      this.cleanupOldRecords();
    }, this.config.cleanupIntervalMs);
  }

  /**
   * Remove records outside time window
   */
  private cleanupOldRecords(): void {
    const now = Date.now();
    const cutoff = now - this.config.windowMs;

    for (const [hash, record] of this.recentToasts.entries()) {
      if (record.lastShown.getTime() < cutoff) {
        this.recentToasts.delete(hash);
      }
    }
  }

  // ═══════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════

  /**
   * Get diagnostic information
   */
  getDiagnostics() {
    const totalSuppressed = Array.from(this.recentToasts.values())
      .reduce((sum, record) => sum + record.suppressedCount, 0);

    const suppressionsByType = Array.from(this.recentToasts.values())
      .reduce((acc, record) => {
        acc[record.type] = (acc[record.type] || 0) + record.suppressedCount;
        return acc;
      }, {} as Record<ToastType, number>);

    return {
      recentToastsTracked: this.recentToasts.size,
      totalSuppressed,
      suppressionsByType,
      config: this.config,
      toastService: this.toastService.getDiagnostics()
    };
  }

  /**
   * Get suppression stats for specific toast
   */
  getSuppressionStats(type: ToastType, message: string, title?: string): {
    suppressedCount: number;
    lastShown: Date | null;
  } {
    const hash = this.createHash({ type, message, title });
    const record = this.recentToasts.get(hash);

    return {
      suppressedCount: record?.suppressedCount || 0,
      lastShown: record?.lastShown || null
    };
  }

  /**
   * Cleanup on destroy
   */
  ngOnDestroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }
  }
}