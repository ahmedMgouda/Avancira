import { Injectable, inject, signal, computed, DestroyRef, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { fromEvent, map, startWith } from 'rxjs';
import { 
  Theme, 
  ActiveTheme, 
  ThemeState, 
  ThemeSource,
  ThemeConfig 
} from '../models/theme.model';
import { THEME_CONFIG } from '../config/theme.config';

/**
 * ThemeService
 * ═══════════════════════════════════════════════════════════════════════
 * Manages application theme (light/dark mode)
 * 
 * Features:
 * - User preference persistence (localStorage)
 * - System theme detection (prefers-color-scheme)
 * - Auto theme (follows system preference)
 * - Signal-based reactive state
 * - CSS variable-based theming
 * - Smooth transitions
 * - SSR compatible
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly config = inject(THEME_CONFIG);
  private readonly destroyRef = inject(DestroyRef);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);

  // ═══════════════════════════════════════════════════════════════════════
  // State Signals
  // ═══════════════════════════════════════════════════════════════════════

  private readonly _preference = signal<Theme>('auto');
  private readonly _systemPreference = signal<ActiveTheme>('light');

  // ═══════════════════════════════════════════════════════════════════════
  // Computed Signals
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Active theme (resolved from preference and system)
   */
  readonly activeTheme = computed<ActiveTheme>(() => {
    const preference = this._preference();
    
    if (preference === 'auto') {
      return this._systemPreference();
    }
    
    return preference as ActiveTheme;
  });

  /**
   * Theme source (where the active theme came from)
   */
  readonly themeSource = computed<ThemeSource>(() => {
    const preference = this._preference();
    
    if (preference === 'auto') {
      return 'system';
    }
    
    // Check if preference was loaded from storage
    if (this.isBrowser) {
      const stored = localStorage.getItem(this.config.storageKey);
      if (stored === preference) {
        return 'user';
      }
    }
    
    return 'default';
  });

  /**
   * Complete theme state
   */
  readonly state = computed<ThemeState>(() => ({
    preference: this._preference(),
    active: this.activeTheme(),
    source: this.themeSource(),
    systemPreference: this._systemPreference()
  }));

  /**
   * Is dark theme active?
   */
  readonly isDark = computed(() => this.activeTheme() === 'dark');

  /**
   * Is light theme active?
   */
  readonly isLight = computed(() => this.activeTheme() === 'light');

  /**
   * Is using system theme?
   */
  readonly isAuto = computed(() => this._preference() === 'auto');

  constructor() {
    if (this.isBrowser) {
      this.initialize();
    }
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Set theme preference
   */
  setTheme(theme: Theme): void {
    this._preference.set(theme);
    this.persistTheme(theme);
    this.applyTheme();
  }

  /**
   * Toggle between light and dark themes
   * If currently 'auto', switches to the opposite of system preference
   */
  toggleTheme(): void {
    const currentActive = this.activeTheme();
    const newTheme: ActiveTheme = currentActive === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }

  /**
   * Reset to system theme (auto)
   */
  resetToSystem(): void {
    this.setTheme('auto');
  }

  /**
   * Clear theme preference (use default)
   */
  clearTheme(): void {
    if (this.isBrowser) {
      localStorage.removeItem(this.config.storageKey);
    }
    this._preference.set('auto');
    this.applyTheme();
  }

  // ═══════════════════════════════════════════════════════════════════════
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════════

  /**
   * Initialize theme system
   */
  private initialize(): void {
    // Load system preference
    this._systemPreference.set(this.detectSystemTheme());

    // Monitor system theme changes
    if (this.config.detectSystemTheme) {
      this.monitorSystemTheme();
    }

    // Load saved preference
    const savedTheme = this.loadTheme();
    if (savedTheme) {
      this._preference.set(savedTheme);
    }

    // Apply initial theme
    this.applyTheme();
  }

  /**
   * Detect system color scheme preference
   */
  private detectSystemTheme(): ActiveTheme {
    if (!this.isBrowser) {
      return this.config.defaultTheme;
    }

    const isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return isDark ? 'dark' : 'light';
  }

  /**
   * Monitor system theme changes
   */
  private monitorSystemTheme(): void {
    if (!this.isBrowser) {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    
    fromEvent<MediaQueryListEvent>(mediaQuery, 'change')
      .pipe(
        map(event => event.matches ? 'dark' : 'light' as ActiveTheme),
        startWith(this.detectSystemTheme()),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(theme => {
        this._systemPreference.set(theme);
        // Re-apply theme if using auto
        if (this._preference() === 'auto') {
          this.applyTheme();
        }
      });
  }

  /**
   * Load theme from localStorage
   */
  private loadTheme(): Theme | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      const saved = localStorage.getItem(this.config.storageKey);
      if (saved && this.isValidTheme(saved)) {
        return saved as Theme;
      }
    } catch (error) {
      console.warn('Failed to load theme from localStorage:', error);
    }

    return null;
  }

  /**
   * Persist theme to localStorage
   */
  private persistTheme(theme: Theme): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.setItem(this.config.storageKey, theme);
    } catch (error) {
      console.warn('Failed to save theme to localStorage:', error);
    }
  }

  /**
   * Apply theme to DOM
   */
  private applyTheme(): void {
    if (!this.isBrowser) {
      return;
    }

    const theme = this.activeTheme();
    const root = document.documentElement;

    // Add transition class if enabled
    if (this.config.enableTransitions) {
      root.classList.add('theme-transition');
      setTimeout(() => {
        root.classList.remove('theme-transition');
      }, this.config.transitionDuration);
    }

    // Update data-theme attribute
    root.setAttribute('data-theme', theme);

    // Update class for legacy support
    root.classList.remove('theme-light', 'theme-dark');
    root.classList.add(`theme-${theme}`);
  }

  /**
   * Validate theme string
   */
  private isValidTheme(value: string): value is Theme {
    return ['light', 'dark', 'auto'].includes(value);
  }
}