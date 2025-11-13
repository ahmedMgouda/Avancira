import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '@core/theme';

/**
 * ThemeToggleComponent
 * ═══════════════════════════════════════════════════════════════════════
 * Toggle button for switching between light and dark themes
 * 
 * Features:
 * - Icon-based toggle button
 * - Smooth transitions
 * - Keyboard accessible
 * - ARIA labels for screen readers
 * - Tooltip with current theme
 */
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      class="theme-toggle"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-pressed]="isDark()"
      [title]="tooltipText()"
      (click)="toggleTheme()"
    >
      <svg
        class="theme-icon"
        [class.sun]="!isDark()"
        [class.moon]="isDark()"
        width="20"
        height="20"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        stroke-linejoin="round"
      >
        <!-- Sun Icon (Light Mode) -->
        <ng-container *ngIf="!isDark()">
          <circle cx="12" cy="12" r="5"></circle>
          <line x1="12" y1="1" x2="12" y2="3"></line>
          <line x1="12" y1="21" x2="12" y2="23"></line>
          <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
          <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
          <line x1="1" y1="12" x2="3" y2="12"></line>
          <line x1="21" y1="12" x2="23" y2="12"></line>
          <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
          <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
        </ng-container>

        <!-- Moon Icon (Dark Mode) -->
        <ng-container *ngIf="isDark()">
          <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"></path>
        </ng-container>
      </svg>
    </button>
  `,
  styles: [`
    .theme-toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.5rem;
      height: 2.5rem;
      padding: 0;
      border: none;
      border-radius: 0.5rem;
      background-color: transparent;
      cursor: pointer;
      transition: all 0.2s ease;
      color: var(--color-text-primary, #000000);
    }

    .theme-toggle:hover {
      background-color: var(--color-surface-hover, rgba(0, 0, 0, 0.05));
      transform: scale(1.05);
    }

    .theme-toggle:active {
      transform: scale(0.95);
    }

    .theme-toggle:focus-visible {
      outline: 2px solid var(--color-primary, #0066cc);
      outline-offset: 2px;
    }

    .theme-icon {
      transition: all 0.3s ease;
    }

    .theme-icon.sun {
      color: #f59e0b;
      animation: rotate 8s linear infinite;
    }

    .theme-icon.moon {
      color: #60a5fa;
      animation: moonFloat 3s ease-in-out infinite;
    }

    @keyframes rotate {
      from {
        transform: rotate(0deg);
      }
      to {
        transform: rotate(360deg);
      }
    }

    @keyframes moonFloat {
      0%, 100% {
        transform: translateY(0);
      }
      50% {
        transform: translateY(-3px);
      }
    }

    /* Reduced motion support */
    @media (prefers-reduced-motion: reduce) {
      .theme-toggle,
      .theme-icon {
        transition: none;
        animation: none;
      }
    }

    /* High contrast mode */
    @media (prefers-contrast: high) {
      .theme-toggle {
        border: 1px solid currentColor;
      }
    }
  `]
})
export class ThemeToggleComponent {
  private readonly themeService = inject(ThemeService);

  /**
   * Is dark theme active?
   */
  protected readonly isDark = this.themeService.isDark;

  /**
   * Current theme state
   */
  protected readonly themeState = this.themeService.state;

  /**
   * ARIA label for accessibility
   */
  protected readonly ariaLabel = computed(() => {
    const current = this.themeState().active;
    const next = current === 'light' ? 'dark' : 'light';
    return `Switch to ${next} mode`;
  });

  /**
   * Tooltip text
   */
  protected readonly tooltipText = computed(() => {
    const state = this.themeState();
    const sourceText = state.source === 'system' 
      ? ' (following system preference)' 
      : '';
    return `Current theme: ${state.active}${sourceText}`;
  });

  /**
   * Toggle theme
   */
  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}