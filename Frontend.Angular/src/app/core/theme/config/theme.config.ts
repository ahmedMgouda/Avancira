/**
 * Theme Configuration
 * ═══════════════════════════════════════════════════════════════════════
 * Default configuration for the theme system
 */

import { InjectionToken } from '@angular/core';
import { ThemeConfig } from '../models/theme.model';

/**
 * Default theme configuration
 */
export const DEFAULT_THEME_CONFIG: ThemeConfig = {
  defaultTheme: 'light',
  storageKey: 'avancira-theme',
  detectSystemTheme: true,
  enableTransitions: true,
  transitionDuration: 300
};

/**
 * Injection token for theme configuration
 */
export const THEME_CONFIG = new InjectionToken<ThemeConfig>(
  'THEME_CONFIG',
  {
    providedIn: 'root',
    factory: () => DEFAULT_THEME_CONFIG
  }
);