import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NetworkService } from '../services/network.service';

/**
 * Network Status Indicator Component
 * 
 * Displays a small, unobtrusive network status indicator in the header.
 * 
 * Visual States:
 * - Green dot: ✅ Online and healthy (all good)
 * - Orange dot: 🟠 Online but server unreachable
 * - Red dot: 🔴 Offline (no internet connection)
 * 
 * Features:
 * - Real-time updates using Angular signals
 * - Tooltip showing detailed status
 * - Minimal footprint (small circle with pulse animation when unhealthy)
 * - Accessible (ARIA labels and keyboard navigation)
 */
@Component({
  selector: 'app-network-status-indicator',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="network-status-indicator"
      [attr.aria-label]="statusLabel()"
      [title]="statusTooltip()"
      role="status"
      [attr.aria-live]="!status().healthy ? 'polite' : 'off'"
    >
      <div 
        class="status-dot"
        [class.status-online]="status().online && status().healthy"
        [class.status-server-issue]="status().online && !status().healthy"
        [class.status-offline]="!status().online"
        [class.pulse]="!status().healthy"
      ></div>
    </div>
  `,
  styles: [`
    .network-status-indicator {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 0.25rem;
      cursor: help;
    }

    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      transition: background-color 0.3s ease;
    }

    .status-online {
      background-color: #10b981; /* Green */
    }

    .status-server-issue {
      background-color: #f59e0b; /* Orange */
    }

    .status-offline {
      background-color: #ef4444; /* Red */
    }

    /* Pulse animation for unhealthy states */
    .pulse {
      animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    /* Hover effect */
    .network-status-indicator:hover .status-dot {
      transform: scale(1.2);
      transition: transform 0.2s ease;
    }
  `]
})
export class NetworkStatusIndicatorComponent {
  private readonly networkService = inject(NetworkService);

  // Public computed signals for template
  readonly status = this.networkService.networkState;

  readonly statusLabel = computed(() => {
    const state = this.status();
    if (!state.online) {
      return 'Network status: Offline';
    }
    if (!state.healthy) {
      return 'Network status: Server unreachable';
    }
    return 'Network status: Online';
  });

  readonly statusTooltip = computed(() => {
    const state = this.status();
    if (!state.online) {
      return 'No internet connection';
    }
    if (!state.healthy) {
      return `Server unreachable (${state.consecutiveErrors} failed attempts)`;
    }
    return 'Connected and healthy';
  });
}