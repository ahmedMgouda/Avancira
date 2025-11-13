import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NetworkService } from '../services/network.service';
import { NetworkNotificationService } from '../services/network-notification.service';
import { WifiIconComponent } from './wifi-icon.component';

/**
 * Network Status Indicator Component
 * 
 * Displays WiFi icon with network status and provides manual retry
 * 
 * Visual States:
 * - Green WiFi (full): Online and healthy
 * - Orange WiFi (weak): Online but server unreachable (pulsing)
 * - Red WiFi (off): Offline (pulsing)
 * 
 * Features:
 * - Real-time updates using Angular signals
 * - Clickable for manual retry
 * - Tooltip showing detailed status
 * - Loading state during retry
 */
@Component({
  selector: 'app-network-status-indicator',
  standalone: true,
  imports: [CommonModule, WifiIconComponent],
  template: `
    <button
      type="button"
      class="network-status-button"
      [attr.aria-label]="statusLabel()"
      [title]="statusTooltip()"
      [disabled]="isRetrying()"
      (click)="onRetry()"
      [class.can-retry]="!status().healthy"
      [class.retrying]="isRetrying()"
    >
      <div
        class="icon-wrapper"
        [class.pulse]="!status().healthy"
        [class.spin]="isRetrying()"
      >
        <app-wifi-icon [status]="iconStatus()" [size]="20" />
      </div>
    </button>
  `,
  styles: [
    `
      .network-status-button {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.5rem;
        background: transparent;
        border: none;
        cursor: help;
        border-radius: 0.375rem;
        transition: background-color 0.2s;

        &:hover:not(:disabled) {
          background-color: rgba(0, 0, 0, 0.05);
        }

        &.can-retry {
          cursor: pointer;

          &:hover:not(:disabled) {
            background-color: rgba(0, 0, 0, 0.1);
          }
        }

        &:disabled {
          opacity: 0.6;
          cursor: not-allowed;
        }

        &:focus-visible {
          outline: 2px solid #3b82f6;
          outline-offset: 2px;
        }
      }

      .icon-wrapper {
        display: flex;
        align-items: center;
        justify-content: center;
        transition: transform 0.2s;
      }

      .network-status-button:hover:not(:disabled) .icon-wrapper {
        transform: scale(1.1);
      }

      /* Pulse animation for unhealthy states */
      .pulse {
        animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
      }

      @keyframes pulse {
        0%,
        100% {
          opacity: 1;
        }
        50% {
          opacity: 0.5;
        }
      }

      /* Spin animation for retry */
      .spin {
        animation: spin 1s linear infinite;
      }

      @keyframes spin {
        from {
          transform: rotate(0deg);
        }
        to {
          transform: rotate(360deg);
        }
      }
    `
  ]
})
export class NetworkStatusIndicatorComponent {
  private readonly networkService = inject(NetworkService);
  private readonly notificationService = inject(NetworkNotificationService);

  readonly status = this.networkService.networkState;
  readonly isRetrying = this.notificationService.isRetrying;

  readonly iconStatus = computed(() => {
    const state = this.status();
    if (!state.online) return 'offline';
    if (!state.healthy) return 'server-issue';
    return 'online';
  });

  readonly statusLabel = computed(() => {
    if (this.isRetrying()) return 'Retrying connection...';
    const state = this.status();
    if (!state.online) return 'Network offline - Click to retry';
    if (!state.healthy) return 'Server unreachable - Click to retry';
    return 'Network online';
  });

  readonly statusTooltip = computed(() => {
    if (this.isRetrying()) return 'Retrying connection...';
    const state = this.status();
    if (!state.online) return 'No internet connection. Click to retry.';
    if (!state.healthy)
      return `Server unreachable (${state.consecutiveErrors} failed attempts). Click to retry.`;
    return 'Connected and healthy';
  });

  onRetry(): void {
    const state = this.status();
    // Only retry if unhealthy
    if (!state.healthy && !this.isRetrying()) {
      this.notificationService.retryConnection();
    }
  }
}
