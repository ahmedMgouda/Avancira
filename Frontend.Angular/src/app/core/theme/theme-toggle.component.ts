import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';

import { ThemeService } from '../services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      class="theme-toggle"
      (click)="toggleTheme()"
      [attr.aria-label]="ariaLabel()"
      [attr.aria-pressed]="isDark()"
      [attr.data-theme]="currentTheme()">
      <svg class="icon icon-sun" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M12 4.75a.75.75 0 0 1-.75-.75V2a.75.75 0 0 1 1.5 0v2c0 .414-.336.75-.75.75Zm5.303 2.197a.75.75 0 0 1 0-1.06l1.414-1.415a.75.75 0 1 1 1.06 1.061l-1.414 1.414a.75.75 0 0 1-1.06 0Zm-10.606 0a.75.75 0 0 1-1.06 0L4.223 5.533a.75.75 0 1 1 1.06-1.061l1.415 1.414a.75.75 0 0 1 0 1.06ZM12 19.25a.75.75 0 0 1 .75.75v2a.75.75 0 0 1-1.5 0v-2c0-.414.336-.75.75-.75Zm9-7.25a.75.75 0 0 1-.75.75h-2a.75.75 0 0 1 0-1.5h2c.414 0 .75.336.75.75Zm-16.25.75h-2a.75.75 0 0 1 0-1.5h2a.75.75 0 0 1 0 1.5ZM18.364 18.364a.75.75 0 0 1-1.06 0l-1.415-1.414a.75.75 0 1 1 1.061-1.06l1.414 1.414a.75.75 0 0 1 0 1.06Zm-11.728 0a.75.75 0 0 1 0-1.06l1.414-1.415a.75.75 0 1 1 1.06 1.061l-1.414 1.414a.75.75 0 0 1-1.06 0ZM12 8a4 4 0 1 1 0 8 4 4 0 0 1 0-8Z" />
      </svg>
      <svg class="icon icon-moon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
        <path d="M21 12.79A9 9 0 0 1 11.21 3 7 7 0 1 0 21 12.79Z" />
      </svg>
      <span class="sr-only">{{ helperText() }}</span>
    </button>
  `,
  styles: [`
    :host {
      display: inline-flex;
    }

    .theme-toggle {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 2.75rem;
      height: 2.75rem;
      border-radius: 9999px;
      border: 1px solid rgba(15, 23, 42, 0.12);
      background: linear-gradient(135deg, rgba(255, 255, 255, 0.95), rgba(248, 250, 252, 0.95));
      color: #0f172a;
      cursor: pointer;
      transition: transform 150ms ease, box-shadow 150ms ease, border-color 150ms ease, background 150ms ease;
      position: relative;
      overflow: hidden;
    }

    .theme-toggle:hover {
      transform: translateY(-1px);
      box-shadow: 0 10px 25px rgba(15, 23, 42, 0.15);
    }

    .theme-toggle:focus-visible {
      outline: 3px solid rgba(59, 130, 246, 0.45);
      outline-offset: 3px;
    }

    .theme-toggle:active {
      transform: scale(0.97);
    }

    .theme-toggle .icon {
      width: 1.35rem;
      height: 1.35rem;
      fill: currentColor;
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      transition: opacity 200ms ease, transform 200ms ease;
    }

    .theme-toggle .icon-moon {
      opacity: 0;
      transform: translate(-50%, calc(-50% + 6px)) scale(0.6);
    }

    .theme-toggle[data-theme='dark'] {
      border-color: rgba(148, 163, 184, 0.35);
      background: linear-gradient(135deg, rgba(30, 41, 59, 0.9), rgba(15, 23, 42, 0.95));
      color: #f8fafc;
      box-shadow: 0 12px 30px rgba(15, 23, 42, 0.6);
    }

    .theme-toggle[data-theme='dark'] .icon-sun {
      opacity: 0;
      transform: translate(-50%, calc(-50% - 6px)) scale(0.6);
    }

    .theme-toggle[data-theme='dark'] .icon-moon {
      opacity: 1;
      transform: translate(-50%, -50%) scale(1);
    }

    .theme-toggle[data-theme='light'] .icon-sun {
      opacity: 1;
      transform: translate(-50%, -50%) scale(1);
    }

    .theme-toggle[data-theme='light'] .icon-moon {
      opacity: 0;
      transform: translate(-50%, calc(-50% + 6px)) scale(0.6);
    }

    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }

    :host-context(.dark-theme) .theme-toggle {
      border-color: rgba(148, 163, 184, 0.35);
      background: linear-gradient(135deg, rgba(30, 41, 59, 0.9), rgba(15, 23, 42, 0.95));
      color: #f8fafc;
      box-shadow: 0 12px 30px rgba(15, 23, 42, 0.6);
    }

    :host-context(.dark-theme) .theme-toggle:hover {
      box-shadow: 0 16px 35px rgba(15, 23, 42, 0.7);
    }
  `]
})
export class ThemeToggleComponent {
  private readonly themeService = inject(ThemeService);

  readonly currentTheme = this.themeService.theme;
  readonly isDark = computed(() => this.currentTheme() === 'dark');
  readonly ariaLabel = computed(() =>
    this.isDark()
      ? 'Activate light mode'
      : 'Activate dark mode'
  );
  readonly helperText = computed(() =>
    this.isDark()
      ? 'Switch to light appearance'
      : 'Switch to dark appearance'
  );

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
