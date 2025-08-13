// src/app/services/google-auth.service.ts
import { Injectable } from '@angular/core';

declare const google: any;

@Injectable({ providedIn: 'root' })
export class GoogleAuthService {
  private initialized = false;

  /** Initializes the Google Identity SDK */
  async init(clientId: string): Promise<void> {
    if (this.initialized) return;

    await this.waitForGIS();

    google.accounts.id.initialize({
      client_id: clientId,
      callback: () => {}, // callback will be used manually in signIn()
      ux_mode: 'popup',
    });

    this.initialized = true;
  }

  /** Triggers Google Sign-In popup and resolves ID token */
  async signIn(): Promise<string> {
    return new Promise((resolve, reject) => {
      try {
        google.accounts.id.prompt((notification: any) => {
          if (notification.isNotDisplayed()) {
            reject('Google Sign-In not displayed.');
          }
        });

        google.accounts.id.initialize({
          client_id: '', // Already initialized once
          callback: (response: any) => {
            if (response.credential) {
              resolve(response.credential); // This is the ID token
            } else {
              reject('No credential returned.');
            }
          },
          ux_mode: 'popup',
        });

        google.accounts.id.prompt(); // trigger popup
      } catch (e) {
        reject(e);
      }
    });
  }

  /** Waits for `google` object to load */
  private waitForGIS(): Promise<void> {
    return new Promise((resolve, reject) => {
      let retries = 0;
      const interval = setInterval(() => {
        if (typeof google !== 'undefined') {
          clearInterval(interval);
          resolve();
        } else if (++retries > 10) {
          clearInterval(interval);
          reject('Google Identity SDK not loaded.');
        }
      }, 300);
    });
  }
}
