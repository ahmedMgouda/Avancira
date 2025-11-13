import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';

import { NetworkService } from '@core/network/services/network.service';

@Component({
  selector: 'app-network-indicator',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div 
      class="network-indicator" 
      [class.online]="isOnline()"
      [class.offline]="!isOnline()"
      [class.unhealthy]="!isHealthy()"
      [title]="tooltipText()"
    >
      <span class="status-dot"></span>
      <span class="status-text">{{ statusText() }}</span>
    </div>
  `,
  styles: [`
    .network-indicator {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 12px;
      border-radius: 20px;
      font-size: 13px;
      font-weight: 500;
      transition: all 0.3s ease;
      cursor: help;
      user-select: none;
    }

    .network-indicator.online {
      background-color: rgba(16, 185, 129, 0.1);
      color: rgb(16, 185, 129);
    }

    .network-indicator.unhealthy {
      background-color: rgba(245, 158, 11, 0.1);
      color: rgb(245, 158, 11);
    }

    .network-indicator.offline {
      background-color: rgba(239, 68, 68, 0.1);
      color: rgb(239, 68, 68);
    }

    .status-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      animation: pulse 2s ease-in-out infinite;
    }

    .network-indicator.online .status-dot {
      background-color: rgb(16, 185, 129);
    }

    .network-indicator.unhealthy .status-dot {
      background-color: rgb(245, 158, 11);
    }

    .network-indicator.offline .status-dot {
      background-color: rgb(239, 68, 68);
      animation: pulse 1s ease-in-out infinite;
    }

    .status-text {
      font-weight: 500;
      white-space: nowrap;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    /* Responsive: Hide text on small screens */
    @media (max-width: 768px) {
      .status-text {
        display: none;
      }
      
      .network-indicator {
        padding: 6px;
      }
    }
  `]
})
export class NetworkIndicatorComponent {
  private readonly network = inject(NetworkService);

  readonly isOnline = computed(() => this.network.networkState().online);
  readonly isHealthy = computed(() => this.network.networkState().healthy);
  readonly consecutiveErrors = computed(() => this.network.networkState().consecutiveErrors);

  readonly statusText = computed(() => {
    if (!this.isOnline()) {
      return 'Offline';
    }
    if (!this.isHealthy()) {
      return 'Server issue';
    }
    return 'Online';
  });

  readonly tooltipText = computed(() => {
    if (!this.isOnline()) {
      return 'No internet connection';
    }
    if (!this.isHealthy()) {
      const errors = this.consecutiveErrors();
      return `Server unreachable (${errors} failed ${errors === 1 ? 'attempt' : 'attempts'})`;
    }
    return 'Connected and healthy';
  });
}