import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NetworkService } from '../../services/network.service';

/**
 * NetworkStatusIndicator Component
 * ═══════════════════════════════════════════════════════════════════════
 * Displays real-time network connection status in the dashboard header
 * 
 * Features:
 * - Green indicator when online and healthy
 * - Red indicator when offline or server unreachable
 * - Tooltip with detailed status information
 * - Accessible with ARIA labels
 * - Signal-based reactive updates
 */
@Component({
  selector: 'app-network-status-indicator',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="network-status-indicator"
      [class.online]="status().isHealthy"
      [class.offline]="!status().isHealthy"
      [attr.aria-label]="ariaLabel()"
      [title]="tooltipText()"
      role="status"
      aria-live="polite"
    >
      <div class="status-dot"></div>
      <span class="status-text" *ngIf="showText">{{ statusText() }}</span>
    </div>
  `,
  styles: [`
    .network-status-indicator {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.25rem 0.75rem;
      border-radius: 1rem;
      cursor: help;
      transition: all 0.3s ease;
      user-select: none;
    }

    .network-status-indicator:hover {
      transform: scale(1.05);
    }

    .status-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      transition: all 0.3s ease;
      box-shadow: 0 0 0 0 rgba(0, 0, 0, 0.4);
      animation: pulse 2s infinite;
    }

    .online .status-dot {
      background-color: #22c55e;
      box-shadow: 0 0 8px rgba(34, 197, 94, 0.6);
    }

    .offline .status-dot {
      background-color: #ef4444;
      box-shadow: 0 0 8px rgba(239, 68, 68, 0.6);
      animation: pulse-error 1s infinite;
    }

    @keyframes pulse {
      0% {
        box-shadow: 0 0 0 0 rgba(34, 197, 94, 0.7);
      }
      70% {
        box-shadow: 0 0 0 6px rgba(34, 197, 94, 0);
      }
      100% {
        box-shadow: 0 0 0 0 rgba(34, 197, 94, 0);
      }
    }

    @keyframes pulse-error {
      0% {
        box-shadow: 0 0 0 0 rgba(239, 68, 68, 0.7);
      }
      70% {
        box-shadow: 0 0 0 6px rgba(239, 68, 68, 0);
      }
      100% {
        box-shadow: 0 0 0 0 rgba(239, 68, 68, 0);
      }
    }

    .status-text {
      font-size: 0.875rem;
      font-weight: 500;
      white-space: nowrap;
    }

    .online .status-text {
      color: #22c55e;
    }

    .offline .status-text {
      color: #ef4444;
    }

    /* High contrast mode support */
    @media (prefers-contrast: high) {
      .online .status-dot {
        border: 2px solid #166534;
      }
      
      .offline .status-dot {
        border: 2px solid #7f1d1d;
      }
    }

    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .network-status-indicator,
      .status-dot {
        transition: none;
        animation: none;
      }
    }
  `]
})
export class NetworkStatusIndicatorComponent {
  private readonly networkService = inject(NetworkService);

  /**
   * Show status text next to the indicator
   * Default: false (icon only)
   */
  showText = false;

  /**
   * Get current network status from service
   */
  protected readonly status = computed(() => {
    const state = this.networkService.networkState();
    return {
      isHealthy: state.healthy && state.online,
      online: state.online,
      consecutiveErrors: state.consecutiveErrors,
      lastCheck: state.lastCheck
    };
  });

  /**
   * Compute status text for display
   */
  protected readonly statusText = computed(() => {
    return this.status().isHealthy ? 'Online' : 'Offline';
  });

  /**
   * Compute tooltip text with detailed information
   */
  protected readonly tooltipText = computed(() => {
    const status = this.status();
    
    if (status.isHealthy) {
      return 'Connected - All systems operational';
    }

    if (!status.online) {
      return 'No internet connection - Please check your network';
    }

    return `Server unreachable - ${status.consecutiveErrors} failed attempts`;
  });

  /**
   * Compute ARIA label for accessibility
   */
  protected readonly ariaLabel = computed(() => {
    const status = this.status();
    return status.isHealthy 
      ? 'Network status: Connected and healthy'
      : 'Network status: Connection issues detected';
  });
}