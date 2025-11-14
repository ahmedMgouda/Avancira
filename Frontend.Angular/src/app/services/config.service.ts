import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { catchError, Observable, of, tap, throwError } from 'rxjs';

import { environment } from '../environments/environment';

/**
 * ═══════════════════════════════════════════════════════════════════════════
 * CONFIG SERVICE - FIXED
 * ═══════════════════════════════════════════════════════════════════════════
 * 
 * FIXES:
 * ✅ SSR-safe: Platform checks for localStorage
 * ✅ Graceful degradation in server context
 * ✅ No crashes during SSR
 * 
 * PURPOSE:
 * Loads third-party API keys (Stripe, PayPal, Google Maps, etc.) from backend
 */

export interface Config {
  stripePublishableKey: string;
  payPalClientId: string;
  googleMapsApiKey: string;
  googleClientId: string;
  googleClientSecret: string;
  facebookAppId: string;
  [key: string]: string;
}

@Injectable({ providedIn: 'root' })
export class ConfigService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly isBrowser = isPlatformBrowser(this.platformId);
  
  private config: Config | null = null;

  // ═══════════════════════════════════════════════════════════════════════════
  // Public API
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Load configuration from backend API
   * Uses localStorage cache if available (browser only)
   */
  loadConfig(): Observable<Config> {
    // Return cached config if valid
    if (this.config && this.isConfigValid(this.config)) {
      return of(this.config);
    }

    // FIX: Platform check for localStorage
    if (this.isBrowser) {
      const storedConfig = this.getFromStorage();
      if (storedConfig && this.isConfigValid(storedConfig)) {
        this.config = storedConfig;
        return of(this.config);
      }
      // If stored config is invalid, remove it
      if (storedConfig) {
        this.removeFromStorage();
      }
    }

    // Fetch from backend
    return this.http.get<Config>(`${environment.bffBaseUrl}/configs`)
      .pipe(
        tap((config) => {
          this.config = config;
          
          // FIX: Only use localStorage in browser
          if (this.isBrowser) {
            this.saveToStorage(config);
          }
          
          console.log('Config loaded:', this.config);
        }),
        catchError((error) => {
          console.error('Failed to load configuration:', error);
          return throwError(() => new Error('Failed to load configuration.'));
        })
      );
  }

  /**
   * Retrieve a specific key from the config
   */
  get(key: string): string {
    return this.getConfig()[key];
  }

  /**
   * Retrieve the entire configuration object
   */
  getConfig(): Config {
    // Return from memory if available
    if (this.config) {
      return this.config;
    }
  
    // FIX: Try localStorage only in browser
    if (this.isBrowser) {
      const storedConfig = this.getFromStorage();
      if (storedConfig) {
        this.config = storedConfig;
        return this.config;
      }
    }
  
    throw new Error('Configuration not loaded. Please call loadConfig() first.');
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Private Methods
  // ═══════════════════════════════════════════════════════════════════════════

  /**
   * Check if configuration has all required keys and values
   */
  private isConfigValid(config: Config): boolean {
    if (!config) {
      return false;
    }

    const requiredKeys = [
      'stripePublishableKey',
      'payPalClientId',
      'googleMapsApiKey',
      'googleClientId',
      'googleClientSecret',
      'facebookAppId'
    ];

    const allKeysExist = requiredKeys.every(key => key in config);
    const allValuesEmpty = requiredKeys.every(key => config[key] === '');
    
    return allKeysExist && !allValuesEmpty;
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // LocalStorage Helpers - SSR-Safe
  // ═══════════════════════════════════════════════════════════════════════════

  private getFromStorage(): Config | null {
    if (!this.isBrowser) return null;
    
    try {
      const stored = localStorage.getItem('config');
      return stored ? JSON.parse(stored) as Config : null;
    } catch {
      return null;
    }
  }

  private saveToStorage(config: Config): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.setItem('config', JSON.stringify(config));
    } catch (e) {
      console.warn('[ConfigService] Failed to save to localStorage:', e);
    }
  }

  private removeFromStorage(): void {
    if (!this.isBrowser) return;
    
    try {
      localStorage.removeItem('config');
    } catch {
      // Ignore errors
    }
  }
}
