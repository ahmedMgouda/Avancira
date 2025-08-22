// src/app/services/google-auth.service.ts
import { Injectable } from '@angular/core';

declare const google: any;

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialized = false;
  private clientId = '';
  private resolveFn?: (token: string) => void;
  private rejectFn?: (reason?: any) => void;

  /** Initializes the Google Identity SDK */
  async init(clientId: string): Promise<void> {
    if (this.initialized) return;

    this.clientId = clientId;
    await this.loadGIS();

    google.accounts.id.initialize({
      client_id: this.clientId,
      callback: (response: any) => {
        if (response.credential) {
          this.resolveFn?.(response.credential);
        } else {
          this.rejectFn?.('No credential returned.');
        }
        this.clearHandlers();
      },
      ux_mode: 'popup',
    });

    this.initialized = true;
  }

  /** Triggers Google Sign-In popup and resolves ID token */
  async signIn(): Promise<string> {
    return new Promise((resolve, reject) => {
      this.resolveFn = resolve;
      this.rejectFn = reject;
      try {
        google.accounts.id.prompt((notification: any) => {
          if (notification.isNotDisplayed()) {
            reject('Google Sign-In not displayed.');
            this.clearHandlers();
          }
        });
      } catch (e) {
        reject(e);
        this.clearHandlers();
      }
    });
  }

  /** Loads Google Identity Services script if needed */
  private loadGIS(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (typeof google !== 'undefined') {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = 'https://accounts.google.com/gsi/client';
      script.async = true;
      script.defer = true;
      script.onload = () => resolve();
      script.onerror = () => reject('Google Identity SDK not loaded.');
      document.head.appendChild(script);
    });
  }

  private clearHandlers(): void {
    this.resolveFn = undefined;
    this.rejectFn = undefined;
  }
}
