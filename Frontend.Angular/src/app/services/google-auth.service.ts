// src/app/services/google-auth.service.ts
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialized = false;
  private clientId = '';
  private resolveFn?: (token: string) => void;
  private rejectFn?: (reason?: unknown) => void;
  private signInInProgress = false;

  /** Initializes the Google Identity SDK */
  async init(clientId: string): Promise<void> {
    if (this.initialized) return;
    if (!clientId) {
      return Promise.reject('Google Client ID is required.');
    }

    this.clientId = clientId;
    await this.loadGIS();
    const google = (window as any).google;
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
    if (!this.initialized) {
      return Promise.reject('GoogleAuthService not initialized.');
    }

    if (this.signInInProgress) {
      return Promise.reject('Google Sign-In already in progress.');
    }

    this.signInInProgress = true;
    const google = (window as any).google;

    return new Promise((resolve, reject) => {
      this.resolveFn = resolve;
      this.rejectFn = reject;
      try {
        google.accounts.id.prompt(
          (notification: any) => {
            if (notification.isNotDisplayed()) {
              reject('Google Sign-In not displayed.');
              this.clearHandlers();
            } else if (notification.isDismissedMoment()) {
              reject('Google Sign-In dismissed.');
              this.clearHandlers();
            } else if (notification.isSkippedMoment()) {
              reject('Google Sign-In skipped.');
              this.clearHandlers();
            }
          },
        );
      } catch (e) {
        reject(e);
        this.clearHandlers();
      }
    });
  }

  /** Loads Google Identity Services script if needed */
  private loadGIS(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (typeof (window as any).google !== 'undefined') {
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
    this.signInInProgress = false;
  }
}
