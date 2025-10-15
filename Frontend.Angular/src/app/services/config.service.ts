import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, isDevMode } from '@angular/core';
import { catchError, Observable, of, tap, throwError, switchMap, map } from 'rxjs';

import { environment } from '../environments/environment';
import { ConfigKey } from '../models/config-key';
import { SocialProvider } from '../models/social-provider';
import { INCLUDE_CREDENTIALS } from '../interceptors/auth.interceptor';

export type Config = Record<ConfigKey, string>;

export interface ConfigResponse {
  config: Config;
  enabledSocialProviders: SocialProvider[];
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config: Config | null = null;
  private enabledSocialProviders: SocialProvider[] = [];

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

    const fetch$ = this.http.get<ConfigResponse>(`${environment.apiUrl}/configs`, {
      context: new HttpContext().set(INCLUDE_CREDENTIALS, true)
    });

    return fetch$.pipe(
      switchMap(resp => this.isConfigValid(resp.config) ? of(resp) : fetch$),
      tap(resp => {
        this.config = resp.config;
        this.enabledSocialProviders = resp.enabledSocialProviders ?? [];
        if (isDevMode()) {
          console.log('Config loaded:', this.config);
        }
      }),
      map(resp => resp.config),
      catchError(error => {
        console.error('Failed to load configuration:', error);
        return throwError(() => new Error('Failed to load configuration.'));
      })
    );
  }

  // Force reload of configuration from backend
  reload(): Observable<Config> {
    this.config = null;
    this.enabledSocialProviders = [];
    return this.loadConfig();
  }

  // Retrieve a specific key from the config
  get(key: ConfigKey): string {
    return this.getConfig()[key];
  }

  getEnabledSocialProviders(): SocialProvider[] {
    return this.enabledSocialProviders;
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
