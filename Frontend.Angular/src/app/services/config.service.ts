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

  private isConfigValid(config: Config): boolean {
    const keys = Object.values(ConfigKey);
    return keys.every(key => this.isConfigKeyValid(config, key));
  }

  // Load configuration from backend API
  loadConfig(): Observable<Config> {
    // Return cached config if it's valid
    if (this.config && this.isConfigValid(this.config)) {
      return of(this.config);
    }

    const fetch$ = this.http.get<Config>(`${environment.apiUrl}/configs`);

    return fetch$.pipe(
      switchMap(config => this.isConfigValid(config) ? of(config) : fetch$),
      tap(config => {
        this.config = config;
        if (isDevMode()) {
          console.log('Config loaded:', this.config);
        }
      }),
      catchError(error => {
        console.error('Failed to load configuration:', error);
        return throwError(() => new Error('Failed to load configuration.'));
      })
    );
  }

  // Force reload of configuration from backend
  reload(): Observable<Config> {
    this.config = null;
    return this.loadConfig();
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
    if (this.config && this.isConfigValid(this.config)) {
      return this.config;
    }

    throw new Error('Configuration not loaded. Please call loadConfig() first.');
  }
}
