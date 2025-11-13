import { Component, Input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * WiFi Icon Component
 * 
 * Visual representation of network status using WiFi signal icon
 * 
 * States:
 * - full: Online and healthy (3 bars, green)
 * - weak: Server unreachable (2 bars, orange)
 * - off: Offline (slash through, red)
 */

type NetworkStatus = 'online' | 'server-issue' | 'offline';

@Component({
  selector: 'app-wifi-icon',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 24 24"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      [attr.aria-label]="ariaLabel()"
    >
      <!-- WiFi Full (Online) -->
      @if (status === 'online') {
        <path
          d="M12 18h.01M9.51 14.5a5 5 0 0 1 5.17 0M6.75 10.75a9 9 0 0 1 10.5 0M3 6.5C6.49 3.5 10.3 2 12 2c1.7 0 5.51 1.5 9 4.5"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      }

      <!-- WiFi Weak (Server Issue) -->
      @if (status === 'server-issue') {
        <path
          d="M12 18h.01M9.51 14.5a5 5 0 0 1 5.17 0"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <path
          d="M6.75 10.75a9 9 0 0 1 10.5 0"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          opacity="0.3"
        />
      }

      <!-- WiFi Off (Offline) -->
      @if (status === 'offline') {
        <path
          d="M12 18h.01"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <path
          d="M9.51 14.5a5 5 0 0 1 5.17 0M6.75 10.75a9 9 0 0 1 10.5 0"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
          stroke-linejoin="round"
          opacity="0.3"
        />
        <!-- Diagonal slash -->
        <line
          x1="4"
          y1="4"
          x2="20"
          y2="20"
          [attr.stroke]="color()"
          stroke-width="2"
          stroke-linecap="round"
        />
      }
    </svg>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        align-items: center;
        justify-content: center;
      }
    `
  ]
})
export class WifiIconComponent {
  @Input() status: NetworkStatus = 'online';
  @Input() size: number = 20;

  color = computed(() => {
    switch (this.status) {
      case 'online':
        return '#10b981'; // Green
      case 'server-issue':
        return '#f59e0b'; // Orange
      case 'offline':
        return '#ef4444'; // Red
      default:
        return '#6b7280'; // Gray
    }
  });

  ariaLabel = computed(() => {
    switch (this.status) {
      case 'online':
        return 'Network online';
      case 'server-issue':
        return 'Server unreachable';
      case 'offline':
        return 'Network offline';
      default:
        return 'Network status';
    }
  });
}
