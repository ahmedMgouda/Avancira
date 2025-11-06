import { computed, Injectable, signal } from '@angular/core';

import { BrowserCompat } from '../logging/utils/browser-compat.util';
import { Toast, ToastAction, ToastConfig, ToastType } from './toast.model';

/**
 * Toast Service (Production-Ready)
 * ═══════════════════════════════════════════════════════════════════════
 * Manages toast notifications with duplicate prevention
 * 
 * Features:
 *   ✅ Multiple toast types (success, error, warning, info)
 *   ✅ Action buttons
 *   ✅ Auto-dismiss with configurable duration
 *   ✅ Duplicate prevention
 *   ✅ Toast history
 *   ✅ Max visible toasts
 *   ✅ Production-ready (no debug logs)
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly toasts = signal<Toast[]>([]);
  private readonly history = signal<Toast[]>([]);
  private readonly recentMessages = new Map<string, number>();
  private readonly dismissTimers = new Map<string, number>();

  private readonly config: Required<ToastConfig> = {
    maxVisible: 5,
    defaultDuration: 5000,
    position: 'top-right',
    enableHistory: true,
    preventDuplicates: true,
    duplicateTimeout: 3000
  };

  readonly activeToasts = computed(() =>
    this.toasts().slice(0, this.config.maxVisible)
  );
  
  readonly toastHistory = computed(() => this.history());
  readonly hasActiveToasts = computed(() => this.toasts().length > 0);
  readonly queuedCount = computed(() => 
    Math.max(0, this.toasts().length - this.config.maxVisible)
  );

  // ────────────────────────────────────────────────────────────────
  // PUBLIC API
  // ────────────────────────────────────────────────────────────────

  success(message: string, title?: string, duration?: number): string {
    return this.show('success', message, title, duration);
  }

  error(message: string, title?: string, duration?: number): string {
    return this.show('error', message, title, duration);
  }

  warning(message: string, title?: string, duration?: number): string {
    return this.show('warning', message, title, duration);
  }

  info(message: string, title?: string, duration?: number): string {
    return this.show('info', message, title, duration);
  }

  showWithAction(
    type: ToastType,
    message: string,
    action: ToastAction,
    title?: string,
    duration?: number
  ): string {
    return this.show(type, message, title, duration, action);
  }

  dismiss(id: string): void {
    const timer = this.dismissTimers.get(id);
    if (timer) {
      window.clearTimeout(timer);
      this.dismissTimers.delete(id);
    }

    this.toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  dismissAll(): void {
    for (const timer of this.dismissTimers.values()) {
      window.clearTimeout(timer);
    }
    this.dismissTimers.clear();
    this.toasts.set([]);
  }

  clearHistory(): void {
    this.history.set([]);
  }

  configure(config: Partial<ToastConfig>): void {
    Object.assign(this.config, config);
  }

  getConfig(): Readonly<Required<ToastConfig>> {
    return { ...this.config };
  }

  // ────────────────────────────────────────────────────────────────
  // INTERNAL
  // ────────────────────────────────────────────────────────────────

  private show(
    type: ToastType,
    message: string,
    title?: string,
    duration?: number,
    action?: ToastAction
  ): string {
    // Check for duplicate
    if (this.config.preventDuplicates && this.isDuplicate(message)) {
      return ''; // Suppressed
    }

    const toast: Toast = {
      id: BrowserCompat.generateUUID(),
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

    // Add to history
    if (this.config.enableHistory) {
      this.history.update(history => [...history, toast].slice(-100));
    }

    // Track for duplicate detection
    if (this.config.preventDuplicates) {
      this.recentMessages.set(message, Date.now());
      this.cleanupRecentMessages();
    }

    // Auto-dismiss
    if (toast.duration && toast.duration > 0) {
      const timer = window.setTimeout(() => {
        this.dismiss(toast.id);
      }, toast.duration);
      
      this.dismissTimers.set(toast.id, timer);
    }

    return toast.id;
  }

  private isDuplicate(message: string): boolean {
    const lastShown = this.recentMessages.get(message);
    if (!lastShown) return false;

    const timeSince = Date.now() - lastShown;
    return timeSince < this.config.duplicateTimeout;
  }

  private cleanupRecentMessages(): void {
    const now = Date.now();
    for (const [message, timestamp] of this.recentMessages.entries()) {
      if (now - timestamp > this.config.duplicateTimeout) {
        this.recentMessages.delete(message);
      }
    }
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