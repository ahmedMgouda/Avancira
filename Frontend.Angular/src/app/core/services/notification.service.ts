import { inject, Injectable, signal } from '@angular/core';

import { LoggerService } from './logger.service';

import { AppError } from '../models/error.model';

import { environment } from '@/environments/environment';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface Notification {
  id: string;
  type: NotificationType;
  message: string;
  duration?: number;
  timestamp: Date;
  correlationId?: string;
}

/**
 * Toast notification service
 * ─────────────────────────────────────────────────────────────
 * Reactive signal-based state
 * Logs all notifications
 * Auto-dismiss + bounded queue
 * Can accept AppError directly
 * 
 * DESIGN CHANGE: This service now owns ALL notification policy:
 *   - Environment flag checks
 *   - Status code filtering
 *   - Severity-based decisions
 * 
 * This creates a single source of truth for "when to show toasts"
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly logger = inject(LoggerService);

  private readonly _notifications = signal<Notification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  private idCounter = 0;
  private readonly maxVisible = 5;

  private readonly defaultDurations: Record<NotificationType, number> = {
    success: 4000,
    info: 5000,
    warning: 6000,
    error: 7000
  };

  // ─────────────────────────────────────────────────────────
  // Public methods
  // ─────────────────────────────────────────────────────────

  success(message: string, duration?: number): void {
    this.show('success', message, duration);
  }

  error(message: string, duration?: number): void {
    this.show('error', message, duration);
  }

  warning(message: string, duration?: number): void {
    this.show('warning', message, duration);
  }

  info(message: string, duration?: number): void {
    this.show('info', message, duration);
  }

  /**
   * Shows a notification derived from an AppError
   * (used by global error interceptor)
   * 
   * ENHANCEMENT: Now handles ALL notification policy centrally
   * 
   * @param appError - The normalized error object
   * @param options - Optional overrides for notification policy
   */
  fromAppError(appError: AppError, options?: NotificationOptions): void {
    // ─────────────────────────────────────────────────────────
    // POLICY CHECK 1: Environment flag
    // ─────────────────────────────────────────────────────────
    if (environment.disableNotifications && !options?.force) {
      this.logger.debug('Notification suppressed by environment config', {
        errorCode: appError.code,
        correlationId: appError.correlationId
      });
      return;
    }

    // ─────────────────────────────────────────────────────────
    // POLICY CHECK 2: Status code filtering
    // ─────────────────────────────────────────────────────────
    const skipStatuses = options?.skipStatuses ?? [404];
    if (appError.status && skipStatuses.includes(appError.status)) {
      this.logger.debug(`Notification skipped for status ${appError.status}`, {
        errorCode: appError.code,
        correlationId: appError.correlationId
      });
      return;
    }

    // ─────────────────────────────────────────────────────────
    // POLICY CHECK 3: Severity-based filtering
    // ─────────────────────────────────────────────────────────
    const silentLevels: AppError['severity'][] = ['info', 'warning'];
    if (appError.severity && silentLevels.includes(appError.severity) && !options?.force) {
      this.logger.debug(`Notification skipped for severity ${appError.severity}`, {
        errorCode: appError.code,
        correlationId: appError.correlationId
      });
      return;
    }

    // ─────────────────────────────────────────────────────────
    // All checks passed - show notification
    // ─────────────────────────────────────────────────────────
    const correlationId = appError.correlationId ?? this.logger.getCorrelationId() ?? undefined;
    const type = this.mapSeverityToType(appError.severity);
    const message = appError.message || 'An unexpected error occurred.';
    const duration = options?.duration ?? environment.errorToastDuration ?? this.defaultDurations[type];

    this.show(type, message, duration, correlationId);
  }

  clearAll(): void {
    this._notifications.set([]);
  }

  remove(id: string): void {
    this._notifications.update(list => list.filter(n => n.id !== id));
  }

  // ─────────────────────────────────────────────────────────
  // Core logic
  // ─────────────────────────────────────────────────────────

  private show(
    type: NotificationType,
    message: string,
    duration?: number,
    correlationId?: string
  ): void {
    const finalDuration = duration ?? this.defaultDurations[type];

    const notification: Notification = {
      id: `notification-${++this.idCounter}`,
      type,
      message,
      duration: finalDuration,
      timestamp: new Date(),
      correlationId
    };

    // Add to queue
    this._notifications.update(list => {
      const updated = [...list, notification];
      return updated.length > this.maxVisible
        ? updated.slice(-this.maxVisible)
        : updated;
    });

    // Log
    this.logger.info(`Notification shown: ${type}`, {
      message,
      correlationId
    });

    // Auto-dismiss
    if (finalDuration > 0) {
      setTimeout(() => this.remove(notification.id), finalDuration);
    }
  }

  // ─────────────────────────────────────────────────────────
  // Utility
  // ─────────────────────────────────────────────────────────

  private mapSeverityToType(severity?: AppError['severity']): NotificationType {
    switch (severity) {
      case 'info': return 'info';
      case 'warning': return 'warning';
      case 'error':
      case 'critical': return 'error';
      default: return 'error';
    }
  }
}

// ─────────────────────────────────────────────────────────
// Type Definitions
// ─────────────────────────────────────────────────────────

/**
 * NEW: Options for notification behavior overrides
 */
export interface NotificationOptions {
  /** Force notification even if environment.disableNotifications = true */
  force?: boolean;

  /** Custom list of status codes to skip (default: [404]) */
  skipStatuses?: number[];

  /** Custom duration override */
  duration?: number;
}