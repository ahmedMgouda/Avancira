import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';

import { NotificationService } from '../../core/services/notification.service';

/**
 * NotificationContainerComponent
 * ---------------------------------------------------------------------------
 * Displays active notifications from NotificationService
 * Signal-driven — no RxJS subscriptions required
 * Framework-agnostic — minimal CSS, easy to theme or replace
 *
 * Usage:
 *   <app-notification-container />
 * Add it once in app.component.html or main layout template.
 */
@Component({
  selector: 'app-notification-container',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="notification-container">
      @for (n of notifications(); track n.id) {
        <div
          class="notification"
          [class.notification-success]="n.type === 'success'"
          [class.notification-error]="n.type === 'error'"
          [class.notification-warning]="n.type === 'warning'"
          [class.notification-info]="n.type === 'info'"
          role="alert"
        >
          <div class="notification-content">
            <span class="notification-icon">
              @switch (n.type) {
                @case ('success') { ✓ }
                @case ('error')   { ✕ }
                @case ('warning') { ⚠ }
                @case ('info')    { ℹ }
              }
            </span>
            <span class="notification-message">{{ n.message }}</span>
          </div>
          <button
            class="notification-close"
            (click)="close(n.id)"
            aria-label="Close notification"
          >
            ✕
          </button>
        </div>
      }
    </div>
  `,
  styles: [`
    .notification-container {
      position: fixed;
      top: 1rem;
      right: 1rem;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      max-width: 400px;
    }

    .notification {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem;
      border-radius: 0.5rem;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      animation: slideIn 0.3s ease-out;
      background: white;
      border-left: 4px solid;
    }

    @keyframes slideIn {
      from { transform: translateX(400px); opacity: 0; }
      to   { transform: translateX(0);     opacity: 1; }
    }

    .notification-success { border-left-color: #10b981; background: #ecfdf5; }
    .notification-error   { border-left-color: #ef4444; background: #fef2f2; }
    .notification-warning { border-left-color: #f59e0b; background: #fffbeb; }
    .notification-info    { border-left-color: #3b82f6; background: #eff6ff; }

    .notification-content {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      flex: 1;
    }

    .notification-icon { font-size: 1.25rem; font-weight: bold; }
    .notification-success .notification-icon { color: #10b981; }
    .notification-error   .notification-icon { color: #ef4444; }
    .notification-warning .notification-icon { color: #f59e0b; }
    .notification-info    .notification-icon { color: #3b82f6; }

    .notification-message {
      color: #374151;
      font-size: 0.875rem;
      line-height: 1.4;
    }

    .notification-close {
      background: none;
      border: none;
      color: #9ca3af;
      cursor: pointer;
      padding: 0.25rem;
      font-size: 1.25rem;
      transition: color 0.2s;
    }

    .notification-close:hover { color: #374151; }

    @media (max-width: 640px) {
      .notification-container {
        left: 1rem;
        right: 1rem;
        max-width: none;
      }
    }
  `]
})
export class NotificationContainerComponent {
  private readonly notificationService = inject(NotificationService);
  readonly notifications = this.notificationService.notifications;

  close(id: string): void {
    this.notificationService.remove(id);
  }
}
