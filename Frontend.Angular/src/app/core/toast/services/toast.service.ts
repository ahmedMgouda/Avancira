// core/toast/services/toast.service.ts
/**
 * Toast Service - SIMPLIFIED
 * ═══════════════════════════════════════════════════════════════════════
 * 
 * CHANGES FROM ORIGINAL (200 → 120 lines, -40%):
 * ✅ Removed deduplication (moved to coordinator)
 * ✅ Removed history tracking (coordinator's job)
 * ✅ Pure operations only (dumb service)
 */

import { computed, Injectable, signal } from '@angular/core';

import { IdGenerator } from '../../utils/id-generator.utility';
import { Toast, ToastAction, ToastConfig, ToastType } from '../models/toast.model';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toasts = signal<Toast[]>([]);

  private readonly config: Required<ToastConfig> = {
    maxVisible: 5,
    defaultDuration: 5000,
    position: 'top-right',
    enableHistory: false,
    preventDuplicates: false,
    duplicateTimeout: 0
  };

  readonly activeToasts = computed(() =>
    this.toasts().slice(0, this.config.maxVisible)
  );

  readonly hasActiveToasts = computed(() => this.toasts().length > 0);

  readonly queuedCount = computed(() =>
    Math.max(0, this.toasts().length - this.config.maxVisible)
  );

  // ✅ Pure operations (no business logic)
  show(
    type: ToastType,
    message: string,
    title?: string,
    duration?: number,
    action?: ToastAction
  ): string {
    const toast: Toast = {
      id: IdGenerator.generateUUID(),
      type,
      title,
      message,
      duration: duration ?? this.config.defaultDuration,
      dismissible: true,
      action,
      icon: this.getIcon(type),
      timestamp: Date.now()
    };

    this.toasts.update(toasts => [...toasts, toast]);

    if (toast.duration && toast.duration > 0) {
      setTimeout(() => this.dismiss(toast.id), toast.duration);
    }

    return toast.id;
  }

  dismiss(id: string): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  dismissAll(): void {
    this.toasts.set([]);
  }

  configure(config: Partial<ToastConfig>): void {
    Object.assign(this.config, config);
  }

  getConfig(): Readonly<Required<ToastConfig>> {
    return { ...this.config };
  }

  private getIcon(type: ToastType): string {
    const icons: Record<ToastType, string> = {
      success: '✓',
      error: '✕',
      warning: '⚠',
      info: 'ℹ'
    };
    return icons[type];
  }
}