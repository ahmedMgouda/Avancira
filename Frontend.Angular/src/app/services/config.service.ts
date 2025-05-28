import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, of, tap, throwError } from 'rxjs';

import { environment } from '../environments/environment';


export interface Config {
  stripePublishableKey: string;
  payPalClientId: string;
  googleMapsApiKey: string;
  googleClientId: string;
  googleClientSecret: string;
  facebookAppId: string;
  [key: string]: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config: any = null;

  constructor(private http: HttpClient) { }

  // Load configuration from backend API
  loadConfig(): Observable<Config> {
    if (this.config) {
      return of(this.config);
    }

    const storedConfig = localStorage.getItem('config');
    if (storedConfig) {
      this.config = JSON.parse(storedConfig) as Config;
      return of(this.config);
    }

    return this.http.get<Config>(`${environment.apiUrl}/configs`)
      .pipe(
        tap((config) => {
          this.config = config;
          localStorage.setItem('config', JSON.stringify(config));
          console.log('Config loaded:', this.config);
        }),
        catchError((error) => {
          console.error('Failed to load configuration:', error);
          return throwError(() => new Error('Failed to load configuration.'));
        })
      );
  }

  // Retrieve a specific key from the config
  get(key: string): string {
    return this.getConfig()[key];
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
      this.config = JSON.parse(storedConfig) as Config;
      return this.config;
    }
  
    // If still not found, throw an error
    throw new Error('Configuration not loaded. Please call loadConfig() first.');
  }
}
