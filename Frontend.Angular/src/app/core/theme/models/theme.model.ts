/**
 * Theme Models
 * ═══════════════════════════════════════════════════════════════════════
 * Type definitions for the theme system
 */

/**
 * Available theme options
 */
export type Theme = 'light' | 'dark' | 'auto';

/**
 * Active theme (resolved from 'auto')
 */
export type ActiveTheme = 'light' | 'dark';

/**
 * Theme preference source
 */
export type ThemeSource = 'user' | 'system' | 'default';

/**
 * Complete theme state
 */
export interface ThemeState {
  /**
   * User's theme preference (can be 'auto')
   */
  preference: Theme;

  /**
   * Currently active theme (always 'light' or 'dark')
   */
  active: ActiveTheme;

  /**
   * Where the current theme came from
   */
  source: ThemeSource;

  /**
   * System's color scheme preference
   */
  systemPreference: ActiveTheme;
}

/**
 * Theme configuration options
 */
export interface ThemeConfig {
  /**
   * Default theme to use when no preference is set
   * @default 'light'
   */
  defaultTheme: ActiveTheme;

  /**
   * LocalStorage key for theme persistence
   * @default 'app-theme'
   */
  storageKey: string;

  /**
   * Enable system theme detection
   * @default true
   */
  detectSystemTheme: boolean;

  /**
   * Enable smooth transitions when switching themes
   * @default true
   */
  enableTransitions: boolean;

  /**
   * Transition duration in milliseconds
   * @default 300
   */
  transitionDuration: number;
}