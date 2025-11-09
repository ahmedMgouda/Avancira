
// core/toast/services/toast-coordinator.service.ts
/**
 * Toast Coordinator Service
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * Uses DedupManager (removed inline deduplication)
 */

import { computed, inject, Injectable, signal } from '@angular/core';

import { ToastService } from './toast.service';

import { getDeduplicationConfig } from '../../config/deduplication.config';
import { DedupManager } from '../../utils/dedup-manager.utility';
import { ToastAction, ToastType } from '../models/toast.model';

interface ToastRequest {
  type: ToastType;
  message: string;
  title?: string;
  duration?: number;
  action?: ToastAction;
  dismissible?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ToastManager {
  private readonly toastService = inject(ToastService);

  // Use DedupManager for toast deduplication
  private readonly dedup = new DedupManager<ToastRequest>({
    ...getDeduplicationConfig().toasts,
    hashFn: (toast) => `${toast.type}:${toast.message}:${toast.title || ''}`
  });

  // Rate limiting per type
  private readonly rateLimiters = new Map<ToastType, number>();
  private readonly RATE_LIMIT_MS = 1000;

  // Active toast tracking
  private readonly activeToastIds = signal<Set<string>>(new Set());
  private readonly MAX_ACTIVE_TOASTS = 5;

  readonly availableSlots = computed(() =>
    this.MAX_ACTIVE_TOASTS - this.activeToastIds().size
  );

  readonly hasAvailableSlots = computed(() => this.availableSlots() > 0);

  show(request: ToastRequest): string | null {
    // Check rate limit
    if (this.isRateLimited(request.type)) {
      return null;
    }

    // Check deduplication
    if (this.dedup.check(request)) {
      return null;
    }

    // Check slots
    if (!this.hasAvailableSlots()) {
      return null;
    }

    const toastId = this.toastService.show(
      request.type,
      request.message,
      request.title,
      request.duration,
      request.action
    );

    this.activeToastIds.update(ids => {
      const newIds = new Set(ids);
      newIds.add(toastId);
      return newIds;
    });

    this.rateLimiters.set(request.type, Date.now());
    this.scheduleCleanup(toastId, request.duration || 5000);

    return toastId;
  }

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

  showWithAction(
    type: ToastType,
    message: string,
    action: ToastAction,
    title?: string,
    duration?: number
  ): string | null {
    return this.show({ type, message, title, duration, action });
  }

  dismiss(id: string): void {
    this.toastService.dismiss(id);
    this.removeFromActive(id);
  }

  dismissAll(): void {
    this.toastService.dismissAll();
    this.activeToastIds.set(new Set());
  }

  private isRateLimited(type: ToastType): boolean {
    const lastShown = this.rateLimiters.get(type);
    if (!lastShown) return false;

    const elapsed = Date.now() - lastShown;
    return elapsed < this.RATE_LIMIT_MS;
  }

  private scheduleCleanup(toastId: string, duration: number): void {
    const cleanupDelay = duration > 0 ? duration + 500 : 5500;

    setTimeout(() => {
      this.removeFromActive(toastId);
    }, cleanupDelay);
  }

  private removeFromActive(toastId: string): void {
    this.activeToastIds.update(ids => {
      const newIds = new Set(ids);
      newIds.delete(toastId);
      return newIds;
    });
  }

  getDiagnostics() {
    return {
      activeToasts: this.activeToastIds().size,
      maxToasts: this.MAX_ACTIVE_TOASTS,
      availableSlots: this.availableSlots(),
      deduplication: this.dedup.getStats()
    };
  }
}