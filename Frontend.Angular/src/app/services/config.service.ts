import { HttpClient } from '@angular/common/http';
import { Injectable, isDevMode } from '@angular/core';
import { catchError, Observable, of, tap, throwError } from 'rxjs';

import { environment } from '../environments/environment';
import { SocialProvider } from '../models/social-provider';


export interface Config {
  stripePublishableKey: string;
  payPalClientId: string;
  googleMapsApiKey: string;
  googleClientId: string;
  facebookAppId: string;
  enabledSocialProviders: SocialProvider[];
  [key: string]: string | SocialProvider[];
}

interface StoredConfig {
  config: Config;
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config: Config | null = null;

  constructor(private http: HttpClient) { }

  // Check if configuration is valid (all required keys are present and not all empty)
  private isConfigValid(config: Config): boolean {
    if (!config) {
      return false;
    }

    // Define the required configuration keys that should be loaded from API
    const requiredKeys = [
      'stripePublishableKey',
      'payPalClientId',
      'googleMapsApiKey',
      'googleClientId',
      'facebookAppId'
    ];

    // Check if all required keys exist in the config object
    const allKeysExist = requiredKeys.every(key => key in config);
    
    // Check if all values are empty strings (this should trigger a reload)
    const allValuesEmpty = requiredKeys.every(key => config[key] === '');
    
    // Config is valid if all keys exist AND not all values are empty
    return allKeysExist && !allValuesEmpty;
  }

  private isStoredConfigFresh(timestamp: number): boolean {
    const oneDay = 24 * 60 * 60 * 1000;
    return Date.now() - timestamp < oneDay;
  }

  // Load configuration from backend API
  loadConfig(): Observable<Config> {
    // Check if we have a valid config with all required keys
    if (this.config && this.isConfigValid(this.config)) {
      return of(this.config);
    }

    const storedConfig = localStorage.getItem('config');
    if (storedConfig) {
      try {
        const parsed = JSON.parse(storedConfig) as StoredConfig | Config;
        if ('config' in parsed && 'timestamp' in parsed) {
          if (this.isConfigValid(parsed.config) && this.isStoredConfigFresh(parsed.timestamp)) {
            this.config = parsed.config;
            return of(this.config);
          }
        } else if (this.isConfigValid(parsed as Config)) {
          // migrate old format without timestamp
          this.config = parsed as Config;
          const migration: StoredConfig = { config: this.config, timestamp: Date.now() };
          localStorage.setItem('config', JSON.stringify(migration));
          return of(this.config);
        }
      } catch {
        // fall through to refetch
      }
      localStorage.removeItem('config');
    }

    return this.http.get<Config>(`${environment.apiUrl}/configs`)
      .pipe(
        tap((config) => {
          this.config = config;
          const stored: StoredConfig = { config, timestamp: Date.now() };
          localStorage.setItem('config', JSON.stringify(stored));
          if (isDevMode()) {
            console.log('Config loaded:', this.config);
          }
        }),
        catchError((error) => {
          console.error('Failed to load configuration:', error);
          return throwError(() => new Error('Failed to load configuration.'));
        })
      );
  }

  // Retrieve a specific key from the config
  get(key: string): string {
    return this.getConfig()[key] as string;
  }

  getEnabledSocialProviders(): SocialProvider[] {
    return (this.getConfig().enabledSocialProviders ?? []) as SocialProvider[];
  }

  isSocialProviderEnabled(provider: SocialProvider): boolean {
    return this.getEnabledSocialProviders().includes(provider);
  }

  get googleEnabled(): boolean {
    return this.isSocialProviderEnabled(SocialProvider.Google);
  }

  get facebookEnabled(): boolean {
    return this.isSocialProviderEnabled(SocialProvider.Facebook);
  }

  // Optional: Retrieve the entire configuration object
  getConfig(): Config {
    // If config is already loaded in memory, return it
    if (this.config) {
      return this.config;
    }
  
    // Try loading from localStorage if not in memory
    const storedConfig = localStorage.getItem('config');
    if (storedConfig) {
      try {
        const parsed = JSON.parse(storedConfig) as StoredConfig | Config;
        this.config = ('config' in parsed) ? parsed.config : (parsed as Config);
        return this.config;
      } catch {
        // ignore parse errors and fall through
      }
    }
  
    // If still not found, throw an error
    throw new Error('Configuration not loaded. Please call loadConfig() first.');
  }
}
