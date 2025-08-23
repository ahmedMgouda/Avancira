import { HttpClient } from '@angular/common/http';
import { Injectable, isDevMode } from '@angular/core';
import { catchError, Observable, of, tap, throwError, switchMap } from 'rxjs';

import { environment } from '../environments/environment';
import { ConfigKey } from '../models/config-key';
import { SocialProvider } from '../models/social-provider';


export interface Config {
  [ConfigKey.StripePublishableKey]: string;
  [ConfigKey.PayPalClientId]: string;
  [ConfigKey.GoogleMapsApiKey]: string;
  [ConfigKey.GoogleClientId]: string;
  [ConfigKey.FacebookAppId]: string;
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

  private isConfigKeyValid(config: Config, key: ConfigKey): boolean {
    const value = config[key];
    return typeof value === 'string' ? value.trim() !== '' : value !== undefined && value !== null;
  }

  private isStoredConfigFresh(timestamp: number): boolean {
    const oneDay = 24 * 60 * 60 * 1000;
    return Date.now() - timestamp < oneDay;
  }

  // Load configuration from backend API
  loadConfig(): Observable<Config> {
    const keys = Object.values(ConfigKey);
    const allKeysValid = (cfg: Config) => keys.every(key => this.isConfigKeyValid(cfg, key));

    // Check if we have a valid config with all required keys
    if (this.config && allKeysValid(this.config)) {
      return of(this.config);
    }

    const storedConfig = localStorage.getItem('config');
    if (storedConfig) {
      try {
        const parsed = JSON.parse(storedConfig) as StoredConfig | Config;
        if ('config' in parsed && 'timestamp' in parsed) {
          const stored = parsed as StoredConfig;
          if (allKeysValid(stored.config) && this.isStoredConfigFresh(stored.timestamp)) {
            this.config = stored.config;
            return of(this.config);
          }
        } else if (allKeysValid(parsed as Config)) {
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

    const fetch$ = this.http.get<Config>(`${environment.apiUrl}/configs`);

    return fetch$.pipe(
      switchMap((config) => {
        if (allKeysValid(config)) {
          return of(config);
        }
        return fetch$;
      }),
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
  get(key: ConfigKey): string {
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
        this.config = ('config' in parsed) ? (parsed as StoredConfig).config : (parsed as Config);
        return this.config as Config;
      } catch {
        // ignore parse errors and fall through
      }
    }
  
    // If still not found, throw an error
    throw new Error('Configuration not loaded. Please call loadConfig() first.');
  }
}
