import { computed, Injectable, signal } from '@angular/core';

import { getToastDisplayConfig } from '../../config/toast.config';
import { IdGenerator } from '../../utils/id-generator.utility';
import { Toast, ToastAction, ToastType } from '../models/toast.model';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toasts = signal<Toast[]>([]);
  private readonly config = getToastDisplayConfig();

  readonly activeToasts = computed(() =>
    this.toasts().slice(0, this.config.maxVisible)
  );

  readonly hasActiveToasts = computed(() => this.toasts().length > 0);

  readonly queuedCount = computed(() =>
    Math.max(0, this.toasts().length - this.config.maxVisible)
  );

  /**
   * Show a toast notification
   */
  show(
    type: ToastType,
    message: string,
    title?: string,
    duration?: number,
    action?: ToastAction,
    dismissible = true
  ): string {
    const toast: Toast = {
      id: IdGenerator.generateUUID(),
      type,
      title,
      message,
      duration: duration ?? this.config.defaultDuration,
      dismissible,
      action,
      icon: this.getIcon(type),
      timestamp: Date.now()
    };

    this.toasts.update(toasts => [...toasts, toast]);

    // Auto-dismiss if duration specified and > 0
    if (toast.duration && toast.duration > 0) {
      setTimeout(() => this.dismiss(toast.id), toast.duration);
    }

    return toast.id;
  }

  /**
   * Dismiss a specific toast
   */
  dismiss(id: string): void {
    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  /**
   * Dismiss all toasts
   */
  dismissAll(): void {
    this.toasts.set([]);
  }

  /**
   * Get icon for toast type
   */
  private getIcon(type: ToastType): string {
    const icons: Record<ToastType, string> = {
      success: '✓',
      error: '✕',
      warning: '⚠',
      info: 'ℹ'
    };
    return icons[type];
  }

  /**
   * Get diagnostics
   */
  getDiagnostics() {
    return {
      totalToasts: this.toasts().length,
      activeToasts: this.activeToasts().length,
      queuedToasts: this.queuedCount(),
      config: this.config
    };
  }
}