import { inject, Injectable } from '@angular/core';

import { ToastService } from './toast.service';
import { getToastDeduplicationConfig } from '../../config/toast.config';
import { ToastAction, ToastRecord, ToastRequest, ToastType } from '../models/toast.model';

/**
 * ═══════════════════════════════════════════════════════════════════════
 * TOAST MANAGER - FIXED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ Added onDismiss callback support to all methods
 * ✅ Deduplication window no longer extends indefinitely
 * ✅ Uses firstShown timestamp for window calculation
 */

@Injectable({ providedIn: 'root' })
export class ToastManager {
  private readonly toastService = inject(ToastService);
  private readonly config = getToastDeduplicationConfig();

  private readonly recentToasts = new Map<string, ToastRecord>();
  private readonly dismissCallbacks = new Map<string, () => void>();
  private cleanupTimer?: ReturnType<typeof setInterval>;

  constructor() {
    this.initializeCleanup();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Public API - IMPROVED
  // ═══════════════════════════════════════════════════════════════════════

  show(request: ToastRequest, onDismiss?: () => void): string | null {
    if (!this.config.enabled) {
      return this.showDirectly(request, onDismiss);
    }

    const hash = this.createHash(request);
    const recent = this.recentToasts.get(hash);

    // FIX: Use firstShown for window calculation
    if (recent && this.isWithinWindow(recent.firstShown)) {
      return this.handleDuplicate(request, recent);
    }

    return this.showNewToast(request, hash, onDismiss);
  }

  // FIX: Added onDismiss parameter
  success(message: string, title?: string, duration?: number, onDismiss?: () => void): string | null {
    return this.show({ type: 'success', message, title, duration }, onDismiss);
  }

  // FIX: Added onDismiss parameter
  error(message: string, title?: string, duration?: number, onDismiss?: () => void): string | null {
    return this.show({ type: 'error', message, title, duration }, onDismiss);
  }

  // FIX: Added onDismiss parameter
  warning(message: string, title?: string, duration?: number, onDismiss?: () => void): string | null {
    return this.show({ type: 'warning', message, title, duration }, onDismiss);
  }

  // FIX: Added onDismiss parameter
  info(message: string, title?: string, duration?: number, onDismiss?: () => void): string | null {
    return this.show({ type: 'info', message, title, duration }, onDismiss);
  }

  // FIX: Added onDismiss parameter
  showWithAction(
    type: ToastType,
    message: string,
    action: ToastAction,
    title?: string,
    duration?: number,
    onDismiss?: () => void
  ): string | null {
    return this.show({ type, message, title, duration, action }, onDismiss);
  }

  dismiss(id: string): void {
    // Trigger callback if exists
    const callback = this.dismissCallbacks.get(id);
    if (callback) {
      callback();
      this.dismissCallbacks.delete(id);
    }

    this.toastService.dismiss(id);
  }

  dismissAll(): void {
    // Trigger all callbacks
    this.dismissCallbacks.forEach(callback => callback());
    this.dismissCallbacks.clear();

    this.toastService.dismissAll();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Private Methods - IMPROVED
  // ═══════════════════════════════════════════════════════════════════════

  private createHash(request: ToastRequest): string {
    return `${request.type}:${request.title || ''}:${request.message}`;
  }

  private isWithinWindow(firstShown: Date): boolean {
    const elapsed = Date.now() - firstShown.getTime();
    return elapsed < this.config.windowMs;
  }

  // FIX: No longer updates lastShown - uses firstShown for window
  private handleDuplicate(request: ToastRequest, record: ToastRecord): string | null {
    record.suppressedCount++;

    if (record.suppressedCount >= this.config.maxSuppressedBeforeShow) {
      const summaryId = this.showSuppressedSummary(request, record.suppressedCount);
      record.suppressedCount = 0;
      return summaryId;
    }

    return null;
  }

  private showNewToast(request: ToastRequest, hash: string, onDismiss?: () => void): string {
    const toastId = this.showDirectly(request, onDismiss);

    // FIX: Track firstShown timestamp (never updated)
    const now = new Date();
    this.recentToasts.set(hash, {
      hash,
      firstShown: now,
      lastShown: now,
      suppressedCount: 0,
      type: request.type,
      message: request.message,
      title: request.title
    });

    return toastId;
  }

  private showDirectly(request: ToastRequest, onDismiss?: () => void): string {
    const toastId = this.toastService.show(
      request.type,
      request.message,
      request.title,
      request.duration,
      request.action,
      request.dismissible
    );

    // Store callback if provided
    if (onDismiss) {
      this.dismissCallbacks.set(toastId, onDismiss);
    }

    return toastId;
  }

  private showSuppressedSummary(request: ToastRequest, count: number): string | null {
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

  // ═══════════════════════════════════════════════════════════════════════
  // Cleanup Management - IMPROVED
  // ═══════════════════════════════════════════════════════════════════════

  private initializeCleanup(): void {
    this.cleanupTimer = setInterval(() => {
      this.cleanupOldRecords();
    }, this.config.cleanupIntervalMs);
  }

  // FIX: Uses firstShown for cleanup (not lastShown)
  private cleanupOldRecords(): void {
    const now = Date.now();
    const cutoff = now - this.config.windowMs;

    for (const [hash, record] of this.recentToasts.entries()) {
      if (record.firstShown.getTime() < cutoff) {
        this.recentToasts.delete(hash);
      }
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Diagnostics
  // ═══════════════════════════════════════════════════════════════════════

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
      activeCallbacks: this.dismissCallbacks.size,
      config: this.config,
      toastService: this.toastService.getDiagnostics()
    };
  }

  getSuppressionStats(type: ToastType, message: string, title?: string): {
    suppressedCount: number;
    firstShown: Date | null;
    lastShown: Date | null;
  } {
    const hash = this.createHash({ type, message, title });
    const record = this.recentToasts.get(hash);

    return {
      suppressedCount: record?.suppressedCount || 0,
      firstShown: record?.firstShown || null,
      lastShown: record?.lastShown || null
    };
  }

  ngOnDestroy(): void {
    if (this.cleanupTimer) {
      clearInterval(this.cleanupTimer);
    }
    this.dismissCallbacks.clear();
  }
}
