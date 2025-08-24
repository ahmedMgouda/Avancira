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
      use_fedcm_for_prompt: true,
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
      const timeout = setTimeout(() => {
        reject('Google Sign-In timed out.');
        this.clearHandlers();
      }, 10000);

      this.resolveFn = (token: string) => {
        clearTimeout(timeout);
        resolve(token);
      };

      this.rejectFn = (reason?: unknown) => {
        clearTimeout(timeout);
        reject(reason);
      };

      try {
        google.accounts.id.prompt(
          (notification: google.accounts.id.PromptMomentNotification) => {
            console.log(notification);
            if (notification.isNotDisplayed()) {
              this.rejectFn?.('Google Sign-In not displayed.');
              this.clearHandlers();
            } else if (notification.isDismissedMoment()) {
              this.rejectFn?.('Google Sign-In dismissed.');
              this.clearHandlers();
            } else if (notification.isSkippedMoment()) {
              this.rejectFn?.('Google Sign-In skipped.');
              this.clearHandlers();
            } else if (!notification.isNotDisplayed()) {
              // Sign-in prompt displayed successfully; actual resolution happens in callback
            }
          },
        );
      } catch (e) {
        this.rejectFn?.(e);
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
